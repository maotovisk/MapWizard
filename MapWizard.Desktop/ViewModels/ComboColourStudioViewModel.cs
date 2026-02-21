using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BeatmapParser;
using BeatmapParser.Colours;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Utils;
using MapWizard.Tools.ComboColourStudio;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using ToolComboColourPoint = MapWizard.Tools.ComboColourStudio.ComboColourPoint;

namespace MapWizard.Desktop.ViewModels;

public partial class ComboColourStudioViewModel(
    IFilesService filesService,
    IComboColourStudioService comboColourStudioService,
    IOsuMemoryReaderService osuMemoryReaderService,
    IComboColourProjectStore comboColourProjectStore,
    ISettingsService settingsService,
    ISongLibraryService songLibraryService,
    ISukiDialogManager dialogManager,
    ISukiToastManager toastManager) : ViewModelBase
{
    [NotifyPropertyChangedFor(nameof(HasOriginBeatmap))]
    [NotifyPropertyChangedFor(nameof(CanSaveProject))]
    [NotifyPropertyChangedFor(nameof(IsProjectSavedIndicatorVisible))]
    [NotifyPropertyChangedFor(nameof(IsProjectUnsavedIndicatorVisible))]
    [NotifyPropertyChangedFor(nameof(IsProjectNotSavedIndicatorVisible))]
    [ObservableProperty] private SelectedMap _originBeatmap = new();

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))]
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps = [new SelectedMap()];

    [ObservableProperty] private bool _hasMultiple;
    [ObservableProperty] private string _preferredDirectory = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasHeaderBackgroundImage))]
    [ObservableProperty] private Bitmap? _headerBackgroundImage;

    [ObservableProperty] private string _backgroundImagePath = string.Empty;
    [NotifyPropertyChangedFor(nameof(OriginContextTopLine))]
    [ObservableProperty] private string _originArtist = string.Empty;
    [NotifyPropertyChangedFor(nameof(OriginContextTopLine))]
    [ObservableProperty] private string _originSongName = string.Empty;
    [NotifyPropertyChangedFor(nameof(OriginContextBottomLine))]
    [ObservableProperty] private string _originDiffName = string.Empty;
    [ObservableProperty] private int _originBeatmapId;
    [NotifyPropertyChangedFor(nameof(IsProjectSavedIndicatorVisible))]
    [NotifyPropertyChangedFor(nameof(IsProjectUnsavedIndicatorVisible))]
    [NotifyPropertyChangedFor(nameof(IsProjectNotSavedIndicatorVisible))]
    [ObservableProperty] private bool _hasUnsavedChanges;
    [NotifyPropertyChangedFor(nameof(IsProjectSavedIndicatorVisible))]
    [NotifyPropertyChangedFor(nameof(IsProjectUnsavedIndicatorVisible))]
    [NotifyPropertyChangedFor(nameof(IsProjectNotSavedIndicatorVisible))]
    [ObservableProperty] private bool _hasLocalProjectSnapshot;

    [ObservableProperty] private int _maxBurstLength = 1;
    [ObservableProperty] private int _paletteSize = 6;

    [ObservableProperty] private ObservableCollection<AvaloniaComboColour> _comboColours = [];
    [ObservableProperty] private ObservableCollection<AvaloniaComboColourPoint> _colourPoints = [];
    [ObservableProperty] private ObservableCollection<ComboColourOption> _colourDropdownOptions = [];
    private ObservableCollection<AvaloniaComboColour>? _observedComboColours;
    private readonly HashSet<AvaloniaComboColour> _observedComboColourItems = [];
    private ObservableCollection<AvaloniaComboColourPoint>? _observedColourPoints;
    private readonly HashSet<AvaloniaComboColourPoint> _observedColourPointItems = [];
    private readonly HashSet<AvaloniaComboColourToken> _observedColourPointTokens = [];
    private bool _suppressDirtyTracking;

    public bool HasHeaderBackgroundImage => HeaderBackgroundImage is not null;
    public bool HasOriginBeatmap => !string.IsNullOrWhiteSpace(OriginBeatmap.Path);
    public bool CanSaveProject => HasOriginBeatmap;
    public bool IsProjectSavedIndicatorVisible => HasOriginBeatmap && HasLocalProjectSnapshot && !HasUnsavedChanges;
    public bool IsProjectUnsavedIndicatorVisible => HasOriginBeatmap && HasUnsavedChanges;
    public bool IsProjectNotSavedIndicatorVisible => HasOriginBeatmap && !HasUnsavedChanges && !HasLocalProjectSnapshot;
    public string OriginContextTopLine
    {
        get
        {
            var artist = string.IsNullOrWhiteSpace(OriginArtist) ? "Unknown Artist" : OriginArtist;
            var song = string.IsNullOrWhiteSpace(OriginSongName) ? "Unknown Title" : OriginSongName;
            return $"{artist} - {song}";
        }
    }
    public string OriginContextBottomLine
    {
        get
        {
            var diff = string.IsNullOrWhiteSpace(OriginDiffName) ? "Unknown Difficulty" : OriginDiffName;
            return $"[{diff}]";
        }
    }

    public ObservableCollection<SelectedMap> AdditionalBeatmaps
    {
        get => new(DestinationBeatmaps.Skip(1));
        set
        {
            var first = DestinationBeatmaps.FirstOrDefault() ?? new SelectedMap();
            DestinationBeatmaps = new ObservableCollection<SelectedMap>(new[] { first }.Concat(value));
        }
    }

    partial void OnPaletteSizeChanged(int value)
    {
        PaletteSize = value switch
        {
            < 1 => 1,
            > 8 => 8,
            _ => PaletteSize
        };
    }

    partial void OnMaxBurstLengthChanged(int value)
    {
        MarkProjectDirty();
    }

    partial void OnComboColoursChanged(ObservableCollection<AvaloniaComboColour> value)
    {
        ObserveComboColours();
    }

    partial void OnColourPointsChanged(ObservableCollection<AvaloniaComboColourPoint> value)
    {
        ObserveColourPoints();
    }

    [RelayCommand]
    private void RemoveMap(string path)
    {
        var remaining = DestinationBeatmaps.Where(x => x.Path != path).ToList();

        if (remaining.Count == 0)
        {
            remaining.Add(new SelectedMap());
        }

        DestinationBeatmaps = new ObservableCollection<SelectedMap>(remaining);
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    [RelayCommand]
    private void AddColour()
    {
        if (ComboColours.Count >= 8)
        {
            ShowToast(NotificationType.Warning, "Combo Colour Studio", "osu! supports at most 8 combo colours.");
            return;
        }

        var cloneColour = ComboColours.Count > 0
            ? ComboColours[^1].Colour ?? Color.White
            : Color.White;

        ComboColours.Add(new AvaloniaComboColour(ComboColours.Count + 1, cloneColour));
        RefreshComboColourOptions();
        NormalizeColourPointSequences();
        MarkProjectDirty();
    }

    [RelayCommand]
    private void RemoveColour(AvaloniaComboColour colour)
    {
        var index = ComboColours.IndexOf(colour);
        if (index < 0)
        {
            return;
        }

        ComboColours.RemoveAt(index);

        for (var i = 0; i < ComboColours.Count; i++)
        {
            ComboColours[i].Number = i + 1;
        }

        RefreshComboColourOptions();
        NormalizeColourPointSequences();
        MarkProjectDirty();
    }

    [RelayCommand]
    private async Task AddColourPoint()
    {
        var nextTime = await TryGetTimestampFromClipboardAsync()
                       ?? (ColourPoints.Count > 0 ? ColourPoints[^1].Time : 0);
        var point = new AvaloniaComboColourPoint
        {
            Time = nextTime,
            Mode = ColourPointMode.Normal,
            ColourSequence = []
        };

        if (ComboColours.Count > 0)
        {
            point.ColourSequence.Add(new AvaloniaComboColourToken { ComboNumber = 1 });
        }

        ColourPoints.Add(point);
        SetLatestAddedColourPoint(point);
        ObserveColourPoints();
        MarkProjectDirty();
    }

    private static async Task<double?> TryGetTimestampFromClipboardAsync()
    {
        var clipboard = TryGetClipboard();
        if (clipboard is null)
        {
            return null;
        }

        string? clipboardText;
        try
        {
            clipboardText = await clipboard.TryGetTextAsync();
        }
        catch
        {
            return null;
        }

        return MillisecondParser.TryParseMillisecondInput(clipboardText, out var timestamp) ? timestamp : null;
    }

    private static IClipboard? TryGetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return null;
        }

        var activeWindow = lifetime.Windows.FirstOrDefault(window => window.IsActive);
        return activeWindow?.Clipboard ?? lifetime.MainWindow?.Clipboard;
    }

    [RelayCommand]
    private void RemoveColourPoint(AvaloniaComboColourPoint colourPoint)
    {
        var removedWasLatest = colourPoint.IsLatestAdded;
        ColourPoints.Remove(colourPoint);
        if (removedWasLatest)
        {
            SetLatestAddedColourPoint(ColourPoints.LastOrDefault());
        }

        MarkProjectDirty();
    }

    [RelayCommand]
    private async Task AddColourPointFromSequence(AvaloniaComboColourPoint sourcePoint)
    {
        if (sourcePoint is null)
        {
            return;
        }

        var latestTime = ColourPoints.Count > 0 ? ColourPoints.Max(point => point.Time) : sourcePoint.Time;
        var nextTime = await TryGetTimestampFromClipboardAsync() ?? latestTime;
        var newPoint = new AvaloniaComboColourPoint
        {
            Time = nextTime,
            Mode = sourcePoint.Mode,
            ColourSequence = new ObservableCollection<AvaloniaComboColourToken>(
                sourcePoint.ColourSequence.Select(token => new AvaloniaComboColourToken
                {
                    ComboNumber = token.ComboNumber
                }))
        };

        ColourPoints.Add(newPoint);
        SetLatestAddedColourPoint(newPoint);
        ObserveColourPoints();
        MarkProjectDirty();
    }

    [RelayCommand]
    private void AddColourToPoint(AvaloniaComboColourPoint colourPoint)
    {
        if (ComboColours.Count == 0)
        {
            ShowToast(NotificationType.Warning, "Combo Colour Studio", "Add at least one combo colour first.");
            return;
        }

        var defaultNumber = colourPoint.ColourSequence.Count > 0
            ? Math.Clamp(colourPoint.ColourSequence[^1].ComboNumber, 1, ComboColours.Count)
            : 1;

        colourPoint.ColourSequence.Add(new AvaloniaComboColourToken
        {
            ComboNumber = defaultNumber
        });
        MarkProjectDirty();
    }

    [RelayCommand]
    private void RemoveColourFromPoint(AvaloniaComboColourToken token)
    {
        foreach (var point in ColourPoints)
        {
            if (point.ColourSequence.Remove(token))
            {
                MarkProjectDirty();
                break;
            }
        }
    }

    [RelayCommand]
    private void SaveProject()
    {
        if (!EnsureOriginSelected())
        {
            return;
        }

        try
        {
            var project = BuildProjectFromUi();
            SaveLocalProjectSnapshot(project, showSuccessToast: true);
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private void ImportComboColoursFromOrigin()
    {
        if (!EnsureOriginSelected())
        {
            return;
        }

        try
        {
            var project = comboColourStudioService.ImportComboColours(OriginBeatmap.Path);
            LoadProjectIntoUi(project, replaceColourPoints: false);

            ShowToast(NotificationType.Success, "Combo Colour Studio", "Imported combo colours from beatmap.");
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private void ImportColourHaxFromOrigin()
    {
        if (!EnsureOriginSelected())
        {
            return;
        }

        try
        {
            var maxBurstLength = Math.Max(1, MaxBurstLength);
            var project = comboColourStudioService.ExtractColourHax(OriginBeatmap.Path, maxBurstLength);
            LoadProjectIntoUi(project, replaceColourPoints: true);
            ShowToast(NotificationType.Success, "Combo Colour Studio", "Imported colour hax from beatmap.");
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private void GenerateColoursFromBackground()
    {
        if (string.IsNullOrWhiteSpace(BackgroundImagePath))
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", "No beatmap background image is available.");
            return;
        }

        GenerateColoursFromImagePath(BackgroundImagePath);
    }

    [RelayCommand]
    private async Task PickImageAndGenerateColours(CancellationToken token)
    {
        try
        {
            var preferredDirectory = await filesService.TryGetFolderFromPathAsync(
                string.IsNullOrWhiteSpace(PreferredDirectory)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : PreferredDirectory);

            var file = await filesService.OpenFileAsync(new FilePickerOpenOptions
            {
                Title = "Select an image",
                AllowMultiple = false,
                SuggestedStartLocation = preferredDirectory,
                FileTypeFilter =
                [
                    new FilePickerFileType("Image")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"]
                    }
                ]
            });

            if (token.IsCancellationRequested || file is null || file.Count == 0)
            {
                return;
            }

            GenerateColoursFromImagePath(file[0].Path.LocalPath);
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private async Task PickOriginFile(CancellationToken token)
    {
        try
        {
            var selectedPaths = await ShowSongSelectDialogAsync(allowMultiple: false, token: token);
            if (token.IsCancellationRequested || selectedPaths is null || selectedPaths.Count == 0)
            {
                return;
            }

            await SetOriginBeatmapPath(selectedPaths[0]);
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private async Task SetOriginFromMemory()
    {
        var beatmapPath = GetBeatmapFromMemory();

        if (beatmapPath is null)
        {
            return;
        }

        await SetOriginBeatmapPath(beatmapPath);
    }

    [RelayCommand]
    private async Task PickDestinationFile(CancellationToken token)
    {
        try
        {
            var selectedPaths = await ShowSongSelectDialogAsync(
                allowMultiple: true,
                token: token,
                preferredMapsetDirectoryPath: BeatmapPathUtils.TryGetMapsetDirectoryPath(OriginBeatmap.Path));
            if (token.IsCancellationRequested || selectedPaths is null || selectedPaths.Count == 0)
            {
                return;
            }

            SetDestinationBeatmaps(selectedPaths);
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private void AddDestinationFromMemory()
    {
        var beatmapPath = GetBeatmapFromMemory();
        if (beatmapPath is null)
        {
            return;
        }

        if (!BeatmapSelectionUtils.TryAppendDestinationBeatmap(DestinationBeatmaps, beatmapPath, out var destinationBeatmaps))
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", "This beatmap is already in the destination list.");
            return;
        }

        DestinationBeatmaps = destinationBeatmaps;
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    private async Task SetOriginBeatmapPath(string beatmapPath)
    {
        OriginBeatmap = new SelectedMap { Path = beatmapPath };
        PreferredDirectory = Path.GetDirectoryName(OriginBeatmap.Path) ?? string.Empty;
        await LoadOriginBeatmapContext();
    }

    private void SetDestinationBeatmaps(IReadOnlyCollection<string> beatmapPaths)
    {
        var normalizedBeatmaps = BeatmapSelectionUtils.NormalizeDestinationBeatmaps(beatmapPaths);
        if (normalizedBeatmaps.Count == 0)
        {
            return;
        }

        DestinationBeatmaps = normalizedBeatmaps;
        HasMultiple = DestinationBeatmaps.Count > 1;
        PreferredDirectory = BeatmapSelectionUtils.GetPreferredDirectoryOrFallback(DestinationBeatmaps, PreferredDirectory);
    }

    [RelayCommand]
    private void ExportColourHax()
    {
        try
        {
            var destinationPaths = DestinationBeatmaps
                .Select(x => x.Path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (destinationPaths.Length == 0)
            {
                ShowToast(NotificationType.Error, "Combo Colour Studio", "Please select at least one destination beatmap.");
                return;
            }

            var project = BuildProjectFromUi();

            comboColourStudioService.ApplyProject(project, destinationPaths, new ComboColourStudioOptions());
            SaveLocalProjectSnapshot(project, showSuccessToast: false);

            ShowToast(NotificationType.Success,
                "Combo Colour Studio",
                $"Exported colour hax to {destinationPaths.Length} beatmap(s).");
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    private void GenerateColoursFromImagePath(string imagePath)
    {
        try
        {
            var colours = comboColourStudioService.GenerateProminentColours(imagePath, PaletteSize);
            ComboColours = new ObservableCollection<AvaloniaComboColour>(colours
                .Select((colour, index) => new AvaloniaComboColour(index + 1, colour)));

            RefreshComboColourOptions();
            NormalizeColourPointSequences();
            MarkProjectDirty();
            ShowToast(NotificationType.Success, "Combo Colour Studio", "Generated combo colours from image.");
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    private bool EnsureOriginSelected()
    {
        if (!string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            return true;
        }

        ShowToast(NotificationType.Error, "Combo Colour Studio", "Please select an origin beatmap.");
        return false;
    }

    private string? GetBeatmapFromMemory()
    {
        return BeatmapSelectionUtils.TryGetBeatmapFromMemory(
            osuMemoryReaderService,
            ShowToast,
            "Memory Error",
            "Failed to read beatmap path from memory.",
            "Memory Error",
            "No beatmap is currently loaded in osu!.");
    }

    private Task<IReadOnlyList<string>?> ShowSongSelectDialogAsync(
        bool allowMultiple,
        CancellationToken token,
        string? preferredMapsetDirectoryPath = null)
        => MapPickerDialogUtils.ShowSongSelectDialogAsync(
            dialogManager,
            toastManager,
            songLibraryService,
            filesService,
            settingsService,
            "Combo Colour Studio",
            allowMultiple,
            token,
            preferredMapsetDirectoryPath);

    private async Task LoadOriginBeatmapContext()
    {
        try
        {
            var project = comboColourStudioService.ImportComboColours(OriginBeatmap.Path);
            LoadProjectIntoUi(project, replaceColourPoints: false, markAsDirty: false);
            LoadOriginBeatmapMetadata();
            LoadBackgroundImage();

            var restoreState = await TryRestoreSavedProject();
            var contextMessage = restoreState switch
            {
                SavedProjectRestoreState.Restored => "Loaded beatmap context and restored saved combo colour project.",
                SavedProjectRestoreState.FoundButIgnored => "Loaded beatmap context. A saved local project is available.",
                _ => "Loaded beatmap context."
            };

            HasLocalProjectSnapshot = restoreState != SavedProjectRestoreState.NotFound;
            HasUnsavedChanges = restoreState == SavedProjectRestoreState.FoundButIgnored;

            ShowToast(NotificationType.Success, "Combo Colour Studio", contextMessage);
        }
        catch (Exception ex)
        {
            ClearOriginBeatmapMetadata();
            HasLocalProjectSnapshot = false;
            HasUnsavedChanges = false;
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    private void LoadOriginBeatmapMetadata()
    {
        var beatmap = Beatmap.Decode(File.ReadAllText(OriginBeatmap.Path));
        var metadata = beatmap.MetadataSection;

        OriginArtist = StringValueUtils.FirstNonEmpty(metadata.ArtistUnicode, metadata.Artist);
        OriginSongName = StringValueUtils.FirstNonEmpty(metadata.TitleUnicode, metadata.Title);
        OriginDiffName = metadata.Version;
        OriginBeatmapId = metadata.BeatmapID;
    }

    private void ClearOriginBeatmapMetadata()
    {
        OriginArtist = string.Empty;
        OriginSongName = string.Empty;
        OriginDiffName = string.Empty;
        OriginBeatmapId = 0;
    }

    private bool SaveLocalProjectSnapshot(ComboColourProject project, bool showSuccessToast)
    {
        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            return false;
        }

        try
        {
            comboColourProjectStore.SaveProject(OriginBeatmap.Path, OriginBeatmapId, project);
            HasLocalProjectSnapshot = true;
            HasUnsavedChanges = false;
            if (showSuccessToast)
            {
                ShowToast(NotificationType.Success, "Combo Colour Studio", "Saved project locally.");
            }

            return true;
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Warning, "Combo Colour Studio", $"Failed to save local project snapshot: {ex.Message}");
            return false;
        }
    }

    private async Task<SavedProjectRestoreState> TryRestoreSavedProject()
    {
        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            return SavedProjectRestoreState.NotFound;
        }

        ComboColourProject? storedProject;
        try
        {
            storedProject = comboColourProjectStore.TryLoadProject(OriginBeatmap.Path, OriginBeatmapId);
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Warning, "Combo Colour Studio", $"Failed to check saved projects: {ex.Message}");
            return SavedProjectRestoreState.NotFound;
        }

        if (storedProject is null)
        {
            return SavedProjectRestoreState.NotFound;
        }

        var shouldRestore = await dialogManager.CreateDialog()
            .WithTitle("Restore Saved Project")
            .WithContent("A saved Combo Colour project was found for this beatmap. Do you want to restore it?")
            .WithYesNoResult("Restore", "Ignore")
            .TryShowAsync();

        if (!shouldRestore)
        {
            return SavedProjectRestoreState.FoundButIgnored;
        }

        LoadProjectIntoUi(storedProject, replaceColourPoints: true, markAsDirty: false);
        return SavedProjectRestoreState.Restored;
    }

    private void LoadProjectIntoUi(ComboColourProject project, bool replaceColourPoints, bool markAsDirty = true)
    {
        WithDirtyTrackingSuppressed(() =>
        {
            ComboColours = new ObservableCollection<AvaloniaComboColour>(project.ComboColours.Select((combo, index) =>
                new AvaloniaComboColour(index + 1, combo.Colour)));

            if (replaceColourPoints)
            {
                ColourPoints = new ObservableCollection<AvaloniaComboColourPoint>(project.ColourPoints.Select(point =>
                    new AvaloniaComboColourPoint
                    {
                        Time = point.Time,
                        Mode = point.Mode,
                        ColourSequence = new ObservableCollection<AvaloniaComboColourToken>(
                            point.ColourSequence.Select(index => new AvaloniaComboColourToken
                            {
                                ComboNumber = index + 1
                            }))
                    }));

                SetLatestAddedColourPoint(null);
            }

            MaxBurstLength = project.MaxBurstLength;
            RefreshComboColourOptions();
            ObserveColourPoints();
            NormalizeColourPointSequences();
        });

        if (markAsDirty)
        {
            MarkProjectDirty();
        }
    }

    private void LoadBackgroundImage()
    {
        try
        {
            var backgroundPath = comboColourStudioService.GetBackgroundPath(OriginBeatmap.Path);
            BackgroundImagePath = backgroundPath ?? string.Empty;

            HeaderBackgroundImage?.Dispose();
            HeaderBackgroundImage = null;

            if (!string.IsNullOrWhiteSpace(backgroundPath) && File.Exists(backgroundPath))
            {
                HeaderBackgroundImage = new Bitmap(backgroundPath);
            }
        }
        catch
        {
            BackgroundImagePath = string.Empty;
            HeaderBackgroundImage?.Dispose();
            HeaderBackgroundImage = null;
        }
    }

    private ComboColourProject BuildProjectFromUi()
    {
        var comboColours = ComboColours
            .Select((combo, index) => new ComboColour((uint)(index + 1), combo.Colour ?? Color.White))
            .ToList();

        if (comboColours.Count == 0)
        {
            throw new InvalidOperationException("Please add at least one combo colour.");
        }

        var colourPoints = ColourPoints
            .Select(point => new ToolComboColourPoint
            {
                Time = point.Time,
                Mode = point.Mode,
                ColourSequence = point.ColourSequence
                    .Select(token => token.ComboNumber)
                    .Select(number =>
                    {
                        if (number < 1 || number > comboColours.Count)
                        {
                            throw new InvalidOperationException(
                                $"Colour point at {point.Time:0.##}ms references combo {number}, but only {comboColours.Count} combo colours exist.");
                        }

                        return number - 1;
                    })
                    .ToList()
            })
            .OrderBy(point => point.Time)
            .ToList();

        return new ComboColourProject
        {
            ComboColours = comboColours,
            ColourPoints = colourPoints,
            MaxBurstLength = Math.Max(1, MaxBurstLength)
        };
    }

    private void NormalizeColourPointSequences()
    {
        var comboCount = ComboColours.Count;

        foreach (var point in ColourPoints)
        {
            if (comboCount == 0)
            {
                point.ColourSequence.Clear();
                continue;
            }

            foreach (var token in point.ColourSequence)
            {
                token.ComboNumber = Math.Clamp(token.ComboNumber, 1, comboCount);
            }
        }
    }

    private void SetLatestAddedColourPoint(AvaloniaComboColourPoint? latestPoint)
    {
        foreach (var point in ColourPoints)
        {
            point.IsLatestAdded = ReferenceEquals(point, latestPoint);
        }
    }

    private void RefreshComboColourOptions()
    {
        ObserveComboColours();

        ColourDropdownOptions = new ObservableCollection<ComboColourOption>(
            ComboColours.Select(combo => new ComboColourOption
            {
                Number = combo.Number,
                Colour = combo.Colour ?? Color.White
            }));
    }

    private void ObserveComboColours()
    {
        if (!ReferenceEquals(_observedComboColours, ComboColours))
        {
            if (_observedComboColours is not null)
            {
                _observedComboColours.CollectionChanged -= ComboColoursOnCollectionChanged;
            }

            foreach (var observed in _observedComboColourItems)
            {
                observed.PropertyChanged -= ComboColourOnPropertyChanged;
            }

            _observedComboColourItems.Clear();
            _observedComboColours = ComboColours;
            _observedComboColours.CollectionChanged += ComboColoursOnCollectionChanged;
        }

        foreach (var comboColour in ComboColours)
        {
            if (_observedComboColourItems.Add(comboColour))
            {
                comboColour.PropertyChanged += ComboColourOnPropertyChanged;
            }
        }

        var removedItems = _observedComboColourItems
            .Where(comboColour => !ComboColours.Contains(comboColour))
            .ToList();

        foreach (var removedItem in removedItems)
        {
            removedItem.PropertyChanged -= ComboColourOnPropertyChanged;
            _observedComboColourItems.Remove(removedItem);
        }
    }

    private void ComboColoursOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ObserveComboColours();
        RefreshComboColourOptions();
        MarkProjectDirty();
    }

    private void ComboColourOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AvaloniaComboColour.Colour) || e.PropertyName == nameof(AvaloniaComboColour.Number))
        {
            RefreshComboColourOptions();
            MarkProjectDirty();
        }
    }

    private void ObserveColourPoints()
    {
        if (!ReferenceEquals(_observedColourPoints, ColourPoints))
        {
            if (_observedColourPoints is not null)
            {
                _observedColourPoints.CollectionChanged -= ColourPointsOnCollectionChanged;
            }

            foreach (var observedPoint in _observedColourPointItems.ToList())
            {
                DetachColourPoint(observedPoint);
            }

            _observedColourPointItems.Clear();
            _observedColourPointTokens.Clear();
            _observedColourPoints = ColourPoints;
            _observedColourPoints.CollectionChanged += ColourPointsOnCollectionChanged;
        }

        foreach (var point in ColourPoints)
        {
            AttachColourPoint(point);
        }

        var removedPoints = _observedColourPointItems
            .Where(point => !ColourPoints.Contains(point))
            .ToList();

        foreach (var removedPoint in removedPoints)
        {
            DetachColourPoint(removedPoint);
        }
    }

    private void ColourPointsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<AvaloniaComboColourPoint>())
            {
                DetachColourPoint(oldItem);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<AvaloniaComboColourPoint>())
            {
                AttachColourPoint(newItem);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var observedPoint in _observedColourPointItems.ToList())
            {
                DetachColourPoint(observedPoint);
            }

            foreach (var point in ColourPoints)
            {
                AttachColourPoint(point);
            }
        }

        MarkProjectDirty();
    }

    private void AttachColourPoint(AvaloniaComboColourPoint point)
    {
        if (!_observedColourPointItems.Add(point))
        {
            return;
        }

        point.PropertyChanged += ColourPointOnPropertyChanged;
        point.ColourSequence.CollectionChanged += ColourSequenceOnCollectionChanged;
        foreach (var token in point.ColourSequence)
        {
            AttachColourPointToken(token);
        }
    }

    private void DetachColourPoint(AvaloniaComboColourPoint point)
    {
        if (!_observedColourPointItems.Remove(point))
        {
            return;
        }

        point.PropertyChanged -= ColourPointOnPropertyChanged;
        point.ColourSequence.CollectionChanged -= ColourSequenceOnCollectionChanged;
        foreach (var token in point.ColourSequence)
        {
            DetachColourPointToken(token);
        }
    }

    private void ColourPointOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AvaloniaComboColourPoint.Time) ||
            e.PropertyName == nameof(AvaloniaComboColourPoint.Mode))
        {
            MarkProjectDirty();
        }
    }

    private void ColourSequenceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldToken in e.OldItems.OfType<AvaloniaComboColourToken>())
            {
                DetachColourPointToken(oldToken);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var newToken in e.NewItems.OfType<AvaloniaComboColourToken>())
            {
                AttachColourPointToken(newToken);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var token in _observedColourPointTokens.ToList())
            {
                token.PropertyChanged -= ColourPointTokenOnPropertyChanged;
            }

            _observedColourPointTokens.Clear();
            foreach (var point in _observedColourPointItems)
            {
                foreach (var token in point.ColourSequence)
                {
                    AttachColourPointToken(token);
                }
            }
        }

        MarkProjectDirty();
    }

    private void AttachColourPointToken(AvaloniaComboColourToken token)
    {
        if (_observedColourPointTokens.Add(token))
        {
            token.PropertyChanged += ColourPointTokenOnPropertyChanged;
        }
    }

    private void DetachColourPointToken(AvaloniaComboColourToken token)
    {
        if (_observedColourPointTokens.Remove(token))
        {
            token.PropertyChanged -= ColourPointTokenOnPropertyChanged;
        }
    }

    private void ColourPointTokenOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AvaloniaComboColourToken.ComboNumber))
        {
            MarkProjectDirty();
        }
    }

    private void WithDirtyTrackingSuppressed(Action action)
    {
        var previousState = _suppressDirtyTracking;
        _suppressDirtyTracking = true;
        try
        {
            action();
        }
        finally
        {
            _suppressDirtyTracking = previousState;
        }
    }

    private void MarkProjectDirty()
    {
        if (_suppressDirtyTracking || !HasOriginBeatmap)
        {
            return;
        }

        HasUnsavedChanges = true;
    }

    private enum SavedProjectRestoreState
    {
        NotFound,
        FoundButIgnored,
        Restored
    }

    private void ShowToast(NotificationType type, string title, string message)
    {
        toastManager.ShowToast(type, title, message);
    }
}
