using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models.SongSelect;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.ViewModels;

public partial class SongSelectDialogViewModel(
    ISongLibraryService songLibraryService,
    IFilesService filesService,
    string songsPath,
    bool allowMultipleSelection,
    string? preferredMapsetDirectoryPath = null) : ViewModelBase, IDisposable
{
    private const int PageSize = 20;
    private const int BackgroundCacheSize = 20;
    private const int SearchDebounceMilliseconds = 2000;
    private const int StatusQueryMaxLength = 36;
    private const double EstimatedMapsetCardHeight = 104d;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly string? _preferredMapsetDirectoryPath = NormalizeDirectoryPath(preferredMapsetDirectoryPath);
    private readonly List<string> _mapsetDirectories = [];
    private readonly List<MapsetDirectoryEntry> _mapsetDirectoryEntries = [];
    private readonly List<MapsetDirectoryEntry> _filteredDirectoryEntries = [];
    private readonly Dictionary<string, SongMapsetCardViewModel> _mapsetViewModelCache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _visibleDirectoryPaths = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _searchDebounceCts;
    private CancellationTokenSource? _searchExecutionCts;
    private bool _isDisposed;
    private int _filteredCursor;
    private int _queryVersion;
    private double _lastScrollOffsetY;
    private double _lastViewportHeight = EstimatedMapsetCardHeight * 6d;
    private int? _lastFirstVisibleIndex;
    private int? _lastLastVisibleIndex;
    private bool _isScrollLoadScheduled;
    private SongMapsetCardViewModel? _lastToggledMapset;
    private DateTime _lastMapsetToggleUtc = DateTime.MinValue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReloadLibraryCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBusyBox))]
    [NotifyCanExecuteChangedFor(nameof(ReloadLibraryCommand))]
    private bool _isBusyAreaActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBusyBox))]
    [NotifyCanExecuteChangedFor(nameof(ReloadLibraryCommand))]
    private bool _isSearchPending;

    [ObservableProperty] private string? _searchText = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private ObservableCollection<SongMapsetCardViewModel> _visibleMapsets = [];
    [ObservableProperty] private int _selectedDifficultyCount;

    public bool AllowMultipleSelection { get; } = allowMultipleSelection;
    public bool HasSongsPath => !string.IsNullOrWhiteSpace(songsPath);
    public bool HasMoreResults => _filteredCursor < _filteredDirectoryEntries.Count;
    public bool ShowBusyBox => IsBusyAreaActive || IsSearchPending;
    public bool ShowEmptyState => !ShowBusyBox && VisibleMapsets.Count == 0 && !ShowNotFoundBadge;
    public bool ShowNotFoundBadge => !ShowBusyBox &&
                                     !HasMoreResults &&
                                     VisibleMapsets.Count == 0 &&
                                     GetSearchQuery().Length > 0;
    public bool CanConfirmSelection => AllowMultipleSelection && SelectedDifficultyCount > 0;
    public bool CanReloadLibrary => HasSongsPath && !IsBusyAreaActive && !IsLoading && !IsSearchPending;

    public event Action<IReadOnlyList<string>>? SelectionSubmitted;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;

        _searchExecutionCts?.Cancel();
        _searchExecutionCts?.Dispose();
        _searchExecutionCts = null;

        foreach (var mapset in _mapsetViewModelCache.Values)
        {
            mapset.Dispose();
        }

        _mapsetViewModelCache.Clear();
        _visibleDirectoryPaths.Clear();
        _filteredDirectoryEntries.Clear();
        _mapsetDirectoryEntries.Clear();
        _mapsetDirectories.Clear();
        VisibleMapsets.Clear();
        _isScrollLoadScheduled = false;

        SelectionSubmitted = null;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusyAreaActive = true;
        IsLoading = true;
        OnPropertyChanged(nameof(ShowEmptyState));
        StatusMessage = "Preparing song list...";

        try
        {
            if (!HasSongsPath)
            {
                StatusMessage = "Songs folder is not configured. Use Manual Open.";
                return;
            }

            var directories = await songLibraryService.GetMapsetDirectoriesAsync(songsPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _mapsetDirectories.Clear();
            _mapsetDirectories.AddRange(directories);
            _mapsetDirectoryEntries.Clear();
            _mapsetDirectoryEntries.AddRange(_mapsetDirectories.Select(path =>
                new MapsetDirectoryEntry(path, Path.GetFileName(path))));
            PrioritizePreferredMapsetEntry();

            if (_mapsetDirectories.Count == 0)
            {
                StatusMessage = "No beatmap folders were found in the selected Songs folder.";
                return;
            }

            StatusMessage = $"Found {_mapsetDirectories.Count} mapset folders. Loading recent entries...";
            await StartSearchFromBeginningAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Song indexing was cancelled.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Failed to prepare song list: {exception.Message}";
        }
        finally
        {
            IsBusyAreaActive = false;
            IsLoading = false;
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    partial void OnSearchTextChanged(string? value)
    {
        if (_mapsetDirectoryEntries.Count == 0)
        {
            return;
        }

        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchExecutionCts?.Cancel();

        _queryVersion++;
        _filteredCursor = 0;
        _filteredDirectoryEntries.Clear();
        _visibleDirectoryPaths.Clear();
        VisibleMapsets.Clear();
        _lastFirstVisibleIndex = null;
        _lastLastVisibleIndex = null;
        _isScrollLoadScheduled = false;
        DeactivateAllCachedBackgrounds();
        foreach (var mapset in _mapsetViewModelCache.Values)
        {
            mapset.IsExpanded = false;
        }

        IsSearchPending = true;
        StatusMessage = $"Searching folders for \"{FormatQueryForStatus(GetSearchQuery())}\"...";
        NotifyListStateChanged();

        var debounceCts = new CancellationTokenSource();
        _searchDebounceCts = debounceCts;

        _ = DebouncedSearchAsync(debounceCts);
    }

    [RelayCommand]
    private void ToggleMapsetExpansion(SongMapsetCardViewModel mapset)
    {
        var now = DateTime.UtcNow;
        if (ReferenceEquals(_lastToggledMapset, mapset) &&
            (now - _lastMapsetToggleUtc).TotalMilliseconds < 200d)
        {
            return;
        }

        _lastToggledMapset = mapset;
        _lastMapsetToggleUtc = now;

        foreach (var item in VisibleMapsets)
        {
            if (!ReferenceEquals(item, mapset))
            {
                item.IsExpanded = false;
            }
        }

        mapset.IsExpanded = !mapset.IsExpanded;
    }

    [RelayCommand]
    private void SelectDifficulty(SongDifficultyItemViewModel difficulty)
    {
        if (!AllowMultipleSelection)
        {
            SelectionSubmitted?.Invoke([difficulty.OsuFilePath]);
            return;
        }

        difficulty.IsSelected = !difficulty.IsSelected;
        RecalculateSelectedDifficultyCount();
    }

    [RelayCommand]
    private async Task PickFilesManually(CancellationToken cancellationToken)
    {
        var files = await filesService.OpenFileAsync(new FilePickerOpenOptions
        {
            Title = AllowMultipleSelection ? "Select destination beatmap(s)" : "Select beatmap",
            AllowMultiple = AllowMultipleSelection,
            FileTypeFilter =
            [
                new FilePickerFileType("osu! beatmap file")
                {
                    Patterns = ["*.osu"],
                    MimeTypes = ["application/octet-stream"]
                }
            ]
        });

        if (cancellationToken.IsCancellationRequested || files is null || files.Count == 0)
        {
            return;
        }

        var selectedPaths = files
            .Select(file => file.Path.LocalPath)
            .Where(path => path.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedPaths.Count > 0)
        {
            SelectionSubmitted?.Invoke(selectedPaths);
        }
    }

    [RelayCommand(CanExecute = nameof(CanReloadLibrary))]
    private async Task ReloadLibrary(CancellationToken cancellationToken)
    {
        CancelPendingSearchOperations();
        songLibraryService.InvalidateCache(songsPath);
        ClearMapsetViewModelCache();
        await InitializeAsync(cancellationToken);
    }

    [RelayCommand]
    private void ConfirmSelection()
    {
        if (!AllowMultipleSelection)
        {
            return;
        }

        var selectedPaths = _mapsetViewModelCache.Values
            .SelectMany(mapset => mapset.Difficulties)
            .Where(difficulty => difficulty.IsSelected)
            .Select(difficulty => difficulty.OsuFilePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedPaths.Count > 0)
        {
            SelectionSubmitted?.Invoke(selectedPaths);
        }
    }

    [RelayCommand]
    private void SelectAllDifficultiesInMapset(SongMapsetCardViewModel? mapset)
    {
        if (!AllowMultipleSelection || mapset is null || mapset.Difficulties.Count == 0)
        {
            return;
        }

        var shouldSelectAll = !mapset.AllDifficultiesSelected;
        foreach (var difficulty in mapset.Difficulties)
        {
            difficulty.IsSelected = shouldSelectAll;
        }

        RecalculateSelectedDifficultyCount();
    }

    public void TryLoadMoreFromScroll(
        double offsetY,
        double viewportHeight,
        double extentHeight,
        int? firstVisibleIndex = null,
        int? lastVisibleIndex = null)
    {
        _lastScrollOffsetY = Math.Max(0d, offsetY);
        _lastViewportHeight = viewportHeight > 0d ? viewportHeight : _lastViewportHeight;

        if (firstVisibleIndex.HasValue && lastVisibleIndex.HasValue && firstVisibleIndex.Value <= lastVisibleIndex.Value)
        {
            _lastFirstVisibleIndex = firstVisibleIndex.Value;
            _lastLastVisibleIndex = lastVisibleIndex.Value;
        }

        UpdateBackgroundCacheForCurrentViewport();

        if (IsLoading || !HasMoreResults)
        {
            return;
        }

        if (offsetY + viewportHeight >= extentHeight - 220)
        {
            _ = LoadMoreFromScrollAsync(_queryVersion);
        }
    }

    private async Task LoadMoreFromScrollAsync(int queryVersion)
    {
        if (_isScrollLoadScheduled)
        {
            return;
        }

        _isScrollLoadScheduled = true;
        try
        {
            await LoadNextPageAsync(queryVersion, CancellationToken.None);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Failed to load more mapsets: {exception.Message}";
        }
        finally
        {
            _isScrollLoadScheduled = false;
        }
    }

    private async Task DebouncedSearchAsync(CancellationTokenSource debounceCts)
    {
        var cancellationToken = debounceCts.Token;
        try
        {
            await Task.Delay(SearchDebounceMilliseconds, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Task.Yield();

            var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _searchExecutionCts = executionCts;
            try
            {
                IsBusyAreaActive = true;
                await StartSearchFromBeginningAsync(executionCts.Token);
            }
            finally
            {
                IsBusyAreaActive = false;
                if (ReferenceEquals(_searchExecutionCts, executionCts))
                {
                    _searchExecutionCts = null;
                }

                executionCts.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            // A newer keystroke superseded this pending search.
        }
        finally
        {
            if (ReferenceEquals(_searchDebounceCts, debounceCts))
            {
                _searchDebounceCts = null;
                IsSearchPending = false;
                NotifyListStateChanged();
            }

            debounceCts.Dispose();
        }
    }

    private async Task StartSearchFromBeginningAsync(CancellationToken cancellationToken)
    {
        _queryVersion++;
        var queryVersion = _queryVersion;

        _filteredCursor = 0;
        _visibleDirectoryPaths.Clear();
        VisibleMapsets.Clear();
        _lastFirstVisibleIndex = null;
        _lastLastVisibleIndex = null;
        _isScrollLoadScheduled = false;
        DeactivateAllCachedBackgrounds();
        foreach (var mapset in _mapsetViewModelCache.Values)
        {
            mapset.IsExpanded = false;
        }

        SelectedDifficultyCount = _mapsetViewModelCache.Values
            .SelectMany(mapset => mapset.Difficulties)
            .Count(difficulty => difficulty.IsSelected);
        OnPropertyChanged(nameof(CanConfirmSelection));

        NotifyListStateChanged();

        if (_mapsetDirectoryEntries.Count == 0)
        {
            StatusMessage = HasSongsPath
                ? "No beatmap folders were found in the selected Songs folder."
                : "Songs folder is not configured. Use Manual Open.";
            return;
        }

        var query = GetSearchQuery();
        var filteredEntries = await Task.Run(() => FilterByFolderName(query), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (queryVersion != _queryVersion)
        {
            return;
        }

        _filteredDirectoryEntries.Clear();
        _filteredDirectoryEntries.AddRange(filteredEntries);
        NotifyListStateChanged();

        if (_filteredDirectoryEntries.Count == 0)
        {
            UpdateStatusMessage();
            return;
        }

        await LoadNextPageAsync(queryVersion, cancellationToken);
    }

    private async Task LoadNextPageAsync(int queryVersion, CancellationToken cancellationToken)
    {
        if (queryVersion != _queryVersion || _filteredDirectoryEntries.Count == 0 || !HasMoreResults)
        {
            return;
        }

        await _loadGate.WaitAsync(cancellationToken);
        try
        {
            if (queryVersion != _queryVersion)
            {
                return;
            }

            IsLoading = true;
            NotifyListStateChanged();

            var added = 0;

            while (_filteredCursor < _filteredDirectoryEntries.Count && added < PageSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (queryVersion != _queryVersion)
                {
                    return;
                }

                var directoryEntry = _filteredDirectoryEntries[_filteredCursor++];
                if (!_mapsetViewModelCache.TryGetValue(directoryEntry.DirectoryPath, out var mapsetViewModel))
                {
                    var mapset = await songLibraryService.LoadMapsetAsync(directoryEntry.DirectoryPath, cancellationToken);
                    if (queryVersion != _queryVersion)
                    {
                        return;
                    }

                    if (mapset is null)
                    {
                        continue;
                    }

                    mapsetViewModel = new SongMapsetCardViewModel(
                        mapset,
                        IsPreferredMapsetDirectory(directoryEntry.DirectoryPath));
                    _mapsetViewModelCache[directoryEntry.DirectoryPath] = mapsetViewModel;
                }
                else
                {
                    mapsetViewModel.IsPreferredMapset = IsPreferredMapsetDirectory(directoryEntry.DirectoryPath);
                }

                if (!_visibleDirectoryPaths.Add(directoryEntry.DirectoryPath))
                {
                    continue;
                }

                VisibleMapsets.Add(mapsetViewModel);
                added++;

                // Keep the dispatcher responsive while paging results.
                if ((added & 3) == 0)
                {
                    await Task.Yield();
                }
            }

            RecalculateSelectedDifficultyCount();
            UpdateBackgroundCacheForCurrentViewport();
            UpdateStatusMessage();
        }
        catch (OperationCanceledException)
        {
            // Expected during debounced typing or dialog dismissal.
        }
        finally
        {
            IsLoading = false;
            NotifyListStateChanged();
            _loadGate.Release();
        }
    }

    private void UpdateStatusMessage()
    {
        if (_mapsetDirectories.Count == 0)
        {
            StatusMessage = "No beatmap folders were found in the selected Songs folder.";
            return;
        }

        var query = GetSearchQuery();
        if (query.Length == 0)
        {
            StatusMessage =
                $"Loaded {VisibleMapsets.Count} mapset(s). Parsed {_filteredCursor} of {_filteredDirectoryEntries.Count}.";
            return;
        }

        var displayQuery = FormatQueryForStatus(query);
        StatusMessage = _filteredDirectoryEntries.Count == 0
            ? $"Not found for \"{displayQuery}\"."
            : $"Matched {_filteredDirectoryEntries.Count} folder(s) for \"{displayQuery}\". Loaded {VisibleMapsets.Count}.";
    }

    private void RecalculateSelectedDifficultyCount()
    {
        SelectedDifficultyCount = _mapsetViewModelCache.Values
            .SelectMany(mapset => mapset.Difficulties)
            .Count(difficulty => difficulty.IsSelected);

        OnPropertyChanged(nameof(CanConfirmSelection));
    }

    private List<MapsetDirectoryEntry> FilterByFolderName(string query)
    {
        if (query.Length == 0)
        {
            return _mapsetDirectoryEntries.ToList();
        }

        return _mapsetDirectoryEntries
            .Where(entry => entry.FolderName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void NotifyListStateChanged()
    {
        OnPropertyChanged(nameof(HasMoreResults));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowNotFoundBadge));
        OnPropertyChanged(nameof(ShowBusyBox));
    }

    private string GetSearchQuery()
    {
        return SearchText?.Trim() ?? string.Empty;
    }

    private static string FormatQueryForStatus(string query)
    {
        if (query.Length <= StatusQueryMaxLength)
        {
            return query;
        }

        return $"{query[..StatusQueryMaxLength]}...";
    }

    private void PrioritizePreferredMapsetEntry()
    {
        if (string.IsNullOrWhiteSpace(_preferredMapsetDirectoryPath) || _mapsetDirectoryEntries.Count == 0)
        {
            return;
        }

        var index = -1;
        for (var i = 0; i < _mapsetDirectoryEntries.Count; i++)
        {
            if (!string.Equals(
                    NormalizeDirectoryPath(_mapsetDirectoryEntries[i].DirectoryPath),
                    _preferredMapsetDirectoryPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            index = i;
            break;
        }

        if (index <= 0)
        {
            return;
        }

        var preferredEntry = _mapsetDirectoryEntries[index];
        _mapsetDirectoryEntries.RemoveAt(index);
        _mapsetDirectoryEntries.Insert(0, preferredEntry);
    }

    private static string? NormalizeDirectoryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }

    private bool IsPreferredMapsetDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(_preferredMapsetDirectoryPath))
        {
            return false;
        }

        return string.Equals(
            NormalizeDirectoryPath(directoryPath),
            _preferredMapsetDirectoryPath,
            StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateBackgroundCacheForCurrentViewport()
    {
        var totalVisible = VisibleMapsets.Count;
        if (totalVisible == 0)
        {
            return;
        }

        var keepCount = Math.Min(BackgroundCacheSize, totalVisible);
        int firstVisibleIndex;
        int lastVisibleIndex;

        if (_lastFirstVisibleIndex.HasValue && _lastLastVisibleIndex.HasValue)
        {
            firstVisibleIndex = Math.Clamp(_lastFirstVisibleIndex.Value, 0, totalVisible - 1);
            lastVisibleIndex = Math.Clamp(_lastLastVisibleIndex.Value, firstVisibleIndex, totalVisible - 1);
        }
        else
        {
            var viewportHeight = _lastViewportHeight > 0d ? _lastViewportHeight : EstimatedMapsetCardHeight * 6d;

            firstVisibleIndex = (int)Math.Floor(_lastScrollOffsetY / EstimatedMapsetCardHeight);
            firstVisibleIndex = Math.Clamp(firstVisibleIndex, 0, totalVisible - 1);

            lastVisibleIndex = (int)Math.Ceiling((_lastScrollOffsetY + viewportHeight) / EstimatedMapsetCardHeight);
            lastVisibleIndex = Math.Clamp(lastVisibleIndex, firstVisibleIndex, totalVisible - 1);
        }

        var visibleCount = lastVisibleIndex - firstVisibleIndex + 1;
        int cacheStart;
        int cacheEnd;

        if (visibleCount >= keepCount)
        {
            cacheStart = firstVisibleIndex;
            cacheEnd = cacheStart + keepCount - 1;
        }
        else
        {
            var extraSlots = keepCount - visibleCount;
            var beforeCount = extraSlots / 2;
            var afterCount = extraSlots - beforeCount;

            cacheStart = firstVisibleIndex - beforeCount;
            cacheEnd = lastVisibleIndex + afterCount;

            if (cacheStart < 0)
            {
                cacheEnd = Math.Min(totalVisible - 1, cacheEnd - cacheStart);
                cacheStart = 0;
            }

            if (cacheEnd >= totalVisible)
            {
                var overflow = cacheEnd - (totalVisible - 1);
                cacheStart = Math.Max(0, cacheStart - overflow);
                cacheEnd = totalVisible - 1;
            }
        }

        for (var i = 0; i < totalVisible; i++)
        {
            VisibleMapsets[i].SetBackgroundActive(i >= cacheStart && i <= cacheEnd);
        }
    }

    private void DeactivateAllCachedBackgrounds()
    {
        foreach (var mapset in _mapsetViewModelCache.Values)
        {
            mapset.SetBackgroundActive(false);
        }
    }

    private void CancelPendingSearchOperations()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;

        _searchExecutionCts?.Cancel();
        _searchExecutionCts?.Dispose();
        _searchExecutionCts = null;

        _isScrollLoadScheduled = false;
        IsSearchPending = false;
    }

    private void ClearMapsetViewModelCache()
    {
        DeactivateAllCachedBackgrounds();

        foreach (var mapset in _mapsetViewModelCache.Values)
        {
            mapset.Dispose();
        }

        _mapsetViewModelCache.Clear();
        _visibleDirectoryPaths.Clear();
        _filteredDirectoryEntries.Clear();
        _mapsetDirectoryEntries.Clear();
        _mapsetDirectories.Clear();
        VisibleMapsets.Clear();
        _filteredCursor = 0;
        SelectedDifficultyCount = 0;
        _lastFirstVisibleIndex = null;
        _lastLastVisibleIndex = null;
        _lastToggledMapset = null;
        _lastMapsetToggleUtc = DateTime.MinValue;
        NotifyListStateChanged();
        OnPropertyChanged(nameof(CanConfirmSelection));
    }

    private readonly record struct MapsetDirectoryEntry(string DirectoryPath, string FolderName);
}

public partial class SongMapsetCardViewModel : ObservableObject, IDisposable
{
    private readonly string? _backgroundImagePath;
    private bool _backgroundLoadFailed;
    private bool _isBackgroundActive;
    private bool _isDisposed;

    public SongMapsetCardViewModel(SongMapsetInfo mapset, bool isPreferredMapset)
    {
        Artist = mapset.Artist;
        Title = mapset.Title;
        Creator = mapset.Creator;
        LastEditedUtc = mapset.LastEditUtc;
        _backgroundImagePath = mapset.BackgroundImagePath;
        IsPreferredMapset = isPreferredMapset;
        Difficulties = new ObservableCollection<SongDifficultyItemViewModel>(mapset.Difficulties
            .OrderByDescending(difficulty => difficulty.LastEditUtc)
            .Select(difficulty => new SongDifficultyItemViewModel(difficulty)));
        foreach (var difficulty in Difficulties)
        {
            difficulty.PropertyChanged += OnDifficultyPropertyChanged;
        }

    }

    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isPreferredMapset;
    public string Artist { get; }
    public string Title { get; }
    public string Creator { get; }
    public DateTime LastEditedUtc { get; }
    public string ArtistAndTitle => $"{Artist} - {Title}";
    public string CreatorLabel => $"Mapped by {Creator}";
    public string LastEditedLabel => $"Edited {LastEditedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)}";
    public string DifficultyCountLabel => Difficulties.Count == 1 ? "1 difficulty" : $"{Difficulties.Count} difficulties";
    public bool AllDifficultiesSelected => Difficulties.Count > 0 && Difficulties.All(difficulty => difficulty.IsSelected);
    public string SelectAllButtonLabel => AllDifficultiesSelected ? "Unselect All Diffs" : "Select All Diffs";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBackgroundImage))]
    private Bitmap? _backgroundImage;
    public bool HasBackgroundImage => BackgroundImage is not null;
    public ObservableCollection<SongDifficultyItemViewModel> Difficulties { get; }

    public void SetBackgroundActive(bool isActive)
    {
        if (_isDisposed || _isBackgroundActive == isActive)
        {
            return;
        }

        _isBackgroundActive = isActive;
        if (isActive)
        {
            EnsureBackgroundLoaded();
            return;
        }

        ReleaseBackground();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        ReleaseBackground();
        foreach (var difficulty in Difficulties)
        {
            difficulty.PropertyChanged -= OnDifficultyPropertyChanged;
        }

        Difficulties.Clear();
    }

    private void OnDifficultyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(SongDifficultyItemViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(AllDifficultiesSelected));
            OnPropertyChanged(nameof(SelectAllButtonLabel));
        }
    }

    private void EnsureBackgroundLoaded()
    {
        if (BackgroundImage is not null || _backgroundLoadFailed)
        {
            return;
        }

        var image = BuildBackgroundImage(_backgroundImagePath);
        if (image is null)
        {
            _backgroundLoadFailed = true;
            return;
        }

        BackgroundImage = image;
    }

    private void ReleaseBackground()
    {
        if (BackgroundImage is null)
        {
            return;
        }

        var image = BackgroundImage;
        BackgroundImage = null;
        image.Dispose();
    }

    private static Bitmap? BuildBackgroundImage(string? backgroundImagePath)
    {
        if (string.IsNullOrWhiteSpace(backgroundImagePath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(backgroundImagePath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            return new Bitmap(fullPath);
        }
        catch
        {
            return null;
        }
    }
}

public partial class SongDifficultyItemViewModel(SongDifficultyInfo difficulty) : ObservableObject
{
    [ObservableProperty] private bool _isSelected;

    public string Name { get; } = difficulty.Name;
    public string OsuFilePath { get; } = difficulty.OsuFilePath;
}
