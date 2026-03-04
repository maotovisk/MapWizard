using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using BeatmapParser.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Models.HitSoundVisualizer;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Services.HitsoundService;
using MapWizard.Desktop.Services.MemoryService;
using MapWizard.Desktop.Services.Playback;
using MapWizard.Desktop.Utils;
using MapWizard.Tools.HitSounds.Event;
using MapWizard.Tools.HitSounds.Timeline;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MapWizard.Desktop.ViewModels;

public partial class HitSoundVisualizerViewModel(
    IFilesService filesService,
    IHitSoundService hitSoundService,
    IAudioPlaybackService audioPlaybackService,
    IOsuMemoryReaderService osuMemoryReaderService,
    ISettingsService settingsService,
    ISongLibraryService songLibraryService,
    ISukiDialogManager dialogManager,
    ISukiToastManager toastManager) : ViewModelBase
{
    public event Action? FocusPlaybackRequested;

    private const string PointClipboardFormatId = "MapWizard.HitSoundVisualizer.Points/v1";
    private const int PlaybackDebugInfoUpdateIntervalMs = 250;
    private const int CursorColumnToleranceMs = 2;
    private const int HitsoundDebugActiveWindowMs = 200;
    private readonly string[] _sampleExtensions = [".wav", ".ogg", ".mp3"];
    private readonly ConcurrentDictionary<string, string> _resolvedPlaybackSamplePathCache =
        new(StringComparer.OrdinalIgnoreCase);
    private int _nextPointId = 1;
    private string _loadedMapsetDirectoryPath = string.Empty;
    private string _loadedAudioFilePath = string.Empty;
    private string _legacySkinDirectoryPath = string.Empty;
    private HitSoundTimeline _workingTimeline = new();
    private bool _suppressHeaderBankQuickApply;
    private PlaybackRunner? _playbackRunner;
    private long _playbackDebugStatusLastUiTickMs;
    private int[] _sortedPointTimeCache = [];
    private readonly Stack<TimelineHistoryState> _undoStack = new();
    private readonly Stack<TimelineHistoryState> _redoStack = new();
    private bool _isRestoringHistory;
    private int _lastNonZeroSongVolumePercent = Math.Max(1, LoadPersistedSongVolumePercent(settingsService));
    private int _lastNonZeroHitSoundVolumePercent = Math.Max(1, LoadPersistedHitSoundVolumePercent(settingsService));
    private readonly object _hitsoundDebugSync = new();
    private readonly Queue<long> _hitsoundDebugRecentSuccessTickMs = new();
    private long _hitsoundDebugAttemptCount;
    private long _hitsoundDebugSuccessCount;
    private long _hitsoundDebugFailureCount;
    private int _hitsoundDebugLastPointTimeMs;
    private int _hitsoundDebugLastResolvedIndex = 1;
    private int _hitsoundDebugLastResolvedVolume = 100;
    private bool _hitsoundDebugLastUsedPointIndexOverride;
    private bool _hitsoundDebugLastUsedPointVolumeOverride;
    private string _hitsoundDebugLastSampleName = "-";

    [ObservableProperty] private SelectedMap _originBeatmap = new();
    // Kept for BeatmapSelectionPanel binding compatibility (destination section is hidden in this view).
    [ObservableProperty] private bool _hasMultiple;

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))]
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps = [];

    public ObservableCollection<SelectedMap> AdditionalBeatmaps
    {
        get => BeatmapPanelViewModelUtils.GetAdditionalBeatmaps(DestinationBeatmaps);
        set => DestinationBeatmaps = BeatmapPanelViewModelUtils.MergeWithAdditionalBeatmaps(DestinationBeatmaps, value);
    }

    [ObservableProperty] private string _preferredDirectory = string.Empty;
    [ObservableProperty] private string _loadedMapTitle = string.Empty;
    [ObservableProperty] private bool _hasLoadedMap;
    [ObservableProperty] private bool _hasLoadedSongAudio;
    [ObservableProperty] private double _timelineEndMs = 1000;
    [ObservableProperty] private double _viewStartMs;
    [ObservableProperty] private double _viewWindowMs = 8000;
    [ObservableProperty] private int _cursorTimeMs;
    [ObservableProperty] private int _selectedPointId = -1;
    [ObservableProperty] private string _selectedSampleSetName = "Normal";
    [ObservableProperty] private string _selectedHitSoundName = "Hitnormal";
    [ObservableProperty] private bool _newPointIsSliderBody;
    [ObservableProperty] private int _contextSamplePointTimeMs;
    [ObservableProperty] private string _contextSampleSetName = "Normal";
    [ObservableProperty] private int _contextSampleIndex = 1;
    [ObservableProperty] private int _contextSampleVolume = 100;
    [ObservableProperty] private bool _contextSamplePointExists;
    [ObservableProperty] private string _contextSamplePointSummary = "Right-click the timeline to edit a sample point.";
    [ObservableProperty] private bool _isSampleRowContextActive;
    [ObservableProperty] private string _headerNormalBankName = "Auto";
    [ObservableProperty] private string _headerAdditionBankName = "Auto";
    [ObservableProperty] private string _hsSelectorNormalBankFilterName = "Any";
    [ObservableProperty] private string _hsSelectorAdditionBankFilterName = "Any";
    [ObservableProperty] private bool _hsSelectorIncludeHitNormal = true;
    [ObservableProperty] private bool _hsSelectorIncludeWhistle = true;
    [ObservableProperty] private bool _hsSelectorIncludeFinish = true;
    [ObservableProperty] private bool _hsSelectorIncludeClap = true;
    [ObservableProperty] private string _timelineStats = "No beatmap loaded.";
    [ObservableProperty] private string _playbackStatus = "Idle";
    [ObservableProperty] private string _timingDebugStatus = "Timing: playback idle";
    [ObservableProperty] private string _audioDebugStatus = "AudioDbg: no song";
    [ObservableProperty] private string _hitsoundDebugStatus = "HSDbg: idle";
    [ObservableProperty] private string _selectedPointSummary = "No point selected.";
    [ObservableProperty] private bool _isPlaybackRunning;
    [ObservableProperty] private bool _isPlaybackPaused;
    [ObservableProperty] private bool _followPlaybackCursor = true;
    [ObservableProperty] private bool _isTimelinePeekActive;
    [ObservableProperty] private int _playbackSeekMs;
    [ObservableProperty] private bool _isPreparingPlayback;
    [ObservableProperty] private int _songVolumePercent = LoadPersistedSongVolumePercent(settingsService);
    [ObservableProperty] private int _hitSoundVolumePercent = LoadPersistedHitSoundVolumePercent(settingsService);
    [ObservableProperty] private int _selectedSnapDivisorDenominator = 4;
    [ObservableProperty] private ObservableCollection<int> _selectedPointIds = [];

    [ObservableProperty] private ObservableCollection<HitSoundVisualizerPoint> _points = [];
    [ObservableProperty] private ObservableCollection<HitSoundVisualizerSampleChange> _sampleChanges = [];
    [ObservableProperty] private ObservableCollection<HitSoundVisualizerSnapTick> _snapTicks = [];
    [ObservableProperty] private ObservableCollection<string> _timelineRowLabels =
    [
        "Sample changes",
        "hitnormal",
        "hitwhistle",
        "hitfinish",
        "hitclap"
    ];

    public IReadOnlyList<string> PointSampleSetNames { get; } = ["Auto", "Normal", "Soft", "Drum"];
    public IReadOnlyList<string> SampleSetNames { get; } = ["Normal", "Soft", "Drum"];
    public IReadOnlyList<string> HeaderBankNames { get; } = ["Auto", "Normal", "Soft", "Drum"];
    public IReadOnlyList<string> SelectorBankFilterNames { get; } = ["Any", "Auto", "Normal", "Soft", "Drum"];
    public IReadOnlyList<string> HitSoundNames { get; } = ["Hitnormal", "Whistle", "Finish", "Clap"];
    public IReadOnlyList<int> SnapDivisorOptions { get; } = Enumerable.Range(1, 16).ToList();

    public double ViewEndMs => Math.Min(TimelineEndMs, ViewStartMs + Math.Max(100, ViewWindowMs));
    public int VisiblePointCount => CountVisiblePointsCached(ViewStartMs, ViewEndMs);
    public bool HasSelectedPoint => SelectedPointId >= 0;
    public int SelectedPointCount => SelectedPointIds.Count;
    public string CursorTimeText => FormatTimeLabel(CursorTimeMs);
    public string TimelineEndText => FormatTimeLabel((int)Math.Round(TimelineEndMs));
    public string PlaybackButtonText => IsPlaybackRunning ? "Pause" : (IsPlaybackPaused ? "Resume" : "Play");
    public MaterialIconKind PlaybackButtonIconKind => IsPlaybackRunning
        ? MaterialIconKind.Pause
        : MaterialIconKind.Play;
    public string AudioSourceStatus => !HasLoadedSongAudio || string.IsNullOrWhiteSpace(_loadedAudioFilePath)
        ? "Song audio unavailable (playback disabled)."
        : $"Song: {Path.GetFileName(_loadedAudioFilePath)}";
    public bool ShowPlaybackTimeline => HasLoadedMap && HasLoadedSongAudio;
    public bool ShowPlaybackTimelineSection => ShowPlaybackTimeline || IsPreparingPlayback;
    public bool ShowDebugInfoCard => Debugger.IsAttached;
    public string LegacySkinStatus => string.IsNullOrWhiteSpace(_legacySkinDirectoryPath)
        ? "Legacy fallback skin: not found (place osu-resources legacy skin locally)."
        : $"Legacy fallback skin: {Path.GetFileName(_legacySkinDirectoryPath)}";
    public string SongVolumeText => $"{Math.Clamp(SongVolumePercent, 0, 100)}%";
    public string HitSoundVolumeText => $"{Math.Clamp(HitSoundVolumePercent, 0, 100)}%";
    public MaterialIconKind SongVolumeIconKind => SongVolumePercent <= 0 ? MaterialIconKind.MusicNoteOff : MaterialIconKind.MusicNote;
    public MaterialIconKind EffectsVolumeIconKind => HitSoundVolumePercent <= 0 ? MaterialIconKind.VolumeOff : MaterialIconKind.VolumeHigh;
    public string SelectedSnapDivisorText => $"1/{SelectedSnapDivisorDenominator}";
    public string ContextSamplePointTimeText => FormatTimeLabel(ContextSamplePointTimeMs);
    public bool ShowSamplePointContextPopup => IsSampleRowContextActive;
    public bool ShowHitsoundContextPopup => !IsSampleRowContextActive;
    public bool CanEditHeaderBanks => HasLoadedMap;
    public bool HasAnyHsSelectorAudioTypeEnabled =>
        HsSelectorIncludeHitNormal || HsSelectorIncludeWhistle || HsSelectorIncludeFinish || HsSelectorIncludeClap;
    public bool IsHitNormalSelected
    {
        get => string.Equals(SelectedHitSoundName, "Hitnormal", StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SelectedHitSoundName = "Hitnormal";
            }
        }
    }
    public bool IsHitWhistleSelected
    {
        get => string.Equals(SelectedHitSoundName, "Whistle", StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SelectedHitSoundName = "Whistle";
            }
        }
    }
    public bool IsHitFinishSelected
    {
        get => string.Equals(SelectedHitSoundName, "Finish", StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SelectedHitSoundName = "Finish";
            }
        }
    }
    public bool IsHitClapSelected
    {
        get => string.Equals(SelectedHitSoundName, "Clap", StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                SelectedHitSoundName = "Clap";
            }
        }
    }

    private PlaybackRunner PlaybackRunner => _playbackRunner ??= new(audioPlaybackService);

    partial void OnViewStartMsChanged(double value)
    {
        NormalizeViewWindow();
        OnPropertyChanged(nameof(ViewEndMs));
        OnPropertyChanged(nameof(VisiblePointCount));
        UpdateTimelineStats();
    }

    partial void OnViewWindowMsChanged(double value)
    {
        NormalizeViewWindow();
        OnPropertyChanged(nameof(ViewEndMs));
        OnPropertyChanged(nameof(VisiblePointCount));
        UpdateTimelineStats();
    }

    partial void OnTimelineEndMsChanged(double value)
    {
        NormalizeViewWindow();
        OnPropertyChanged(nameof(ViewEndMs));
        OnPropertyChanged(nameof(VisiblePointCount));
        OnPropertyChanged(nameof(TimelineEndText));
        UpdateTimelineStats();
    }

    partial void OnPointsChanged(ObservableCollection<HitSoundVisualizerPoint> value)
    {
        RebuildPointTimeCache(value);
        OnPropertyChanged(nameof(VisiblePointCount));
    }

    partial void OnCursorTimeMsChanged(int value)
    {
        OnPropertyChanged(nameof(CursorTimeText));
        PlaybackSeekMs = value;
        if (!IsPlaybackRunning && !HasSelectedPoint && !IsSampleRowContextActive)
        {
            RefreshHeaderBankQuickState();
        }
    }

    partial void OnSelectedPointIdChanged(int value)
    {
        OnPropertyChanged(nameof(HasSelectedPoint));
        OnPropertyChanged(nameof(CanEditHeaderBanks));
    }

    partial void OnSelectedPointIdsChanged(ObservableCollection<int> value)
    {
        OnPropertyChanged(nameof(SelectedPointCount));
        OnPropertyChanged(nameof(HasSelectedPoint));
        OnPropertyChanged(nameof(CanEditHeaderBanks));
    }

    partial void OnHasLoadedMapChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditHeaderBanks));
        OnPropertyChanged(nameof(ShowPlaybackTimeline));
        OnPropertyChanged(nameof(ShowPlaybackTimelineSection));
    }

    partial void OnHasLoadedSongAudioChanged(bool value)
    {
        OnPropertyChanged(nameof(AudioSourceStatus));
        OnPropertyChanged(nameof(ShowPlaybackTimeline));
        OnPropertyChanged(nameof(ShowPlaybackTimelineSection));
    }

    partial void OnIsPlaybackPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(PlaybackButtonText));
        OnPropertyChanged(nameof(PlaybackButtonIconKind));
        RefreshTimingDebugStatus(force: true);
    }

    partial void OnIsPlaybackRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(PlaybackButtonText));
        OnPropertyChanged(nameof(PlaybackButtonIconKind));
        RefreshTimingDebugStatus(force: true);
    }

    partial void OnSongVolumePercentChanged(int value)
    {
        SongVolumePercent = Math.Clamp(value, 0, 100);
        if (SongVolumePercent > 0)
        {
            _lastNonZeroSongVolumePercent = SongVolumePercent;
        }

        audioPlaybackService.SetSongVolume(SongVolumePercent / 100f);
        PersistVisualizerVolumeSettings();
        OnPropertyChanged(nameof(SongVolumeText));
        OnPropertyChanged(nameof(SongVolumeIconKind));
    }

    partial void OnHitSoundVolumePercentChanged(int value)
    {
        HitSoundVolumePercent = Math.Clamp(value, 0, 100);
        if (HitSoundVolumePercent > 0)
        {
            _lastNonZeroHitSoundVolumePercent = HitSoundVolumePercent;
        }

        audioPlaybackService.SetHitsoundVolume(HitSoundVolumePercent / 100f);
        PersistVisualizerVolumeSettings();
        OnPropertyChanged(nameof(HitSoundVolumeText));
        OnPropertyChanged(nameof(EffectsVolumeIconKind));
    }

    partial void OnIsPreparingPlaybackChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowPlaybackTimelineSection));
    }

    partial void OnSelectedSnapDivisorDenominatorChanged(int value)
    {
        SelectedSnapDivisorDenominator = Math.Clamp(value, 1, 16);
        OnPropertyChanged(nameof(SelectedSnapDivisorText));
    }

    partial void OnSelectedHitSoundNameChanged(string value)
    {
        OnPropertyChanged(nameof(IsHitNormalSelected));
        OnPropertyChanged(nameof(IsHitWhistleSelected));
        OnPropertyChanged(nameof(IsHitFinishSelected));
        OnPropertyChanged(nameof(IsHitClapSelected));
    }

    partial void OnHsSelectorIncludeHitNormalChanged(bool value) => OnPropertyChanged(nameof(HasAnyHsSelectorAudioTypeEnabled));
    partial void OnHsSelectorIncludeWhistleChanged(bool value) => OnPropertyChanged(nameof(HasAnyHsSelectorAudioTypeEnabled));
    partial void OnHsSelectorIncludeFinishChanged(bool value) => OnPropertyChanged(nameof(HasAnyHsSelectorAudioTypeEnabled));
    partial void OnHsSelectorIncludeClapChanged(bool value) => OnPropertyChanged(nameof(HasAnyHsSelectorAudioTypeEnabled));

    partial void OnContextSamplePointTimeMsChanged(int value)
    {
        var clamped = Math.Max(0, value);
        if (clamped != value)
        {
            ContextSamplePointTimeMs = clamped;
            return;
        }

        ContextSamplePointExists = SampleChanges.Any(x => x.TimeMs == clamped);
        UpdateContextSamplePointSummary();
        OnPropertyChanged(nameof(ContextSamplePointTimeText));
    }

    partial void OnContextSampleIndexChanged(int value)
    {
        ContextSampleIndex = Math.Clamp(value, 1, 99);
    }

    partial void OnContextSampleVolumeChanged(int value)
    {
        ContextSampleVolume = Math.Clamp(value, 0, 100);
    }

    partial void OnIsSampleRowContextActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowSamplePointContextPopup));
        OnPropertyChanged(nameof(ShowHitsoundContextPopup));
        OnPropertyChanged(nameof(CanEditHeaderBanks));
        RefreshHeaderBankQuickState();
    }

    partial void OnHeaderNormalBankNameChanged(string value)
    {
        ApplyHeaderBankQuickChange(isNormalBank: true, value);
    }

    partial void OnHeaderAdditionBankNameChanged(string value)
    {
        ApplyHeaderBankQuickChange(isNormalBank: false, value);
    }

    [RelayCommand]
    private void SetSnapDivisor(int denominator)
    {
        SelectedSnapDivisorDenominator = Math.Clamp(denominator, 1, 16);
    }

    [RelayCommand]
    private async Task PickOriginFile(CancellationToken token)
    {
        try
        {
            var selectedPaths = await ShowSongSelectDialogAsync(false, token);
            if (token.IsCancellationRequested || selectedPaths is null || selectedPaths.Count == 0)
            {
                return;
            }

            SetOriginBeatmapPath(selectedPaths[0]);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", ex.Message);
        }
    }

    [RelayCommand]
    private void RemoveMap(string _)
    {
    }

    [RelayCommand]
    private void SelectOriginMap(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            string.Equals(OriginBeatmap.Path, path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SetOriginBeatmapPath(path);
    }

    [RelayCommand]
    private Task PickDestinationFile(CancellationToken _)
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void AddDestinationFromMemory()
    {
    }

    [RelayCommand]
    private void OpenOriginFolder()
    {
        if (BeatmapSelectionUtils.TryOpenBeatmapFolder(OriginBeatmap.Path, out var errorMessage))
        {
            return;
        }

        toastManager.ShowToast(
            NotificationType.Warning,
            "Hitsound Visualizer",
            string.IsNullOrWhiteSpace(errorMessage)
                ? "Unable to open the origin beatmap folder."
                : errorMessage);
    }

    [RelayCommand]
    private void SetOriginFromMemory()
    {
        var currentBeatmap = GetBeatmapFromMemory();
        if (currentBeatmap is null)
        {
            return;
        }

        SetOriginBeatmapPath(currentBeatmap);
    }

    [RelayCommand]
    private Task LoadTimeline()
    {
        return LoadTimelineCore(preservePlaybackPosition: false);
    }

    private async Task LoadTimelineCore(bool preservePlaybackPosition)
    {
        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Please select a beatmap first.");
            return;
        }

        var preservedPlaybackTimeMs = preservePlaybackPosition
            ? CapturePlaybackPositionMsForReload()
            : 0;

        IsPreparingPlayback = true;
        PlaybackStatus = "Loading...";
        HasLoadedMap = false;
        HasLoadedSongAudio = false;
        await Task.Yield();

        try
        {
            RefreshPersistedPlaybackVolumes();
            StopPlaybackCore(resetPausedState: true);
            var document = hitSoundService.LoadHitsoundVisualizerDocument(OriginBeatmap.Path);

            _loadedMapsetDirectoryPath = document.MapsetDirectoryPath;
            _loadedAudioFilePath = document.AudioFilePath;
            _legacySkinDirectoryPath = ResolveLegacySkinDirectory();
            _resolvedPlaybackSamplePathCache.Clear();
            _workingTimeline = document.Timeline;
            _workingTimeline.DraggableSoundTimeline = new SoundTimeline();
            _nextPointId = Math.Max(1, document.Points.Any() ? document.Points.Max(x => x.Id) + 1 : 1);

            Points = new ObservableCollection<HitSoundVisualizerPoint>(
                document.Points
                    .Where(x => !x.IsDraggable)
                    .OrderBy(x => x.TimeMs)
                    .ThenBy(x => (int)x.HitSound));
            SampleChanges = new ObservableCollection<HitSoundVisualizerSampleChange>(document.SampleChanges.OrderBy(x => x.TimeMs));

            var songLoaded = audioPlaybackService.LoadSong(_loadedAudioFilePath);
            var loadedSongDurationMs = songLoaded ? audioPlaybackService.GetLoadedSongDurationMs() : 0;

            LoadedMapTitle = document.DisplayTitle;
            TimelineEndMs = Math.Max(1000, Math.Max(document.EndTimeMs, loadedSongDurationMs));
            SnapTicks = new ObservableCollection<HitSoundVisualizerSnapTick>(
                hitSoundService.BuildHitsoundVisualizerSnapTicks(OriginBeatmap.Path, TimelineEndMs));
            ViewWindowMs = Math.Min(12000, TimelineEndMs);
            var restoredPlaybackTimeMs = preservePlaybackPosition
                ? Math.Clamp(preservedPlaybackTimeMs, 0, (int)Math.Ceiling(TimelineEndMs))
                : 0;
            var playbackWindowMs = Math.Max(100, ViewWindowMs);
            ViewStartMs = preservePlaybackPosition
                ? Math.Clamp(restoredPlaybackTimeMs - (playbackWindowMs * 0.35d), 0, Math.Max(0, TimelineEndMs - playbackWindowMs))
                : 0;
            CursorTimeMs = restoredPlaybackTimeMs;
            ContextSamplePointTimeMs = restoredPlaybackTimeMs;
            PlaybackSeekMs = restoredPlaybackTimeMs;
            SelectedPointId = -1;
            SelectedPointIds = [];
            ContextSampleSetName = "Normal";
            ContextSampleIndex = 1;
            ContextSampleVolume = 100;
            ContextSamplePointExists = false;
            IsSampleRowContextActive = false;
            HeaderNormalBankName = "Auto";
            HeaderAdditionBankName = "Auto";
            UpdateContextSamplePointSummary();
            HasLoadedSongAudio = songLoaded;
            HasLoadedMap = true;
            _undoStack.Clear();
            _redoStack.Clear();

            audioPlaybackService.SetSongVolume(SongVolumePercent / 100f);
            audioPlaybackService.SetHitsoundVolume(HitSoundVolumePercent / 100f);

            UpdateTimelineStats();
            UpdateSelectedPointSummary();
            OnPropertyChanged(nameof(LegacySkinStatus));
            OnPropertyChanged(nameof(TimelineEndText));
            PlaybackStatus = "Loaded";
            FocusPlaybackRequested?.Invoke();
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", ex.Message);
            PlaybackStatus = "Idle";
        }
        finally
        {
            IsPreparingPlayback = false;
        }
    }

    public void RefreshPersistedPlaybackVolumes()
    {
        SongVolumePercent = LoadPersistedSongVolumePercent(settingsService);
        HitSoundVolumePercent = LoadPersistedHitSoundVolumePercent(settingsService);
    }

    [RelayCommand]
    private void SelectTimelinePoint(int pointId)
    {
        IsSampleRowContextActive = false;
        ApplySelection([pointId], primaryPointId: pointId);
        var point = Points.FirstOrDefault(x => x.Id == pointId);
        if (point is null)
        {
            UpdateSelectedPointSummary();
            return;
        }

        CursorTimeMs = point.TimeMs;
        SyncEditorFromPoint(point);
        UpdateSelectedPointSummary();
        RefreshHeaderBankQuickState();
    }

    [RelayCommand]
    private void SelectTimelinePoints(IReadOnlyList<int>? pointIds)
    {
        IsSampleRowContextActive = false;
        var ids = pointIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
        {
            ApplySelection([], primaryPointId: -1);
            UpdateSelectedPointSummary();
            RefreshHeaderBankQuickState();
            return;
        }

        var primaryId = ids[0];
        var primaryPoint = Points.FirstOrDefault(x => x.Id == primaryId);
        ApplySelection(ids, primaryPointId: primaryId);
        if (primaryPoint is not null)
        {
            CursorTimeMs = primaryPoint.TimeMs;
        }

        UpdateSelectedPointSummary();
        if (primaryPoint is not null)
        {
            SyncEditorFromPoint(primaryPoint);
        }
        RefreshHeaderBankQuickState();
    }

    [RelayCommand]
    private void SelectAllPoints()
    {
        if (!HasLoadedMap || Points.Count == 0)
        {
            return;
        }

        IsSampleRowContextActive = false;
        var ids = Points
            .OrderBy(point => point.TimeMs)
            .ThenBy(point => HitSoundSortOrder(point.HitSound))
            .ThenBy(point => SampleSetSortOrder(point.SampleSet))
            .Select(point => point.Id)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        var primaryPoint = Points.FirstOrDefault(point => point.Id == SelectedPointId)
            ?? Points.FirstOrDefault(point => point.Id == ids[0]);
        var primaryId = primaryPoint?.Id ?? ids[0];

        ApplySelection(ids, primaryId);
        if (primaryPoint is not null)
        {
            CursorTimeMs = primaryPoint.TimeMs;
            SyncEditorFromPoint(primaryPoint);
        }

        UpdateSelectedPointSummary();
        RefreshHeaderBankQuickState();
    }

    [RelayCommand]
    private void AddTimelinePointsToSelection(IReadOnlyList<int>? pointIds)
    {
        IsSampleRowContextActive = false;
        var idsToAdd = pointIds?.Where(id => id > 0).Distinct().ToList() ?? [];
        if (idsToAdd.Count == 0)
        {
            return;
        }

        var merged = SelectedPointIds
            .Concat(idsToAdd)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var primaryId = SelectedPointId > 0 ? SelectedPointId : idsToAdd[0];
        ApplySelection(merged, primaryPointId: primaryId);

        var primaryPoint = Points.FirstOrDefault(x => x.Id == primaryId) ?? Points.FirstOrDefault(x => x.Id == idsToAdd[0]);
        if (primaryPoint is not null)
        {
            CursorTimeMs = primaryPoint.TimeMs;
            SyncEditorFromPoint(primaryPoint);
        }

        UpdateSelectedPointSummary();
        RefreshHeaderBankQuickState();
    }

    [RelayCommand]
    private void ToggleTimelinePointSelection(int pointId)
    {
        IsSampleRowContextActive = false;
        if (pointId <= 0)
        {
            return;
        }

        var point = Points.FirstOrDefault(x => x.Id == pointId);
        if (point is null)
        {
            return;
        }

        var current = SelectedPointIds.Where(id => id > 0).Distinct().ToList();
        if (current.Contains(pointId))
        {
            var remaining = current.Where(id => id != pointId).ToList();
            var nextPrimary = pointId == SelectedPointId ? remaining.FirstOrDefault() : SelectedPointId;
            ApplySelection(remaining, primaryPointId: nextPrimary);

            var nextPoint = Points.FirstOrDefault(x => x.Id == SelectedPointId);
            if (nextPoint is not null)
            {
                SyncEditorFromPoint(nextPoint);
            }

            UpdateSelectedPointSummary();
            RefreshHeaderBankQuickState();
            return;
        }

        current.Add(pointId);
        ApplySelection(current, primaryPointId: pointId);
        CursorTimeMs = point.TimeMs;
        SyncEditorFromPoint(point);
        UpdateSelectedPointSummary();
        RefreshHeaderBankQuickState();
    }

    [RelayCommand]
    private void PrepareTimelineContext(HitSoundTimelineContextRequest? request)
    {
        if (!HasLoadedMap || request is null)
        {
            return;
        }

        var clampedTime = Math.Clamp(request.TimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        CursorTimeMs = clampedTime;

        if (request.IsSampleRow)
        {
            IsSampleRowContextActive = true;
            ApplySelection([], primaryPointId: -1);
            UpdateSelectedPointSummary();
        }
        else
        {
            IsSampleRowContextActive = false;
        }

        if (!request.IsSampleRow && request.PointId > 0)
        {
            var point = Points.FirstOrDefault(x => x.Id == request.PointId);
            if (point is not null)
            {
                SyncEditorFromPoint(point);
            }
        }

        var samplePointTime = Math.Clamp(request.SampleChangeTimeMs ?? clampedTime, 0, (int)Math.Ceiling(TimelineEndMs));
        SyncContextSamplePointEditor(samplePointTime);
        RefreshHeaderBankQuickState();
    }

    [RelayCommand]
    private void SeekTime(int timeMs)
    {
        var clamped = Math.Clamp(timeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        CursorTimeMs = clamped;
        ContextSamplePointTimeMs = clamped;

        if (IsPlaybackRunning)
        {
            RestartPlaybackAt(clamped);
        }
    }

    [RelayCommand]
    private void CenterViewOnCursor()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        var halfWindow = Math.Max(100, ViewWindowMs) / 2d;
        ViewStartMs = Math.Clamp(CursorTimeMs - halfWindow, 0, Math.Max(0, TimelineEndMs - Math.Max(100, ViewWindowMs)));
    }

    [RelayCommand]
    private void ZoomToSelection()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        ViewWindowMs = Math.Clamp(ViewWindowMs / 2d, 500, Math.Max(500, TimelineEndMs));
        CenterViewOnCursor();
    }

    [RelayCommand]
    private void ZoomOut()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        ViewWindowMs = Math.Clamp(ViewWindowMs * 2d, 500, Math.Max(500, TimelineEndMs));
        CenterViewOnCursor();
    }

    [RelayCommand]
    private void PanTimeline(double deltaMs)
    {
        if (!HasLoadedMap)
        {
            return;
        }

        ViewStartMs = Math.Clamp(ViewStartMs + deltaMs, 0, Math.Max(0, TimelineEndMs - Math.Max(100, ViewWindowMs)));
    }

    [RelayCommand]
    private void ZoomTimeline(HitSoundTimelineZoomRequest? request)
    {
        if (!HasLoadedMap || request is null || request.ZoomFactor <= 0)
        {
            return;
        }

        var oldWindow = Math.Max(100, ViewWindowMs);
        var newWindow = Math.Clamp(oldWindow * request.ZoomFactor, 200, Math.Max(200, TimelineEndMs));
        var anchorTime = Math.Clamp(request.AnchorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        var anchorRatio = oldWindow <= 0 ? 0 : (anchorTime - ViewStartMs) / oldWindow;

        ViewWindowMs = newWindow;
        ViewStartMs = Math.Clamp(anchorTime - (anchorRatio * newWindow), 0, Math.Max(0, TimelineEndMs - newWindow));
    }

    [RelayCommand]
    private void AddPoint()
    {
        if (!HasLoadedMap)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Load a beatmap first.");
            return;
        }

        var timeMs = Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        var hitSound = ParseHitSound(SelectedHitSoundName);
        var usesAutoSampleSet = IsAutoSampleSetDisplay(SelectedSampleSetName);
        var sampleSet = usesAutoSampleSet
            ? ResolveAutoSampleSetForPointAtTime(hitSound, timeMs)
            : ParseSampleSet(SelectedSampleSetName);

        var point = new HitSoundVisualizerPoint
        {
            Id = _nextPointId++,
            TimeMs = timeMs,
            SampleSet = sampleSet,
            HitSound = hitSound,
            IsAutoSampleSet = usesAutoSampleSet,
            IsDraggable = false
        };

        if (HasPointConflict(point, out var conflictMessage))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", conflictMessage);
            return;
        }

        var updated = Points.ToList();
        updated.Add(point);
        ApplyUpdatedPoints(updated, selectPointId: point.Id);
    }

    [RelayCommand]
    private void AddTimelinePointOnSnapLine(HitSoundTimelineRowAddRequest? request)
    {
        if (!HasLoadedMap || request is null)
        {
            return;
        }

        if (!TryGetHitSoundFromRowIndex(request.RowIndex, out var hitSound))
        {
            return;
        }

        var timeMs = Math.Clamp(request.TimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        var normalBank = ResolveHeaderBankAtTime(isNormalBank: true, timeMs);
        var additionBank = ResolveHeaderBankAtTime(isNormalBank: false, timeMs, normalBank);
        var sampleSet = hitSound == HitSound.Normal ? normalBank : additionBank;

        var point = new HitSoundVisualizerPoint
        {
            Id = _nextPointId++,
            TimeMs = timeMs,
            SampleSet = sampleSet,
            HitSound = hitSound,
            IsDraggable = false
        };

        if (HasPointConflict(point, out var conflictMessage))
        {
            _nextPointId = Math.Max(1, _nextPointId - 1);
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", conflictMessage);
            return;
        }

        IsSampleRowContextActive = false;
        CursorTimeMs = timeMs;

        var updated = Points.ToList();
        updated.Add(point);
        ApplyUpdatedPoints(updated, selectPointId: point.Id);
    }

    [RelayCommand]
    private void SelectHitsoundsByFilter()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        if (!HasAnyHsSelectorAudioTypeEnabled)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Select at least one hitsound type in HS Selector.");
            return;
        }

        var matched = Points
            .Where(MatchesHsSelectorFilter)
            .OrderBy(x => x.TimeMs)
            .ThenBy(x => HitSoundSortOrder(x.HitSound))
            .ThenBy(x => SampleSetSortOrder(x.SampleSet))
            .ToList();

        if (matched.Count == 0)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "No hitsound points matched the current HS selector filters.");
            return;
        }

        IsSampleRowContextActive = false;
        var ids = matched.Select(x => x.Id).ToArray();
        var primary = matched[0];
        ApplySelection(ids, primary.Id);
        CursorTimeMs = primary.TimeMs;
        SyncEditorFromPoint(primary);
        UpdateSelectedPointSummary();
        RefreshHeaderBankQuickState();
    }

    public bool TryBuildPointClipboardPayload(out string payload)
    {
        payload = string.Empty;

        var selectedIds = SelectedPointIds.Where(id => id > 0).Distinct().ToHashSet();
        if (selectedIds.Count == 0 && SelectedPointId > 0)
        {
            selectedIds.Add(SelectedPointId);
        }

        if (selectedIds.Count == 0)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Select at least one hitsound point to copy.");
            return false;
        }

        var selectedPoints = Points
            .Where(point => selectedIds.Contains(point.Id))
            .OrderBy(point => point.TimeMs)
            .ThenBy(point => HitSoundSortOrder(point.HitSound))
            .ThenBy(point => SampleSetSortOrder(point.SampleSet))
            .ToList();

        if (selectedPoints.Count == 0)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "No hitsound points available to copy.");
            return false;
        }

        var baseTimeMs = selectedPoints.Min(point => point.TimeMs);
        var clipboard = new PointClipboardPayload
        {
            Format = PointClipboardFormatId,
            Points = selectedPoints.Select(point => new PointClipboardItem
            {
                OffsetMs = point.TimeMs - baseTimeMs,
                SampleSet = SampleSetToDisplay(point.SampleSet),
                HitSound = HitSoundToDisplay(point.HitSound)
            }).ToList()
        };

        payload = JsonSerializer.Serialize(clipboard);
        return true;
    }

    public void PastePointClipboardPayload(string? clipboardText)
    {
        if (!HasLoadedMap)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Load a beatmap first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Clipboard is empty.");
            return;
        }

        PointClipboardPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PointClipboardPayload>(clipboardText);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Clipboard does not contain valid hitsound point data.");
            return;
        }

        if (payload?.Points is null || payload.Points.Count == 0 || payload.Format != PointClipboardFormatId)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Clipboard does not contain hitsound point data.");
            return;
        }

        var minOffset = payload.Points.Min(point => point.OffsetMs);
        var anchorTimeMs = Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        var pastedPoints = new List<HitSoundVisualizerPoint>(payload.Points.Count);
        var nextPointId = _nextPointId;

        foreach (var item in payload.Points.OrderBy(point => point.OffsetMs))
        {
            var pastedTime = anchorTimeMs + (item.OffsetMs - minOffset);
            var candidate = new HitSoundVisualizerPoint
            {
                Id = nextPointId++,
                TimeMs = Math.Clamp(pastedTime, 0, (int)Math.Ceiling(TimelineEndMs)),
                SampleSet = ParseSampleSet(item.SampleSet ?? "Normal"),
                HitSound = ParseHitSound(item.HitSound ?? "Hitnormal"),
                IsDraggable = false
            };

            pastedPoints.Add(candidate);
        }

        var duplicateInPaste = pastedPoints
            .GroupBy(point => (point.TimeMs, point.HitSound, point.SampleSet))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateInPaste is not null)
        {
            toastManager.ShowToast(
                NotificationType.Error,
                "Hitsound Visualizer",
                $"Paste would create duplicate points at {duplicateInPaste.Key.TimeMs}ms.");
            return;
        }

        var conflictingPastedAdditionTime = pastedPoints
            .Where(point => point.HitSound != HitSound.Normal)
            .GroupBy(point => point.TimeMs)
            .FirstOrDefault(group => group.Select(x => x.SampleSet).Distinct().Count() > 1);
        if (conflictingPastedAdditionTime is not null)
        {
            toastManager.ShowToast(
                NotificationType.Error,
                "Hitsound Visualizer",
                $"Clipboard contains conflicting addition sample sets at {conflictingPastedAdditionTime.Key}ms.");
            return;
        }

        var updated = Points.ToList();
        foreach (var candidate in pastedPoints)
        {
            updated.RemoveAll(point =>
                point.TimeMs == candidate.TimeMs &&
                (
                    point.HitSound == candidate.HitSound ||
                    (candidate.HitSound != HitSound.Normal && point.HitSound != HitSound.Normal && point.SampleSet != candidate.SampleSet)));
        }

        updated.AddRange(pastedPoints);

        IsSampleRowContextActive = false;
        if (!ApplyUpdatedPoints(updated, selectPointId: pastedPoints[0].Id))
        {
            return;
        }

        _nextPointId = nextPointId;
        ApplySelection(pastedPoints.Select(point => point.Id), pastedPoints[0].Id);
        UpdateSelectedPointSummary();
    }

    [RelayCommand]
    private void RemoveSelectedPoint()
    {
        if (SelectedPointIds.Count == 0 && SelectedPointId < 0)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Select a point to remove.");
            return;
        }

        var idsToRemove = SelectedPointIds.Count > 0
            ? SelectedPointIds.ToHashSet()
            : new[] { SelectedPointId }.ToHashSet();
        var updated = Points.Where(x => !idsToRemove.Contains(x.Id)).ToList();
        ApplyUpdatedPoints(updated, selectPointId: -1);
    }

    [RelayCommand]
    private void UpdateSelectedPoint()
    {
        var ids = SelectedPointIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0 && SelectedPointId > 0)
        {
            ids.Add(SelectedPointId);
        }

        if (ids.Count == 0)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Select a point to edit.");
            return;
        }

        if (ids.Count > 1)
        {
            ApplySelectedPointAttributes();
            return;
        }

        var selected = Points.FirstOrDefault(x => x.Id == ids[0]);
        if (selected is null)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Select a point to edit.");
            return;
        }

        var editedTimeMs = Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        var editedHitSound = ParseHitSound(SelectedHitSoundName);
        var usesAutoSampleSet = IsAutoSampleSetDisplay(SelectedSampleSetName);
        var editedSampleSet = usesAutoSampleSet
            ? ResolveAutoSampleSetForPointAtTime(editedHitSound, editedTimeMs)
            : ParseSampleSet(SelectedSampleSetName);

        var edited = new HitSoundVisualizerPoint
        {
            Id = selected.Id,
            TimeMs = editedTimeMs,
            SampleSet = editedSampleSet,
            SampleIndexOverride = selected.SampleIndexOverride,
            SampleVolumeOverridePercent = selected.SampleVolumeOverridePercent,
            HitSound = editedHitSound,
            IsAutoSampleSet = usesAutoSampleSet,
            IsDraggable = false
        };

        if (HasPointConflict(edited, out var conflictMessage, ignorePointId: selected.Id))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", conflictMessage);
            return;
        }

        var updated = Points.Select(x => x.Id == selected.Id ? edited : x).ToList();

        ApplyUpdatedPoints(updated, selectPointId: edited.Id);
    }

    [RelayCommand]
    private void ApplySelectedPointAttributes()
    {
        var ids = SelectedPointIds.Where(id => id > 0).Distinct().ToHashSet();
        if (ids.Count == 0 && SelectedPointId > 0)
        {
            ids.Add(SelectedPointId);
        }

        if (ids.Count == 0)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Select at least one point first.");
            return;
        }

        var targetUsesAutoSampleSet = IsAutoSampleSetDisplay(SelectedSampleSetName);
        var targetSampleSet = targetUsesAutoSampleSet ? SampleSet.Normal : ParseSampleSet(SelectedSampleSetName);
        var targetHitSound = ParseHitSound(SelectedHitSoundName);
        var selectedPoints = Points
            .Where(point => ids.Contains(point.Id))
            .ToList();

        var destinationLanes = selectedPoints
            .Select(point => (point.TimeMs, HitSound: targetHitSound))
            .ToHashSet();

        var transformedPoints = selectedPoints
            .Select(point => new HitSoundVisualizerPoint
            {
                Id = point.Id,
                TimeMs = point.TimeMs,
                SampleSet = targetUsesAutoSampleSet
                    ? ResolveAutoSampleSetForPointAtTime(targetHitSound, point.TimeMs)
                    : targetSampleSet,
                SampleIndexOverride = point.SampleIndexOverride,
                SampleVolumeOverridePercent = point.SampleVolumeOverridePercent,
                HitSound = targetHitSound,
                IsAutoSampleSet = targetUsesAutoSampleSet,
                IsDraggable = false
            })
            .GroupBy(point => (point.TimeMs, point.HitSound))
            .Select(group => group.First())
            .ToList();
        var destinationTimes = selectedPoints
            .Select(point => point.TimeMs)
            .Distinct()
            .ToHashSet();

        var normalizeAllAdditionsPerTargetColumn = targetHitSound != HitSound.Normal;
        var preservedPoints = Points
            .Where(point => !ids.Contains(point.Id) && !destinationLanes.Contains((point.TimeMs, point.HitSound)))
            .Select(point =>
            {
                if (!normalizeAllAdditionsPerTargetColumn ||
                    point.HitSound == HitSound.Normal ||
                    !destinationTimes.Contains(point.TimeMs))
                {
                    return point;
                }

                return new HitSoundVisualizerPoint
                {
                    Id = point.Id,
                    TimeMs = point.TimeMs,
                    SampleSet = targetUsesAutoSampleSet
                        ? ResolveAutoSampleSetForPointAtTime(point.HitSound, point.TimeMs)
                        : targetSampleSet,
                    SampleIndexOverride = point.SampleIndexOverride,
                    SampleVolumeOverridePercent = point.SampleVolumeOverridePercent,
                    HitSound = point.HitSound,
                    IsAutoSampleSet = targetUsesAutoSampleSet,
                    IsDraggable = point.IsDraggable
                };
            })
            .ToList();

        var updated = preservedPoints
            .Concat(transformedPoints)
            .ToList();

        var primaryId = SelectedPointId > 0 ? SelectedPointId : ids.First();
        var selectedIds = transformedPoints.Select(point => point.Id).ToArray();
        if (selectedIds.Length == 0)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Batch edit produced no points.");
            return;
        }

        if (!selectedIds.Contains(primaryId))
        {
            primaryId = selectedIds[0];
        }

        ApplyUpdatedPoints(updated, selectPointId: primaryId);
        ApplySelection(selectedIds, primaryId);
        UpdateSelectedPointSummary();
    }

    [RelayCommand]
    private void ApplyContextSamplePoint()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        var timeMs = Math.Clamp(ContextSamplePointTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        var updated = SampleChanges
            .Where(x => x.TimeMs != timeMs)
            .ToList();

        updated.Add(new HitSoundVisualizerSampleChange
        {
            TimeMs = timeMs,
            SampleSet = ParseSampleSet(ContextSampleSetName),
            Index = Math.Clamp(ContextSampleIndex, 1, 99),
            Volume = Math.Clamp(ContextSampleVolume, 0, 100)
        });

        ApplyUpdatedSampleChanges(updated, timeMs);
    }

    [RelayCommand]
    private void RemoveContextSamplePoint()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        var timeMs = Math.Clamp(ContextSamplePointTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        if (!SampleChanges.Any(x => x.TimeMs == timeMs))
        {
            return;
        }

        var updated = SampleChanges.Where(x => x.TimeMs != timeMs).ToList();
        ApplyUpdatedSampleChanges(updated, timeMs);
    }

    [RelayCommand]
    private async Task PlaySelectedPoint()
    {
        var point = Points.FirstOrDefault(x => x.Id == SelectedPointId);
        if (point is null)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Select a point first.");
            return;
        }

        StopPlaybackCore(resetPausedState: true);
        PlaybackStatus = $"Playing {point.TimeMs}ms";
        await PlayPointAsync(point, CancellationToken.None);
        PlaybackStatus = "Idle";
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (!HasLoadedMap || IsPreparingPlayback)
        {
            return;
        }

        if (IsPlaybackRunning)
        {
            PausePlayback();
            return;
        }

        if (IsPlaybackPaused)
        {
            ResumePlayback();
            return;
        }

        PlayPlaybackFromCursor();
    }

    [RelayCommand]
    private void PlayPlaybackFromCursor()
    {
        if (!HasLoadedMap || IsPreparingPlayback)
        {
            return;
        }

        var startTime = CursorTimeMs >= Math.Max(0, TimelineEndMs - 1) ? 0 : CursorTimeMs;
        StartPlaybackAt(startTime);
    }

    [RelayCommand]
    private void PausePlayback()
    {
        if (!IsPlaybackRunning)
        {
            return;
        }

        var currentTime = PlaybackRunner.Pause();
        IsPlaybackRunning = false;
        CursorTimeMs = currentTime;
        PlaybackStatus = "Paused";
        IsPlaybackPaused = true;
        RefreshTimingDebugStatus(force: true);
    }

    [RelayCommand]
    private void ResumePlayback()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        StartPlaybackAt(CursorTimeMs);
    }

    [RelayCommand]
    private void StopPlaybackSession()
    {
        StopPlaybackCore(resetPausedState: true);
        ResetPlaybackPositionToStart();
        PlaybackStatus = "Stopped";
    }

    [RelayCommand]
    private void CommitPlaybackSeek()
    {
        SeekTime(PlaybackSeekMs);
    }

    [RelayCommand]
    private void StopPlayback()
    {
        StopPlaybackCore(resetPausedState: true);
        ResetPlaybackPositionToStart();
        PlaybackStatus = "Stopped";
    }

    [RelayCommand]
    private void BeginTimelinePeek()
    {
        if (!IsPlaybackRunning || !FollowPlaybackCursor)
        {
            return;
        }

        IsTimelinePeekActive = true;
    }

    [RelayCommand]
    private void EndTimelinePeek()
    {
        IsTimelinePeekActive = false;
    }

    [RelayCommand]
    private void ToggleSongMute()
    {
        if (SongVolumePercent <= 0)
        {
            SongVolumePercent = Math.Max(1, _lastNonZeroSongVolumePercent);
            return;
        }

        _lastNonZeroSongVolumePercent = Math.Max(1, SongVolumePercent);
        SongVolumePercent = 0;
    }

    [RelayCommand]
    private void ToggleHitSoundMute()
    {
        if (HitSoundVolumePercent <= 0)
        {
            HitSoundVolumePercent = Math.Max(1, _lastNonZeroHitSoundVolumePercent);
            return;
        }

        _lastNonZeroHitSoundVolumePercent = Math.Max(1, HitSoundVolumePercent);
        HitSoundVolumePercent = 0;
    }

    [RelayCommand]
    private void ApplyAdditionShortcut(string bankName)
    {
        if (!HasLoadedMap || string.IsNullOrWhiteSpace(bankName))
        {
            return;
        }

        var shouldForceApply = string.Equals(HeaderAdditionBankName, bankName, StringComparison.OrdinalIgnoreCase);
        HeaderAdditionBankName = bankName;
        if (shouldForceApply)
        {
            ApplyHeaderBankQuickChange(isNormalBank: false, bankName);
        }
    }

    [RelayCommand]
    private void ApplySampleSetShortcut(string bankName)
    {
        if (!HasLoadedMap || string.IsNullOrWhiteSpace(bankName))
        {
            return;
        }

        var shouldForceApply = string.Equals(HeaderNormalBankName, bankName, StringComparison.OrdinalIgnoreCase);
        HeaderNormalBankName = bankName;
        if (shouldForceApply)
        {
            ApplyHeaderBankQuickChange(isNormalBank: true, bankName);
        }
    }

    [RelayCommand]
    private void AddSelectionHitSound(string hitSoundName)
    {
        if (!HasLoadedMap)
        {
            return;
        }

        var targetHitSound = ParseHitSound(hitSoundName);
        if (targetHitSound == HitSound.Normal)
        {
            return;
        }

        var selectedIds = SelectedPointIds.Where(id => id > 0).Distinct().ToHashSet();
        if (selectedIds.Count == 0 && SelectedPointId > 0)
        {
            selectedIds.Add(SelectedPointId);
        }

        var selectedTimes = selectedIds.Count > 0
            ? Points
                .Where(point => selectedIds.Contains(point.Id))
                .Select(point => point.TimeMs)
                .Distinct()
                .OrderBy(time => time)
                .ToList()
            : [ResolveCursorColumnTimeOrFallback()];
        if (selectedTimes.Count == 0)
        {
            return;
        }

        IsSampleRowContextActive = false;
        var nextPointId = _nextPointId;
        var updated = Points.ToList();
        var addedOrMatchedIds = new List<int>(selectedTimes.Count);

        foreach (var timeMs in selectedTimes)
        {
            var existingAtTime = updated.Where(point => point.TimeMs == timeMs).ToList();
            var existingTarget = existingAtTime.FirstOrDefault(point => point.HitSound == targetHitSound);
            if (existingTarget is not null)
            {
                addedOrMatchedIds.Add(existingTarget.Id);
                continue;
            }

            var existingAdditionSampleSet = existingAtTime
                .Where(point => point.HitSound != HitSound.Normal)
                .Select(point => point.SampleSet)
                .Distinct()
                .FirstOrDefault();
            var normalBank = existingAtTime.FirstOrDefault(point => point.HitSound == HitSound.Normal)?.SampleSet
                ?? ResolveHeaderBankAtTime(true, timeMs);
            var additionBank = existingAdditionSampleSet is SampleSet.Normal or SampleSet.Soft or SampleSet.Drum
                ? existingAdditionSampleSet
                : ResolveHeaderBankAtTime(false, timeMs, normalBank);

            var candidate = new HitSoundVisualizerPoint
            {
                Id = nextPointId++,
                TimeMs = timeMs,
                SampleSet = additionBank,
                HitSound = targetHitSound,
                IsDraggable = false
            };

            if (HasPointConflict(candidate, out _))
            {
                continue;
            }

            updated.Add(candidate);
            addedOrMatchedIds.Add(candidate.Id);
        }

        if (addedOrMatchedIds.Count == 0)
        {
            return;
        }

        var primaryId = SelectedPointId > 0 ? SelectedPointId : addedOrMatchedIds[0];
        if (!ApplyUpdatedPoints(updated, selectPointId: primaryId))
        {
            return;
        }

        _nextPointId = nextPointId;
        var mergedSelection = selectedIds.Concat(addedOrMatchedIds).Distinct().ToArray();
        var mergedPrimaryId = mergedSelection.Contains(primaryId) ? primaryId : mergedSelection[0];
        ApplySelection(mergedSelection, mergedPrimaryId);
        var primaryPoint = Points.FirstOrDefault(point => point.Id == mergedPrimaryId);
        if (primaryPoint is not null)
        {
            SyncEditorFromPoint(primaryPoint);
        }

        UpdateSelectedPointSummary();
        RefreshHeaderBankQuickState();
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var currentState = CaptureHistoryState();
        var targetState = _undoStack.Pop();
        if (!RestoreHistoryState(targetState))
        {
            _undoStack.Push(targetState);
            return;
        }

        _redoStack.Push(currentState);
    }

    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var currentState = CaptureHistoryState();
        var targetState = _redoStack.Pop();
        if (!RestoreHistoryState(targetState))
        {
            _redoStack.Push(targetState);
            return;
        }

        _undoStack.Push(currentState);
    }

    private void StartPlaybackAt(int startTimeMs)
    {
        startTimeMs = Math.Clamp(startTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));

        StopPlaybackCore(resetPausedState: false);
        ResetHitsoundPlaybackDebugTelemetry();

        audioPlaybackService.SetSongVolume(SongVolumePercent / 100f);
        audioPlaybackService.SetHitsoundVolume(HitSoundVolumePercent / 100f);
        var playbackSampleChanges = SampleChanges.OrderBy(x => x.TimeMs).ToList();
        var startResult = PlaybackRunner.Start(
            _loadedAudioFilePath,
            startTimeMs,
            TimelineEndMs,
            Points,
            postToUi: action => Dispatcher.UIThread.Post(action),
            onCursorUpdatedOnUi: UpdatePlaybackCursor,
            onPlaybackCompletedOnUi: OnPlaybackRunnerCompleted,
            onPlayPoint: (point, token) => PlayPointForPlayback(point, playbackSampleChanges, token));
        if (startResult != PlaybackRunnerStartResult.Started)
        {
            CursorTimeMs = startTimeMs;
            PlaybackSeekMs = startTimeMs;
            PlaybackStatus = "Playback requires song audio";
            RefreshTimingDebugStatus(force: true);
            return;
        }

        CursorTimeMs = startTimeMs;
        PlaybackSeekMs = startTimeMs;
        IsPlaybackRunning = true;
        IsPlaybackPaused = false;

        PlaybackStatus = $"Playing from {FormatTimeLabel(startTimeMs)}";
        RefreshTimingDebugStatus(force: true);
    }

    private void RestartPlaybackAt(int startTimeMs)
    {
        if (!IsPlaybackRunning)
        {
            return;
        }

        StartPlaybackAt(startTimeMs);
    }

    private int GetCurrentPlaybackTimeMs()
    {
        return PlaybackRunner.GetCurrentTimeMs();
    }

    private void UpdatePlaybackCursor(int currentTimeMs)
    {
        if (FollowPlaybackCursor && IsPlaybackRunning && !IsTimelinePeekActive)
        {
            var window = Math.Max(100, ViewWindowMs);
            const double playheadAnchorRatio = 0.38d;
            var desiredStart = currentTimeMs - (window * playheadAnchorRatio);
            ViewStartMs = Math.Clamp(desiredStart, 0, Math.Max(0, TimelineEndMs - window));
        }

        CursorTimeMs = currentTimeMs;
        RefreshTimingDebugStatus();
    }

    private void OnPlaybackRunnerCompleted(int endTimeMs)
    {
        IsPlaybackRunning = false;
        IsPlaybackPaused = false;
        IsTimelinePeekActive = false;
        CursorTimeMs = Math.Clamp(endTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        PlaybackStatus = "Playback complete";
        RefreshTimingDebugStatus(force: true);
    }

    private void StopPlaybackCore(bool resetPausedState)
    {
        PlaybackRunner.Stop();

        if (resetPausedState)
        {
            IsPlaybackPaused = false;
        }

        IsPlaybackRunning = false;
        IsTimelinePeekActive = false;
        RefreshTimingDebugStatus(force: true);
    }

    private void ResetPlaybackPositionToStart()
    {
        CursorTimeMs = 0;
        PlaybackSeekMs = 0;
        ContextSamplePointTimeMs = 0;
    }

    private void RefreshTimingDebugStatus(bool force = false)
    {
        var nowTickMs = Environment.TickCount64;
        var lastTickMs = Interlocked.Read(ref _playbackDebugStatusLastUiTickMs);
        if (!force && lastTickMs > 0 && nowTickMs - lastTickMs < PlaybackDebugInfoUpdateIntervalMs)
        {
            return;
        }

        Interlocked.Exchange(ref _playbackDebugStatusLastUiTickMs, nowTickMs);

        var runnerTiming = _playbackRunner?.GetTimingTelemetryStatus() ?? "PlaybackRunner[idle]";
        var audioTiming = audioPlaybackService.GetTimingTelemetryStatus();
        TimingDebugStatus = $"Timing | {runnerTiming} | {audioTiming}";
        AudioDebugStatus = audioPlaybackService.GetSongDebugStatus();
        HitsoundDebugStatus = BuildHitsoundDebugStatus();
    }

    private string ResolveLegacySkinDirectory()
    {
        foreach (var candidate in EnumerateLegacySkinDirectoryCandidates())
        {
            if (LooksLikeLegacyHitsoundDirectory(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private IEnumerable<string> EnumerateLegacySkinDirectoryCandidates()
    {
        var seen = new HashSet<string>(OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal);
        var candidatePaths = new List<string>();

        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                if (seen.Add(fullPath))
                {
                    candidatePaths.Add(fullPath);
                }
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                // Ignore invalid paths while probing.
            }
        }

        Add(Path.Combine(AppContext.BaseDirectory, "Assets", "Skins", "Legacy"));
        Add(Path.Combine(AppContext.BaseDirectory, "Skins", "Legacy"));
        Add(Path.Combine(AppContext.BaseDirectory, "Legacy"));

        foreach (var root in EnumerateAncestorDirectories(AppContext.BaseDirectory))
        {
            Add(Path.Combine(root, "osu-resources", "osu.Game.Resources", "Skins", "Legacy"));
            Add(Path.Combine(root, "osu.Game.Resources", "Skins", "Legacy"));
        }

        if (!string.IsNullOrWhiteSpace(_loadedMapsetDirectoryPath))
        {
            foreach (var root in EnumerateAncestorDirectories(_loadedMapsetDirectoryPath))
            {
                Add(Path.Combine(root, "osu-resources", "osu.Game.Resources", "Skins", "Legacy"));
                Add(Path.Combine(root, "osu.Game.Resources", "Skins", "Legacy"));
            }
        }

        return candidatePaths;
    }

    private bool LooksLikeLegacyHitsoundDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return false;
        }

        // Accept only folders that look like the osu! legacy skin hitsound set.
        // This avoids false positives when probing parent directories.
        string[] requiredBaseNames =
        [
            "normal-hitnormal",
            "normal-hitwhistle",
            "normal-hitfinish",
            "normal-hitclap",
            "soft-hitnormal",
            "drum-hitnormal"
        ];

        foreach (var baseName in requiredBaseNames)
        {
            var exists = _sampleExtensions.Any(ext => File.Exists(Path.Combine(directoryPath, baseName + ext)));
            if (!exists)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> EnumerateAncestorDirectories(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            yield break;
        }

        DirectoryInfo? current;
        try
        {
            current = Directory.Exists(startPath)
                ? new DirectoryInfo(startPath)
                : new FileInfo(startPath).Directory;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            yield break;
        }

        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private void SetOriginBeatmapPath(string beatmapPath)
    {
        var preservePlaybackPosition = ShouldPreservePlaybackPosition(beatmapPath);
        OriginBeatmap = new SelectedMap { Path = beatmapPath };
        PreferredDirectory = Path.GetDirectoryName(beatmapPath) ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(beatmapPath) && File.Exists(beatmapPath))
        {
            _ = LoadTimelineCore(preservePlaybackPosition);
        }
    }

    private int CapturePlaybackPositionMsForReload()
    {
        if (IsPlaybackRunning)
        {
            return Math.Max(0, GetCurrentPlaybackTimeMs());
        }

        return Math.Max(0, Math.Max(CursorTimeMs, PlaybackSeekMs));
    }

    private bool ShouldPreservePlaybackPosition(string nextBeatmapPath)
    {
        if (!HasLoadedMap || string.IsNullOrWhiteSpace(_loadedMapsetDirectoryPath))
        {
            return false;
        }

        var nextMapsetDirectoryPath = Path.GetDirectoryName(nextBeatmapPath);
        if (string.IsNullOrWhiteSpace(nextMapsetDirectoryPath))
        {
            return false;
        }

        try
        {
            var currentMapsetDirectoryPath = Path.GetFullPath(_loadedMapsetDirectoryPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var targetMapsetDirectoryPath = Path.GetFullPath(nextMapsetDirectoryPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(currentMapsetDirectoryPath, targetMapsetDirectoryPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string? GetBeatmapFromMemory()
    {
        return BeatmapSelectionUtils.TryGetBeatmapFromMemory(
            osuMemoryReaderService,
            (type, title, message) => toastManager.ShowToast(type, title, message),
            "Memory Error",
            "Something went wrong while getting the beatmap path from memory.",
            "No Beatmap",
            "No beatmap found in memory.");
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
            "Hitsound Visualizer",
            allowMultiple,
            token,
            preferredMapsetDirectoryPath);

    private bool ApplyUpdatedPoints(
        IReadOnlyCollection<HitSoundVisualizerPoint> updatedPoints,
        int selectPointId,
        bool recordHistory = true)
    {
        if (!TryRebuildWorkingTimeline(updatedPoints, out var timeline, out var error))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", error);
            return false;
        }

        var before = recordHistory && !_isRestoringHistory
            ? CaptureHistoryState()
            : null;

        _workingTimeline = timeline;
        Points = new ObservableCollection<HitSoundVisualizerPoint>(
            updatedPoints
                .OrderBy(x => x.TimeMs)
                .ThenBy(x => HitSoundSortOrder(x.HitSound))
                .ThenBy(x => SampleSetSortOrder(x.SampleSet)));

        ApplySelection(selectPointId > 0 ? [selectPointId] : [], selectPointId);
        SyncContextSamplePointEditor(CursorTimeMs);
        RefreshHeaderBankQuickState();
        UpdateTimelineStats();
        UpdateSelectedPointSummary();
        OnPropertyChanged(nameof(ViewEndMs));
        OnPropertyChanged(nameof(VisiblePointCount));
        PushUndoStateIfChanged(before);
        return true;
    }

    private bool ApplyUpdatedSampleChanges(
        IReadOnlyCollection<HitSoundVisualizerSampleChange> updatedSampleChanges,
        int focusTimeMs,
        bool recordHistory = true)
    {
        if (!TryRebuildWorkingTimeline(Points.ToList(), out var timeline, out var error, updatedSampleChanges))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", error);
            return false;
        }

        var before = recordHistory && !_isRestoringHistory
            ? CaptureHistoryState()
            : null;

        _workingTimeline = timeline;
        SampleChanges = new ObservableCollection<HitSoundVisualizerSampleChange>(
            updatedSampleChanges
                .OrderBy(x => x.TimeMs)
                .ThenBy(x => SampleSetSortOrder(x.SampleSet))
                .ThenBy(x => x.Index));

        SyncContextSamplePointEditor(focusTimeMs);
        RefreshHeaderBankQuickState();
        UpdateTimelineStats();
        OnPropertyChanged(nameof(ViewEndMs));
        PushUndoStateIfChanged(before);
        return true;
    }

    private void PushUndoStateIfChanged(TimelineHistoryState? before)
    {
        if (before is null)
        {
            return;
        }

        if (HistoryPointsEqual(before.Points, Points) &&
            HistorySampleChangesEqual(before.SampleChanges, SampleChanges))
        {
            return;
        }

        _undoStack.Push(before);
        _redoStack.Clear();
    }

    private TimelineHistoryState CaptureHistoryState()
    {
        return new TimelineHistoryState(
            Points
                .OrderBy(x => x.TimeMs)
                .ThenBy(x => HitSoundSortOrder(x.HitSound))
                .ThenBy(x => SampleSetSortOrder(x.SampleSet))
                .ThenBy(x => x.Id)
                .Select(ClonePoint)
                .ToArray(),
            SampleChanges
                .OrderBy(x => x.TimeMs)
                .ThenBy(x => SampleSetSortOrder(x.SampleSet))
                .ThenBy(x => x.Index)
                .Select(CloneSampleChange)
                .ToArray(),
            CursorTimeMs,
            ContextSamplePointTimeMs,
            IsSampleRowContextActive,
            SelectedPointId,
            SelectedPointIds.Where(id => id > 0).Distinct().ToArray());
    }

    private bool RestoreHistoryState(TimelineHistoryState state)
    {
        if (!TryRebuildWorkingTimeline(state.Points, out var timeline, out var error, state.SampleChanges))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", error);
            return false;
        }

        StopPlaybackCore(resetPausedState: true);
        PlaybackStatus = "Idle";

        _isRestoringHistory = true;
        try
        {
            _workingTimeline = timeline;
            Points = new ObservableCollection<HitSoundVisualizerPoint>(
                state.Points
                    .OrderBy(x => x.TimeMs)
                    .ThenBy(x => HitSoundSortOrder(x.HitSound))
                    .ThenBy(x => SampleSetSortOrder(x.SampleSet))
                    .Select(ClonePoint));
            SampleChanges = new ObservableCollection<HitSoundVisualizerSampleChange>(
                state.SampleChanges
                    .OrderBy(x => x.TimeMs)
                    .ThenBy(x => SampleSetSortOrder(x.SampleSet))
                    .ThenBy(x => x.Index)
                    .Select(CloneSampleChange));

            _nextPointId = Math.Max(1, Points.Any() ? Points.Max(x => x.Id) + 1 : 1);
            CursorTimeMs = Math.Clamp(state.CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
            IsSampleRowContextActive = state.IsSampleRowContextActive;

            var existingIds = Points.Select(x => x.Id).ToHashSet();
            var selectedIds = state.SelectedPointIds.Where(existingIds.Contains).ToArray();
            var primaryId = selectedIds.Contains(state.SelectedPointId)
                ? state.SelectedPointId
                : selectedIds.FirstOrDefault();
            ApplySelection(selectedIds, primaryId);

            ContextSamplePointTimeMs = Math.Clamp(state.ContextSamplePointTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
            SyncContextSamplePointEditor(ContextSamplePointTimeMs);

            var primary = Points.FirstOrDefault(x => x.Id == SelectedPointId);
            if (primary is not null && !IsSampleRowContextActive)
            {
                SyncEditorFromPoint(primary);
            }

            RefreshHeaderBankQuickState();
            UpdateTimelineStats();
            UpdateSelectedPointSummary();
            OnPropertyChanged(nameof(ViewEndMs));
            OnPropertyChanged(nameof(VisiblePointCount));
            return true;
        }
        finally
        {
            _isRestoringHistory = false;
        }
    }

    private static HitSoundVisualizerPoint ClonePoint(HitSoundVisualizerPoint point)
    {
        return new HitSoundVisualizerPoint
        {
            Id = point.Id,
            TimeMs = point.TimeMs,
            SampleSet = point.SampleSet,
            SampleIndexOverride = point.SampleIndexOverride,
            SampleVolumeOverridePercent = point.SampleVolumeOverridePercent,
            IsAutoSampleSet = point.IsAutoSampleSet,
            HitSound = point.HitSound,
            IsDraggable = point.IsDraggable
        };
    }

    private static HitSoundVisualizerSampleChange CloneSampleChange(HitSoundVisualizerSampleChange sampleChange)
    {
        return new HitSoundVisualizerSampleChange
        {
            TimeMs = sampleChange.TimeMs,
            SampleSet = sampleChange.SampleSet,
            Index = sampleChange.Index,
            Volume = sampleChange.Volume
        };
    }

    private static bool HistoryPointsEqual(
        IReadOnlyList<HitSoundVisualizerPoint> left,
        IReadOnlyList<HitSoundVisualizerPoint> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            var l = left[i];
            var r = right[i];
            if (l.Id != r.Id ||
                l.TimeMs != r.TimeMs ||
                l.SampleSet != r.SampleSet ||
                l.SampleIndexOverride != r.SampleIndexOverride ||
                l.SampleVolumeOverridePercent != r.SampleVolumeOverridePercent ||
                l.HitSound != r.HitSound ||
                l.IsAutoSampleSet != r.IsAutoSampleSet ||
                l.IsDraggable != r.IsDraggable)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HistorySampleChangesEqual(
        IReadOnlyList<HitSoundVisualizerSampleChange> left,
        IReadOnlyList<HitSoundVisualizerSampleChange> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            var l = left[i];
            var r = right[i];
            if (l.TimeMs != r.TimeMs ||
                l.SampleSet != r.SampleSet ||
                l.Index != r.Index ||
                l.Volume != r.Volume)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplySelection(IEnumerable<int> pointIds, int primaryPointId)
    {
        var ids = pointIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        SelectedPointIds = new ObservableCollection<int>(ids);
        SelectedPointId = ids.Count == 0 ? -1 : (primaryPointId > 0 && ids.Contains(primaryPointId) ? primaryPointId : ids[0]);
    }

    private void SyncEditorFromPoint(HitSoundVisualizerPoint point)
    {
        SelectedSampleSetName = point.IsAutoSampleSet ? "Auto" : SampleSetToDisplay(point.SampleSet);
        SelectedHitSoundName = HitSoundToDisplay(point.HitSound);
        NewPointIsSliderBody = false;
    }

    private void SyncContextSamplePointEditor(int timeMs)
    {
        ContextSamplePointTimeMs = Math.Clamp(timeMs, 0, (int)Math.Ceiling(TimelineEndMs));

        var exact = SampleChanges
            .Where(x => x.TimeMs == ContextSamplePointTimeMs)
            .OrderBy(x => SampleSetSortOrder(x.SampleSet))
            .FirstOrDefault();

        var effective = exact ?? SampleChanges
            .Where(x => x.TimeMs <= ContextSamplePointTimeMs)
            .OrderBy(x => x.TimeMs)
            .LastOrDefault();

        ContextSamplePointExists = exact is not null;
        ContextSampleSetName = SampleSetToDisplay((exact ?? effective)?.SampleSet ?? SampleSet.Normal);
        ContextSampleIndex = Math.Max(1, (exact ?? effective)?.Index ?? 1);
        ContextSampleVolume = Math.Clamp((exact ?? effective)?.Volume ?? 100, 0, 100);
        UpdateContextSamplePointSummary();
        if (IsSampleRowContextActive)
        {
            RefreshHeaderBankQuickState();
        }
    }

    private void UpdateContextSamplePointSummary()
    {
        ContextSamplePointSummary = ContextSamplePointExists
            ? $"Sample point at {FormatTimeLabel(ContextSamplePointTimeMs)}"
            : $"New sample point at {FormatTimeLabel(ContextSamplePointTimeMs)} (uses current values)";
    }

    private int ResolveCursorColumnTimeOrFallback()
    {
        return TryResolveCursorColumnTime(out var columnTimeMs)
            ? columnTimeMs
            : Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
    }

    private bool TryResolveCursorColumnTime(out int columnTimeMs)
    {
        columnTimeMs = Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        if (Points.Count == 0)
        {
            return false;
        }

        var bestDistanceMs = CursorColumnToleranceMs + 1;
        var hasCandidate = false;
        var candidateTimeMs = columnTimeMs;
        foreach (var point in Points)
        {
            var distanceMs = Math.Abs(point.TimeMs - columnTimeMs);
            if (distanceMs > CursorColumnToleranceMs)
            {
                continue;
            }

            if (distanceMs < bestDistanceMs ||
                (distanceMs == bestDistanceMs && point.TimeMs < candidateTimeMs))
            {
                bestDistanceMs = distanceMs;
                candidateTimeMs = point.TimeMs;
                hasCandidate = true;
            }
        }

        if (!hasCandidate)
        {
            return false;
        }

        columnTimeMs = candidateTimeMs;
        return true;
    }

    private void RefreshHeaderBankQuickState()
    {
        _suppressHeaderBankQuickApply = true;
        try
        {
            if (IsSampleRowContextActive)
            {
                HeaderNormalBankName = ContextSampleSetName;
                HeaderAdditionBankName = "Auto";
                return;
            }

            var primaryPoint = Points.FirstOrDefault(x => x.Id == SelectedPointId);
            if (primaryPoint is null)
            {
                var cursorTimeMs = ResolveCursorColumnTimeOrFallback();
                var pointsNearCursor = Points
                    .Where(x => Math.Abs(x.TimeMs - cursorTimeMs) <= CursorColumnToleranceMs)
                    .OrderBy(x => HitSoundSortOrder(x.HitSound))
                    .ToList();

                var cursorNormalPoint = pointsNearCursor.FirstOrDefault(x => x.HitSound == HitSound.Normal);
                var cursorAdditionPoint = pointsNearCursor.FirstOrDefault(x => x.HitSound != HitSound.Normal);
                var cursorNormalBank = cursorNormalPoint?.SampleSet
                                       ?? ResolveHeaderBankAtTime(true, cursorTimeMs);
                var cursorAdditionBank = cursorAdditionPoint?.SampleSet
                                         ?? cursorNormalBank;

                HeaderNormalBankName = cursorNormalPoint?.IsAutoSampleSet == true
                    ? "Auto"
                    : SampleSetToDisplay(cursorNormalBank);
                HeaderAdditionBankName = cursorAdditionPoint?.IsAutoSampleSet == true
                    ? "Auto"
                    : (cursorAdditionPoint is null && cursorAdditionBank == cursorNormalBank
                        ? "Auto"
                        : SampleSetToDisplay(cursorAdditionBank));
                return;
            }

            var selectedPointIds = SelectedPointIds.Where(id => id > 0).Distinct().ToHashSet();
            if (selectedPointIds.Count == 0 && SelectedPointId > 0)
            {
                selectedPointIds.Add(SelectedPointId);
            }

            var selectedPoints = selectedPointIds.Count == 0
                ? []
                : Points
                    .Where(point => selectedPointIds.Contains(point.Id))
                    .OrderBy(point => point.TimeMs)
                    .ThenBy(point => HitSoundSortOrder(point.HitSound))
                    .ToList();

            var sameTimePoints = Points
                .Where(x => x.TimeMs == primaryPoint.TimeMs)
                .OrderBy(x => HitSoundSortOrder(x.HitSound))
                .ToList();

            var normalPoint = sameTimePoints.FirstOrDefault(x => x.HitSound == HitSound.Normal) ??
                              selectedPoints.FirstOrDefault(x => x.HitSound == HitSound.Normal);
            var additionPoint = sameTimePoints.FirstOrDefault(x => x.HitSound != HitSound.Normal) ??
                                selectedPoints.FirstOrDefault(x => x.HitSound != HitSound.Normal);
            var normalBank = normalPoint?.SampleSet ?? primaryPoint.SampleSet;
            var additionBank = additionPoint?.SampleSet ?? normalBank;

            HeaderNormalBankName = normalPoint?.IsAutoSampleSet == true
                ? "Auto"
                : SampleSetToDisplay(normalBank);
            HeaderAdditionBankName = additionPoint?.IsAutoSampleSet == true
                ? "Auto"
                : (additionPoint is null && additionBank == normalBank
                    ? "Auto"
                    : SampleSetToDisplay(additionBank));
        }
        finally
        {
            _suppressHeaderBankQuickApply = false;
        }
    }

    private void ApplyHeaderBankQuickChange(bool isNormalBank, string value)
    {
        if (_suppressHeaderBankQuickApply || !HasLoadedMap)
        {
            return;
        }
        
        var applyAuto = string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase);
        var sampleSet = applyAuto ? SampleSet.Normal : ParseSampleSet(value);

        if (IsSampleRowContextActive)
        {
            if (applyAuto)
            {
                return;
            }

            ContextSampleSetName = SampleSetToDisplay(sampleSet);

            _suppressHeaderBankQuickApply = true;
            HeaderNormalBankName = ContextSampleSetName;
            HeaderAdditionBankName = ContextSampleSetName;
            _suppressHeaderBankQuickApply = false;

            ApplyContextSamplePoint();
            return;
        }

        if (!HasSelectedPoint)
        {
            // No selected point: apply to points at the current cursor column when possible.
            // If nothing exists there, keep the quick bank values as defaults for subsequent actions.
            var cursorTimeMs = ResolveCursorColumnTimeOrFallback();
            bool IsTargetPointAtCursor(HitSoundVisualizerPoint point) =>
                Math.Abs(point.TimeMs - cursorTimeMs) <= CursorColumnToleranceMs &&
                MatchesQuickBankTarget(point, isNormalBank);

            if (!Points.Any(IsTargetPointAtCursor))
            {
                return;
            }

            var updatedAtCursor = Points
                .Select(point => IsTargetPointAtCursor(point)
                    ? ApplyQuickBankEditToPoint(
                        point,
                        applyAuto,
                        sampleSet)
                    : point)
                .ToList();

            if (!ApplyUpdatedPoints(updatedAtCursor, selectPointId: -1))
            {
                return;
            }

            ApplySelection([], -1);
            UpdateSelectedPointSummary();
            RefreshHeaderBankQuickState();
            return;
        }

        ApplySampleSetToSelectionSubset(isNormalBank, sampleSet, applyAuto);
        RefreshHeaderBankQuickState();
    }

    private bool MatchesHsSelectorFilter(HitSoundVisualizerPoint point)
    {
        if (!MatchesSelectedAudioType(point.HitSound))
        {
            return false;
        }

        var bankFilter = point.HitSound == HitSound.Normal
            ? HsSelectorNormalBankFilterName
            : HsSelectorAdditionBankFilterName;

        if (string.Equals(bankFilter, "Any", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(bankFilter, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return point.IsAutoSampleSet;
        }

        return point.SampleSet == ParseSampleSet(bankFilter);
    }

    private bool MatchesSelectedAudioType(HitSound hitSound)
    {
        return hitSound switch
        {
            HitSound.Normal => HsSelectorIncludeHitNormal,
            HitSound.Whistle => HsSelectorIncludeWhistle,
            HitSound.Finish => HsSelectorIncludeFinish,
            HitSound.Clap => HsSelectorIncludeClap,
            _ => false
        };
    }

    private SampleSet ResolveHeaderBankAtTime(bool isNormalBank, int timeMs, SampleSet? resolvedNormalBank = null)
    {
        var headerValue = isNormalBank ? HeaderNormalBankName : HeaderAdditionBankName;
        if (!string.Equals(headerValue, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return ParseSampleSet(headerValue);
        }

        if (!isNormalBank && resolvedNormalBank is not null)
        {
            return resolvedNormalBank.Value;
        }

        var effectiveSample = SampleChanges
            .Where(x => x.TimeMs <= timeMs)
            .OrderBy(x => x.TimeMs)
            .LastOrDefault()
            ?.SampleSet;

        return effectiveSample is SampleSet.Normal or SampleSet.Soft or SampleSet.Drum
            ? effectiveSample.Value
            : SampleSet.Normal;
    }

    private static bool TryGetHitSoundFromRowIndex(int rowIndex, out HitSound hitSound)
    {
        hitSound = rowIndex switch
        {
            1 => HitSound.Normal,
            2 => HitSound.Whistle,
            3 => HitSound.Finish,
            4 => HitSound.Clap,
            _ => HitSound.Normal
        };

        return rowIndex is >= 1 and <= 4;
    }

    private void ApplySampleSetToSelectionSubset(bool affectNormalPoints, SampleSet sampleSet, bool applyAuto)
    {
        var selectedIds = SelectedPointIds.Where(id => id > 0).Distinct().ToHashSet();
        if (selectedIds.Count == 0 && SelectedPointId > 0)
        {
            selectedIds.Add(SelectedPointId);
        }

        if (selectedIds.Count == 0)
        {
            return;
        }

        // Header bank quick-edit is column-based (timestamp-based), not point-based.
        // It only edits points from the selected bank (normal or additions), never the opposite bank.
        var selectedTimes = Points
            .Where(point => selectedIds.Contains(point.Id))
            .Select(point => point.TimeMs)
            .Distinct()
            .ToHashSet();

        if (selectedTimes.Count == 0)
        {
            return;
        }

        bool IsTargetPoint(HitSoundVisualizerPoint point) =>
            selectedTimes.Contains(point.TimeMs) &&
            MatchesQuickBankTarget(point, affectNormalPoints);

        if (!Points.Any(IsTargetPoint))
        {
            return;
        }

        var updated = Points
            .Select(point => IsTargetPoint(point)
                ? ApplyQuickBankEditToPoint(
                    point,
                    applyAuto,
                    sampleSet)
                : point)
            .ToList();

        var primaryId = SelectedPointId > 0 ? SelectedPointId : selectedIds.First();
        var selectedIdArray = selectedIds.ToArray();
        ApplyUpdatedPoints(updated, selectPointId: primaryId);
        ApplySelection(selectedIdArray, primaryId);
        UpdateSelectedPointSummary();
    }

    private HitSoundVisualizerPoint ApplyQuickBankEditToPoint(
        HitSoundVisualizerPoint point,
        bool applyAuto,
        SampleSet manualSampleSet)
    {
        var resolvedNormalSampleSet = ResolveHeaderBankAtTime(isNormalBank: true, point.TimeMs);
        var resolvedSampleSet = applyAuto
            ? (point.HitSound == HitSound.Normal
                ? resolvedNormalSampleSet
                : ResolveHeaderBankAtTime(isNormalBank: false, point.TimeMs, resolvedNormalSampleSet))
            : manualSampleSet;

        return new HitSoundVisualizerPoint
        {
            Id = point.Id,
            TimeMs = point.TimeMs,
            SampleSet = resolvedSampleSet,
            SampleIndexOverride = point.SampleIndexOverride,
            SampleVolumeOverridePercent = point.SampleVolumeOverridePercent,
            IsAutoSampleSet = applyAuto,
            HitSound = point.HitSound,
            IsDraggable = point.IsDraggable
        };
    }

    private static bool MatchesQuickBankTarget(HitSoundVisualizerPoint point, bool normalBank)
    {
        return normalBank
            ? point.HitSound == HitSound.Normal
            : point.HitSound != HitSound.Normal;
    }

    private bool TryRebuildWorkingTimeline(
        IReadOnlyCollection<HitSoundVisualizerPoint> sourcePoints,
        out HitSoundTimeline timeline,
        out string error,
        IReadOnlyCollection<HitSoundVisualizerSampleChange>? sampleChangesOverride = null)
    {
        error = string.Empty;

        if (!TryBuildSoundTimeline(sourcePoints.Where(x => !x.IsDraggable), out var nonDraggable, out error))
        {
            timeline = new HitSoundTimeline();
            return false;
        }

        var sampleChanges = sampleChangesOverride ?? SampleChanges.ToList();

        timeline = new HitSoundTimeline
        {
            NonDraggableSoundTimeline = nonDraggable,
            DraggableSoundTimeline = new SoundTimeline(),
            SampleSetTimeline = new SampleSetTimeline
            {
                HitSamples = sampleChanges
                    .OrderBy(x => x.TimeMs)
                    .Select(x => new SampleSetEvent(x.TimeMs, x.SampleSet, x.Index, x.Volume))
                    .ToList()
            }
        };

        return true;
    }

    private static bool TryBuildSoundTimeline(
        IEnumerable<HitSoundVisualizerPoint> sourcePoints,
        out SoundTimeline timeline,
        out string error)
    {
        error = string.Empty;
        var events = new List<SoundEvent>();

        foreach (var timeGroup in sourcePoints
                     .GroupBy(x => x.TimeMs)
                     .OrderBy(x => x.Key))
        {
            var groupPoints = timeGroup.ToList();

            var normalPoints = groupPoints.Where(x => x.HitSound == HitSound.Normal).ToList();
            if (normalPoints.Select(x => x.SampleSet).Distinct().Count() > 1)
            {
                error = $"Conflicting hitnormal sample sets at {timeGroup.Key}ms.";
                timeline = new SoundTimeline();
                return false;
            }

            var additionPoints = groupPoints.Where(x => x.HitSound != HitSound.Normal).ToList();
            if (additionPoints.Select(x => x.SampleSet).Distinct().Count() > 1)
            {
                error = $"Conflicting addition sample sets at {timeGroup.Key}ms. Current format only supports one addition sample set per timestamp/source.";
                timeline = new SoundTimeline();
                return false;
            }

            var distinctSounds = groupPoints
                .Select(x => x.HitSound)
                .Distinct()
                .OrderBy(HitSoundSortOrder)
                .ToList();

            var normalSample = normalPoints.FirstOrDefault()?.SampleSet
                ?? additionPoints.FirstOrDefault()?.SampleSet
                ?? SampleSet.Normal;
            var additionSample = additionPoints.FirstOrDefault()?.SampleSet ?? normalSample;
            var sampleIndexOverride = normalPoints.FirstOrDefault()?.SampleIndexOverride
                                      ?? additionPoints.FirstOrDefault()?.SampleIndexOverride;
            var sampleVolumeOverride = normalPoints.FirstOrDefault()?.SampleVolumeOverridePercent
                                       ?? additionPoints.FirstOrDefault()?.SampleVolumeOverridePercent;

            events.Add(new SoundEvent(
                TimeSpan.FromMilliseconds(timeGroup.Key),
                distinctSounds,
                normalSample,
                additionSample,
                sampleIndexOverride: sampleIndexOverride,
                sampleVolumeOverride: sampleVolumeOverride));
        }

        timeline = new SoundTimeline(events);
        return true;
    }

    private bool HasPointConflict(HitSoundVisualizerPoint candidate, out string message, int ignorePointId = -1)
    {
        message = string.Empty;
        var otherPoints = Points.Where(x => x.Id != ignorePointId);

        if (otherPoints.Any(x =>
                x.TimeMs == candidate.TimeMs &&
                x.HitSound == candidate.HitSound &&
                x.SampleSet == candidate.SampleSet))
        {
            message = "That point already exists on the same lane.";
            return true;
        }

        if (candidate.HitSound == HitSound.Normal)
        {
            var existingNormal = otherPoints.FirstOrDefault(x =>
                x.TimeMs == candidate.TimeMs &&
                x.HitSound == HitSound.Normal);

            if (existingNormal is not null && existingNormal.SampleSet != candidate.SampleSet)
            {
                message = "A hitnormal point already exists at this timestamp with another sample set.";
                return true;
            }
        }
        else
        {
            var additionSampleSet = otherPoints
                .Where(x => x.TimeMs == candidate.TimeMs && x.HitSound != HitSound.Normal)
                .Select(x => x.SampleSet)
                .Distinct()
                .ToList();

            if (additionSampleSet.Count > 0 && additionSampleSet[0] != candidate.SampleSet)
            {
                message = "This timeline format only supports one addition sample set per timestamp/source.";
                return true;
            }
        }

        return false;
    }

    private void RebuildPointTimeCache(IReadOnlyList<HitSoundVisualizerPoint>? points)
    {
        if (points is null || points.Count == 0)
        {
            _sortedPointTimeCache = [];
            return;
        }

        var times = new int[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            times[i] = points[i].TimeMs;
        }

        _sortedPointTimeCache = times;
    }

    private int CountVisiblePointsCached(double viewStartMs, double viewEndMs)
    {
        var times = _sortedPointTimeCache;
        if (times.Length == 0)
        {
            return 0;
        }

        var minTimeMs = (int)Math.Floor(Math.Min(viewStartMs, viewEndMs));
        var maxTimeMs = (int)Math.Ceiling(Math.Max(viewStartMs, viewEndMs));
        var startIndex = LowerBound(times, minTimeMs);
        var endIndexExclusive = UpperBound(times, maxTimeMs);
        return Math.Max(0, endIndexExclusive - startIndex);
    }

    private static int LowerBound(int[] values, int target)
    {
        var lo = 0;
        var hi = values.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (values[mid] < target)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    private static int UpperBound(int[] values, int target)
    {
        var lo = 0;
        var hi = values.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (values[mid] <= target)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    private void NormalizeViewWindow()
    {
        ViewWindowMs = Math.Clamp(ViewWindowMs, 100, Math.Max(100, TimelineEndMs));
        ViewStartMs = Math.Clamp(ViewStartMs, 0, Math.Max(0, TimelineEndMs - ViewWindowMs));
        CursorTimeMs = Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        PlaybackSeekMs = Math.Clamp(PlaybackSeekMs, 0, (int)Math.Ceiling(TimelineEndMs));
        ContextSamplePointTimeMs = Math.Clamp(ContextSamplePointTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
    }

    private void UpdateTimelineStats()
    {
        TimelineStats =
            $"Points: {Points.Count} (hittables) | " +
            $"Sample changes: {SampleChanges.Count} | Visible: {VisiblePointCount}";
    }

    private void UpdateSelectedPointSummary()
    {
        if (SelectedPointIds.Count > 1)
        {
            var primary = Points.FirstOrDefault(x => x.Id == SelectedPointId)
                          ?? Points.FirstOrDefault(x => x.Id == SelectedPointIds[0]);
            SelectedPointSummary = primary is null
                ? $"{SelectedPointIds.Count} points selected."
                : $"{SelectedPointIds.Count} points selected | Primary: {primary.TimeMs}ms | {SampleSetToDisplay(primary.SampleSet)}-{HitSoundToDisplay(primary.HitSound).ToLowerInvariant()}";
            return;
        }

        var point = Points.FirstOrDefault(x => x.Id == SelectedPointId);
        SelectedPointSummary = point is null
            ? "No point selected."
            : $"{point.TimeMs}ms | {SampleSetToDisplay(point.SampleSet)}-{HitSoundToDisplay(point.HitSound).ToLowerInvariant()}";
    }

    private async Task PlayPointAsync(HitSoundVisualizerPoint point, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (TryPlayPointSample(point))
        {
            return;
        }

        await PlayToneFallbackAsync(point, cancellationToken);
    }

    private void PlayPointForPlayback(
        HitSoundVisualizerPoint point,
        IReadOnlyList<HitSoundVisualizerSampleChange> playbackSampleChanges,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            if (TryPlayPointSample(point, playbackSampleChanges))
            {
                return;
            }

            _ = PlayToneFallbackAsync(point, cancellationToken);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            // Ignore point playback failures during playback preview.
        }
    }

    private bool TryPlayPointSample(HitSoundVisualizerPoint point)
    {
        return TryPlayPointSample(point, SampleChanges);
    }

    private bool TryPlayPointSample(HitSoundVisualizerPoint point, IReadOnlyList<HitSoundVisualizerSampleChange> sampleChanges)
    {
        var sampleState = ResolvePlaybackSampleState(point, sampleChanges);
        var sampleFilePath = ResolveSampleFilePath(point, sampleState.Index);
        var playbackBusKey = ResolvePlaybackBusKey(point);
        var played = !string.IsNullOrWhiteSpace(sampleFilePath) &&
                     audioPlaybackService.PlayHitsound(sampleFilePath, sampleState.Volume / 100f, playbackBusKey);
        RecordHitsoundPlaybackDebug(point, sampleState, sampleFilePath, played);
        return played;
    }

    private string ResolveSampleFilePath(HitSoundVisualizerPoint point, int index)
    {
        var mapsetPath = ResolveSampleFilePathFromDirectoryCached(_loadedMapsetDirectoryPath, point.SampleSet, point.HitSound, index);
        if (!string.IsNullOrWhiteSpace(mapsetPath))
        {
            return mapsetPath;
        }

        return ResolveSampleFilePathFromDirectoryCached(_legacySkinDirectoryPath, point.SampleSet, point.HitSound, index);
    }

    private static string ResolvePlaybackBusKey(HitSoundVisualizerPoint point)
    {
        var hitSoundFlags = (int)point.HitSound;
        return point.HitSound switch
        {
            HitSound.Whistle => "hs-whistle",
            HitSound.Finish => "hs-finish",
            HitSound.Clap => "hs-clap",
            _ when (hitSoundFlags & (int)HitSound.Clap) == (int)HitSound.Clap => "hs-clap",
            _ when (hitSoundFlags & (int)HitSound.Finish) == (int)HitSound.Finish => "hs-finish",
            _ when (hitSoundFlags & (int)HitSound.Whistle) == (int)HitSound.Whistle => "hs-whistle",
            _ => "hs-normal"
        };
    }

    private static PlaybackSampleState ResolvePlaybackSampleState(HitSoundVisualizerPoint point, IReadOnlyList<HitSoundVisualizerSampleChange> sampleChanges)
    {
        var normalizedPointIndexOverride = NormalizePointSampleIndexOverride(point.SampleIndexOverride);
        var normalizedPointVolumeOverride = NormalizePointSampleVolumeOverride(point.SampleVolumeOverridePercent);

        if (sampleChanges.Count == 0)
        {
            var fallbackIndex = Math.Max(1, normalizedPointIndexOverride ?? 1);
            var fallbackVolume = ResolveEffectivePlaybackVolume(normalizedPointVolumeOverride, 100);
            return new PlaybackSampleState(
                fallbackIndex,
                fallbackVolume,
                normalizedPointIndexOverride.HasValue,
                normalizedPointVolumeOverride.HasValue);
        }

        var targetTimeMs = point.TimeMs;
        var lo = 0;
        var hi = sampleChanges.Count - 1;
        var matched = -1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (sampleChanges[mid].TimeMs <= targetTimeMs)
            {
                matched = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        if (matched < 0)
        {
            var fallbackIndex = Math.Max(1, normalizedPointIndexOverride ?? 1);
            var fallbackVolume = ResolveEffectivePlaybackVolume(normalizedPointVolumeOverride, 100);
            return new PlaybackSampleState(
                fallbackIndex,
                fallbackVolume,
                normalizedPointIndexOverride.HasValue,
                normalizedPointVolumeOverride.HasValue);
        }

        var sampleChange = sampleChanges[matched];
        var effectiveIndex = Math.Max(1, normalizedPointIndexOverride ?? sampleChange.Index);
        var effectiveVolume = ResolveEffectivePlaybackVolume(
            normalizedPointVolumeOverride,
            Math.Clamp(sampleChange.Volume, 0, 100));

        return new PlaybackSampleState(
            effectiveIndex,
            effectiveVolume,
            normalizedPointIndexOverride.HasValue,
            normalizedPointVolumeOverride.HasValue);
    }

    private readonly record struct PlaybackSampleState(
        int Index,
        int Volume,
        bool UsedPointIndexOverride,
        bool UsedPointVolumeOverride);

    private static int ResolveEffectivePlaybackVolume(int? pointVolumeOverridePercent, int timelineVolumePercent)
    {
        if (pointVolumeOverridePercent is > 0)
        {
            return Math.Clamp(pointVolumeOverridePercent.Value, 1, 100);
        }

        // Timing-point volume 0 is treated as unspecified/default in playback context.
        var normalizedTimelineVolume = timelineVolumePercent <= 0 ? 100 : timelineVolumePercent;
        return Math.Clamp(normalizedTimelineVolume, 1, 100);
    }

    private static int? NormalizePointSampleIndexOverride(int? value)
    {
        return value is > 0 ? value.Value : null;
    }

    private static int? NormalizePointSampleVolumeOverride(int? value)
    {
        return value is > 0 ? Math.Clamp(value.Value, 1, 100) : null;
    }

    private void ResetHitsoundPlaybackDebugTelemetry()
    {
        lock (_hitsoundDebugSync)
        {
            _hitsoundDebugAttemptCount = 0;
            _hitsoundDebugSuccessCount = 0;
            _hitsoundDebugFailureCount = 0;
            _hitsoundDebugLastPointTimeMs = 0;
            _hitsoundDebugLastResolvedIndex = 1;
            _hitsoundDebugLastResolvedVolume = 100;
            _hitsoundDebugLastUsedPointIndexOverride = false;
            _hitsoundDebugLastUsedPointVolumeOverride = false;
            _hitsoundDebugLastSampleName = "-";
            _hitsoundDebugRecentSuccessTickMs.Clear();
        }
    }

    private void RecordHitsoundPlaybackDebug(
        HitSoundVisualizerPoint point,
        PlaybackSampleState sampleState,
        string sampleFilePath,
        bool played)
    {
        var nowTickMs = Environment.TickCount64;
        lock (_hitsoundDebugSync)
        {
            _hitsoundDebugAttemptCount++;
            if (played)
            {
                _hitsoundDebugSuccessCount++;
                _hitsoundDebugRecentSuccessTickMs.Enqueue(nowTickMs);
            }
            else
            {
                _hitsoundDebugFailureCount++;
            }

            _hitsoundDebugLastPointTimeMs = point.TimeMs;
            _hitsoundDebugLastResolvedIndex = sampleState.Index;
            _hitsoundDebugLastResolvedVolume = sampleState.Volume;
            _hitsoundDebugLastUsedPointIndexOverride = sampleState.UsedPointIndexOverride;
            _hitsoundDebugLastUsedPointVolumeOverride = sampleState.UsedPointVolumeOverride;
            _hitsoundDebugLastSampleName = string.IsNullOrWhiteSpace(sampleFilePath)
                ? "-"
                : Path.GetFileName(sampleFilePath);

            PruneHitsoundDebugWindow(nowTickMs);
        }
    }

    private string BuildHitsoundDebugStatus()
    {
        lock (_hitsoundDebugSync)
        {
            PruneHitsoundDebugWindow(Environment.TickCount64);
            var activeVoicesApprox = _hitsoundDebugRecentSuccessTickMs.Count;
            var indexSourceTag = _hitsoundDebugLastUsedPointIndexOverride ? "obj" : "timing";
            var volumeSourceTag = _hitsoundDebugLastUsedPointVolumeOverride ? "obj" : "timing";

            return $"HSDbg | ok {_hitsoundDebugSuccessCount}/{_hitsoundDebugAttemptCount} fail {_hitsoundDebugFailureCount} " +
                   $"active~{activeVoicesApprox} " +
                   $"last t {_hitsoundDebugLastPointTimeMs}ms idx {_hitsoundDebugLastResolvedIndex}({indexSourceTag}) " +
                   $"vol {_hitsoundDebugLastResolvedVolume}%({volumeSourceTag}) file {_hitsoundDebugLastSampleName}";
        }
    }

    private void PruneHitsoundDebugWindow(long nowTickMs)
    {
        while (_hitsoundDebugRecentSuccessTickMs.Count > 0 &&
               nowTickMs - _hitsoundDebugRecentSuccessTickMs.Peek() > HitsoundDebugActiveWindowMs)
        {
            _hitsoundDebugRecentSuccessTickMs.Dequeue();
        }
    }

    private string ResolveSampleFilePathFromDirectoryCached(string directoryPath, SampleSet sampleSet, HitSound hitSound, int index)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return string.Empty;
        }

        var cacheKey = $"{directoryPath}|{(int)sampleSet}|{(int)hitSound}|{Math.Max(1, index)}";
        return _resolvedPlaybackSamplePathCache.GetOrAdd(
            cacheKey,
            _ => ResolveSampleFilePathFromDirectoryUncached(directoryPath, sampleSet, hitSound, index));
    }

    private string ResolveSampleFilePathFromDirectoryUncached(string directoryPath, SampleSet sampleSet, HitSound hitSound, int index)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return string.Empty;
        }

        var baseName = BuildSampleBaseName(sampleSet, hitSound, index);
        foreach (var extension in _sampleExtensions)
        {
            var path = Path.Combine(directoryPath, baseName + extension);
            if (File.Exists(path))
            {
                return path;
            }
        }

        if (index <= 1)
        {
            return string.Empty;
        }

        var fallbackBaseName = BuildSampleBaseName(sampleSet, hitSound, 1);
        foreach (var extension in _sampleExtensions)
        {
            var path = Path.Combine(directoryPath, fallbackBaseName + extension);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    private static string BuildSampleBaseName(SampleSet sampleSet, HitSound hitSound, int index)
    {
        var prefix = sampleSet switch
        {
            SampleSet.Soft => "soft",
            SampleSet.Drum => "drum",
            _ => "normal"
        };

        var hitSoundFlags = (int)hitSound;
        var suffix = hitSound switch
        {
            HitSound.Whistle => "hitwhistle",
            HitSound.Finish => "hitfinish",
            HitSound.Clap => "hitclap",
            _ when (hitSoundFlags & (int)HitSound.Clap) == (int)HitSound.Clap => "hitclap",
            _ when (hitSoundFlags & (int)HitSound.Finish) == (int)HitSound.Finish => "hitfinish",
            _ when (hitSoundFlags & (int)HitSound.Whistle) == (int)HitSound.Whistle => "hitwhistle",
            _ => "hitnormal"
        };

        var indexSuffix = index > 1 ? index.ToString() : string.Empty;
        return $"{prefix}-{suffix}{indexSuffix}";
    }

    private static async Task PlayToneFallbackAsync(HitSoundVisualizerPoint point, CancellationToken cancellationToken)
    {
        var frequency = (point.SampleSet, point.HitSound) switch
        {
            (SampleSet.Normal, HitSound.Normal) => 900,
            (SampleSet.Normal, HitSound.Whistle) => 1200,
            (SampleSet.Normal, HitSound.Finish) => 700,
            (SampleSet.Normal, HitSound.Clap) => 1000,
            (SampleSet.Soft, HitSound.Normal) => 650,
            (SampleSet.Soft, HitSound.Whistle) => 850,
            (SampleSet.Soft, HitSound.Finish) => 500,
            (SampleSet.Soft, HitSound.Clap) => 760,
            (SampleSet.Drum, HitSound.Normal) => 420,
            (SampleSet.Drum, HitSound.Whistle) => 560,
            (SampleSet.Drum, HitSound.Finish) => 320,
            (SampleSet.Drum, HitSound.Clap) => 620,
            _ => 880
        };

        const int duration = 26;

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!OperatingSystem.IsWindows())
                {
                    return;
                }

                Console.Beep(frequency, duration);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            // Silent fallback when local platform cannot emit beeps.
        }
    }

    private static string FormatTimeLabel(int timeMs)
    {
        var clamped = Math.Max(0, timeMs);
        var ts = TimeSpan.FromMilliseconds(clamped);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}"
            : $"{ts.Minutes}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }

    private void PersistVisualizerVolumeSettings()
    {
        try
        {
            var settings = settingsService.GetMainSettings();
            var songVolume = Math.Clamp(SongVolumePercent, 0, 100);
            var hitsoundVolume = Math.Clamp(HitSoundVolumePercent, 0, 100);

            if (settings.AudioPreviewSongVolumePercent == songVolume &&
                settings.AudioPreviewHitSoundVolumePercent == hitsoundVolume)
            {
                return;
            }

            settings.AudioPreviewSongVolumePercent = songVolume;
            settings.AudioPreviewHitSoundVolumePercent = hitsoundVolume;
            settingsService.SaveMainSettings(settings);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            // Ignore settings persistence failures to avoid breaking volume interaction.
        }
    }

    private static int LoadPersistedSongVolumePercent(ISettingsService settingsService)
    {
        try
        {
            var settings = settingsService.GetMainSettings();
            return Math.Clamp(settings.AudioPreviewSongVolumePercent, 0, 100);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return 80;
        }
    }

    private static int LoadPersistedHitSoundVolumePercent(ISettingsService settingsService)
    {
        try
        {
            var settings = settingsService.GetMainSettings();
            return Math.Clamp(settings.AudioPreviewHitSoundVolumePercent, 0, 100);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return 100;
        }
    }

    private sealed class TimelineHistoryState(
        IReadOnlyList<HitSoundVisualizerPoint> points,
        IReadOnlyList<HitSoundVisualizerSampleChange> sampleChanges,
        int cursorTimeMs,
        int contextSamplePointTimeMs,
        bool isSampleRowContextActive,
        int selectedPointId,
        IReadOnlyList<int> selectedPointIds)
    {
        public IReadOnlyList<HitSoundVisualizerPoint> Points { get; } = points;
        public IReadOnlyList<HitSoundVisualizerSampleChange> SampleChanges { get; } = sampleChanges;
        public int CursorTimeMs { get; } = cursorTimeMs;
        public int ContextSamplePointTimeMs { get; } = contextSamplePointTimeMs;
        public bool IsSampleRowContextActive { get; } = isSampleRowContextActive;
        public int SelectedPointId { get; } = selectedPointId;
        public IReadOnlyList<int> SelectedPointIds { get; } = selectedPointIds;
    }

    private sealed class PointClipboardPayload
    {
        public string Format { get; set; } = string.Empty;
        public List<PointClipboardItem> Points { get; set; } = [];
    }

    private sealed class PointClipboardItem
    {
        public int OffsetMs { get; set; }
        public string? SampleSet { get; set; }
        public string? HitSound { get; set; }
    }

    private static bool IsAutoSampleSetDisplay(string value)
    {
        return string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase);
    }

    private SampleSet ResolveAutoSampleSetForPointAtTime(HitSound hitSound, int timeMs)
    {
        var sameTimePoints = Points
            .Where(point => point.TimeMs == timeMs)
            .ToList();

        var normalFromColumn = sameTimePoints.FirstOrDefault(point => point.HitSound == HitSound.Normal)?.SampleSet;
        var resolvedNormal = normalFromColumn ?? ResolveHeaderBankAtTime(isNormalBank: true, timeMs);

        if (hitSound == HitSound.Normal)
        {
            return resolvedNormal;
        }

        var additionFromColumn = sameTimePoints.FirstOrDefault(point => point.HitSound != HitSound.Normal)?.SampleSet;
        return additionFromColumn ?? ResolveHeaderBankAtTime(isNormalBank: false, timeMs, resolvedNormal);
    }

    private static SampleSet ParseSampleSet(string display) => display switch
    {
        "Soft" => SampleSet.Soft,
        "Drum" => SampleSet.Drum,
        _ => SampleSet.Normal
    };

    private static string SampleSetToDisplay(SampleSet sampleSet) => sampleSet switch
    {
        SampleSet.Soft => "Soft",
        SampleSet.Drum => "Drum",
        _ => "Normal"
    };

    private static HitSound ParseHitSound(string display) => display switch
    {
        "Whistle" => HitSound.Whistle,
        "Finish" => HitSound.Finish,
        "Clap" => HitSound.Clap,
        _ => HitSound.Normal
    };

    private static string HitSoundToDisplay(HitSound hitSound) => hitSound switch
    {
        HitSound.Whistle => "Whistle",
        HitSound.Finish => "Finish",
        HitSound.Clap => "Clap",
        _ => "Hitnormal"
    };

    private static int HitSoundSortOrder(HitSound hitSound) => hitSound switch
    {
        HitSound.Normal => 0,
        HitSound.Whistle => 1,
        HitSound.Finish => 2,
        HitSound.Clap => 3,
        _ => 9
    };

    private static int SampleSetSortOrder(SampleSet sampleSet) => sampleSet switch
    {
        SampleSet.Normal => 0,
        SampleSet.Soft => 1,
        SampleSet.Drum => 2,
        _ => 9
    };
}
