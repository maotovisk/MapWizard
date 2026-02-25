using System;
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
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Models.HitSoundVisualizer;
using MapWizard.Desktop.Services;
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
    private const string PointClipboardFormatId = "MapWizard.HitSoundVisualizer.Points/v1";
    private readonly string[] _sampleExtensions = [".wav", ".ogg", ".mp3"];
    private int _nextPointId = 1;
    private string _loadedMapsetDirectoryPath = string.Empty;
    private string _loadedAudioFilePath = string.Empty;
    private string _legacySkinDirectoryPath = string.Empty;
    private HitSoundTimeline _workingTimeline = new();
    private bool _suppressHeaderBankQuickApply;
    private CancellationTokenSource? _transportCts;
    private Stopwatch? _transportStopwatch;
    private int _transportStartTimeMs;
    private bool _transportUsesSongClock;

    [ObservableProperty] private SelectedMap _originBeatmap = new();
    [ObservableProperty] private string _preferredDirectory = string.Empty;
    [ObservableProperty] private string _loadedMapTitle = string.Empty;
    [ObservableProperty] private bool _hasLoadedMap;
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
    [ObservableProperty] private string _selectedPointSummary = "No point selected.";
    [ObservableProperty] private bool _isTransportPlaying;
    [ObservableProperty] private bool _isTransportPaused;
    [ObservableProperty] private bool _followPlaybackCursor = true;
    [ObservableProperty] private int _transportSeekMs;
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

    public IReadOnlyList<string> SampleSetNames { get; } = ["Normal", "Soft", "Drum"];
    public IReadOnlyList<string> HeaderBankNames { get; } = ["Auto", "Normal", "Soft", "Drum"];
    public IReadOnlyList<string> SelectorBankFilterNames { get; } = ["Any", "Normal", "Soft", "Drum"];
    public IReadOnlyList<string> HitSoundNames { get; } = ["Hitnormal", "Whistle", "Finish", "Clap"];
    public IReadOnlyList<int> SnapDivisorOptions { get; } = Enumerable.Range(1, 16).ToList();

    public double ViewEndMs => Math.Min(TimelineEndMs, ViewStartMs + Math.Max(100, ViewWindowMs));
    public int VisiblePointCount => Points.Count(x => x.TimeMs >= ViewStartMs && x.TimeMs <= ViewEndMs);
    public bool HasSelectedPoint => SelectedPointId >= 0;
    public int SelectedPointCount => SelectedPointIds.Count;
    public string CursorTimeText => FormatTimeLabel(CursorTimeMs);
    public string TimelineEndText => FormatTimeLabel((int)Math.Round(TimelineEndMs));
    public string TransportButtonText => IsTransportPlaying ? "Pause" : (IsTransportPaused ? "Resume" : "Play");
    public string AudioSourceStatus => string.IsNullOrWhiteSpace(_loadedAudioFilePath)
        ? "Song audio unavailable (hitsound-only playback)."
        : $"Song: {Path.GetFileName(_loadedAudioFilePath)}";
    public string LegacySkinStatus => string.IsNullOrWhiteSpace(_legacySkinDirectoryPath)
        ? "Legacy fallback skin: not found (place osu-resources legacy skin locally)."
        : $"Legacy fallback skin: {Path.GetFileName(_legacySkinDirectoryPath)}";
    public string SongVolumeText => $"{Math.Clamp(SongVolumePercent, 0, 100)}%";
    public string HitSoundVolumeText => $"{Math.Clamp(HitSoundVolumePercent, 0, 100)}%";
    public string SelectedSnapDivisorText => $"1/{SelectedSnapDivisorDenominator}";
    public string ContextSamplePointTimeText => FormatTimeLabel(ContextSamplePointTimeMs);
    public bool ShowSamplePointContextPopup => IsSampleRowContextActive;
    public bool ShowHitsoundContextPopup => !IsSampleRowContextActive;
    public bool CanEditHeaderBanks => HasLoadedMap && (IsSampleRowContextActive || HasSelectedPoint);
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
        OnPropertyChanged(nameof(VisiblePointCount));
    }

    partial void OnCursorTimeMsChanged(int value)
    {
        OnPropertyChanged(nameof(CursorTimeText));
        TransportSeekMs = value;
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
    }

    partial void OnIsTransportPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(TransportButtonText));
    }

    partial void OnIsTransportPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(TransportButtonText));
    }

    partial void OnSongVolumePercentChanged(int value)
    {
        SongVolumePercent = Math.Clamp(value, 0, 100);
        audioPlaybackService.SetSongVolume(SongVolumePercent / 100f);
        PersistVisualizerVolumeSettings();
        OnPropertyChanged(nameof(SongVolumeText));
    }

    partial void OnHitSoundVolumePercentChanged(int value)
    {
        HitSoundVolumePercent = Math.Clamp(value, 0, 100);
        audioPlaybackService.SetHitsoundVolume(HitSoundVolumePercent / 100f);
        PersistVisualizerVolumeSettings();
        OnPropertyChanged(nameof(HitSoundVolumeText));
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
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", ex.Message);
        }
    }

    [RelayCommand]
    private void SetOriginFromMemory()
    {
        var currentBeatmap = BeatmapSelectionUtils.TryGetBeatmapFromMemory(
            osuMemoryReaderService,
            (type, title, message) => toastManager.ShowToast(type, title, message),
            "Memory Error",
            "Something went wrong while getting the beatmap path from memory.",
            "No Beatmap",
            "No beatmap found in memory.");

        if (currentBeatmap is null)
        {
            return;
        }

        SetOriginBeatmapPath(currentBeatmap);
    }

    [RelayCommand]
    private void LoadTimeline()
    {
        if (string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", "Please select a beatmap first.");
            return;
        }

        try
        {
            RefreshPersistedPlaybackVolumes();
            StopTransportCore(resetPausedState: true);
            var document = hitSoundService.LoadHitsoundVisualizerDocument(OriginBeatmap.Path);

            _loadedMapsetDirectoryPath = document.MapsetDirectoryPath;
            _loadedAudioFilePath = document.AudioFilePath;
            _legacySkinDirectoryPath = ResolveLegacySkinDirectory();
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
            ViewStartMs = 0;
            ViewWindowMs = Math.Min(12000, TimelineEndMs);
            CursorTimeMs = 0;
            ContextSamplePointTimeMs = 0;
            TransportSeekMs = 0;
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
            HasLoadedMap = true;

            audioPlaybackService.SetSongVolume(SongVolumePercent / 100f);
            audioPlaybackService.SetHitsoundVolume(HitSoundVolumePercent / 100f);

            UpdateTimelineStats();
            UpdateSelectedPointSummary();
            OnPropertyChanged(nameof(AudioSourceStatus));
            OnPropertyChanged(nameof(LegacySkinStatus));
            OnPropertyChanged(nameof(TimelineEndText));
            PlaybackStatus = "Loaded";
        }
        catch (Exception ex)
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", ex.Message);
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

        if (IsTransportPlaying)
        {
            RestartTransportAt(clamped);
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

        var point = new HitSoundVisualizerPoint
        {
            Id = _nextPointId++,
            TimeMs = Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs)),
            SampleSet = ParseSampleSet(SelectedSampleSetName),
            HitSound = ParseHitSound(SelectedHitSoundName),
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
        catch
        {
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

        foreach (var item in payload.Points.OrderBy(point => point.OffsetMs))
        {
            var pastedTime = anchorTimeMs + (item.OffsetMs - minOffset);
            var candidate = new HitSoundVisualizerPoint
            {
                Id = _nextPointId++,
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
            _nextPointId = Math.Max(1, _nextPointId - pastedPoints.Count);
            return;
        }

        foreach (var candidate in pastedPoints)
        {
            if (HasPointConflict(candidate, out var conflictMessage))
            {
                toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", conflictMessage);
                _nextPointId = Math.Max(1, _nextPointId - pastedPoints.Count);
                return;
            }
        }

        var updated = Points.ToList();
        updated.AddRange(pastedPoints);

        IsSampleRowContextActive = false;
        ApplyUpdatedPoints(updated, selectPointId: pastedPoints[0].Id);
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

        var edited = new HitSoundVisualizerPoint
        {
            Id = selected.Id,
            TimeMs = Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs)),
            SampleSet = ParseSampleSet(SelectedSampleSetName),
            HitSound = ParseHitSound(SelectedHitSoundName),
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

        var targetSampleSet = ParseSampleSet(SelectedSampleSetName);
        var targetHitSound = ParseHitSound(SelectedHitSoundName);
        var updated = Points
            .Select(point => ids.Contains(point.Id)
                ? new HitSoundVisualizerPoint
                {
                    Id = point.Id,
                    TimeMs = point.TimeMs,
                    SampleSet = targetSampleSet,
                    HitSound = targetHitSound,
                    IsDraggable = false
                }
                : point)
            .ToList();

        var duplicateLane = updated
            .GroupBy(x => (x.TimeMs, x.HitSound, x.SampleSet))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateLane is not null)
        {
            toastManager.ShowToast(
                NotificationType.Error,
                "Hitsound Visualizer",
                $"Batch edit would create duplicate points at {duplicateLane.Key.TimeMs}ms ({HitSoundToDisplay(duplicateLane.Key.HitSound)}).");
            return;
        }

        var primaryId = SelectedPointId > 0 ? SelectedPointId : ids.First();
        var selectedIds = ids.ToArray();
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

        StopTransportCore(resetPausedState: true);
        PlaybackStatus = $"Playing {point.TimeMs}ms";
        await PlayPointAsync(point, CancellationToken.None);
        PlaybackStatus = "Idle";
    }

    [RelayCommand]
    private void ToggleTransportPlayback()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        if (IsTransportPlaying)
        {
            PauseTransport();
            return;
        }

        if (IsTransportPaused)
        {
            ResumeTransport();
            return;
        }

        PlayTransportFromCursor();
    }

    [RelayCommand]
    private void PlayTransportFromCursor()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        var startTime = CursorTimeMs >= Math.Max(0, TimelineEndMs - 1) ? 0 : CursorTimeMs;
        StartTransportAt(startTime);
    }

    [RelayCommand]
    private void PauseTransport()
    {
        if (!IsTransportPlaying)
        {
            return;
        }

        var currentTime = GetCurrentTransportTimeMs();
        StopTransportCore(resetPausedState: false, stopSongPlayback: false);
        CursorTimeMs = currentTime;
        PlaybackStatus = "Paused";
        IsTransportPaused = true;
    }

    [RelayCommand]
    private void ResumeTransport()
    {
        if (!HasLoadedMap)
        {
            return;
        }

        StartTransportAt(CursorTimeMs);
    }

    [RelayCommand]
    private void StopTransport()
    {
        StopTransportCore(resetPausedState: true);
        PlaybackStatus = "Stopped";
    }

    [RelayCommand]
    private void CommitTransportSeek()
    {
        SeekTime(TransportSeekMs);
    }

    [RelayCommand]
    private void StopPlayback()
    {
        StopTransportCore(resetPausedState: true);
        PlaybackStatus = "Stopped";
    }

    private void StartTransportAt(int startTimeMs)
    {
        startTimeMs = Math.Clamp(startTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));

        StopTransportCore(resetPausedState: false);

        _transportStartTimeMs = startTimeMs;
        audioPlaybackService.SetSongVolume(SongVolumePercent / 100f);
        audioPlaybackService.SetHitsoundVolume(HitSoundVolumePercent / 100f);
        var songStarted = audioPlaybackService.LoadSong(_loadedAudioFilePath) && audioPlaybackService.PlaySong(startTimeMs);
        _transportUsesSongClock = songStarted;
        _transportStopwatch = Stopwatch.StartNew();
        var cts = new CancellationTokenSource();
        _transportCts = cts;

        CursorTimeMs = startTimeMs;
        TransportSeekMs = startTimeMs;
        IsTransportPlaying = true;
        IsTransportPaused = false;

        PlaybackStatus = songStarted
            ? $"Playing from {FormatTimeLabel(startTimeMs)}"
            : $"Playing hitsounds from {FormatTimeLabel(startTimeMs)}";

        _ = RunTransportLoopAsync(startTimeMs, cts);
    }

    private void RestartTransportAt(int startTimeMs)
    {
        if (!IsTransportPlaying)
        {
            return;
        }

        StartTransportAt(startTimeMs);
    }

    private int GetCurrentTransportTimeMs()
    {
        if (!IsTransportPlaying)
        {
            return Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        }

        var fallbackTime = _transportStartTimeMs;
        if (_transportStopwatch is not null)
        {
            fallbackTime = _transportStartTimeMs + (int)_transportStopwatch.ElapsedMilliseconds;
        }

        if (_transportUsesSongClock)
        {
            var songTime = Math.Clamp(audioPlaybackService.GetSongPositionMs(), 0, (int)Math.Ceiling(TimelineEndMs));

            // MiniAudio stream cursor can report 0/invalid briefly right after starting or seeking.
            // Use the local transport clock during this warm-up window, then lock to the audio clock
            // to prevent long-map drift between song and hitsounds.
            if (_transportStopwatch is not null)
            {
                var warmupMs = _transportStopwatch.ElapsedMilliseconds;
                var isWarmup = warmupMs < 300;
                var warmupDriftFromExpected = Math.Abs(songTime - fallbackTime);
                var isClearlyInvalidWarmupReading =
                    isWarmup &&
                    (
                        songTime == 0 && fallbackTime > 0 ||
                        warmupDriftFromExpected > 150
                    );

                if (!isClearlyInvalidWarmupReading)
                {
                    return songTime;
                }
            }
            else
            {
                return songTime;
            }
        }

        return Math.Clamp(fallbackTime, 0, (int)Math.Ceiling(TimelineEndMs));
    }

    private async Task RunTransportLoopAsync(int startTimeMs, CancellationTokenSource cts)
    {
        var token = cts.Token;
        var playbackPoints = Points
            .Where(x => x.TimeMs >= startTimeMs)
            .OrderBy(x => x.TimeMs)
            .ToList();

        var nextPointIndex = 0;
        var lastUiUpdateTicks = 0L;
        var sawSongPlaying = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_transportUsesSongClock && audioPlaybackService.IsSongPlaying)
                {
                    sawSongPlaying = true;
                }

                var currentTime = GetCurrentTransportTimeMs();

                while (nextPointIndex < playbackPoints.Count &&
                       playbackPoints[nextPointIndex].TimeMs <= currentTime)
                {
                    var point = playbackPoints[nextPointIndex++];
                    PlayPointForTransport(point, token);
                }

                var uiTick = Environment.TickCount64;
                if (uiTick - lastUiUpdateTicks >= 16)
                {
                    lastUiUpdateTicks = uiTick;
                    var uiTime = Math.Clamp(currentTime, 0, (int)Math.Ceiling(TimelineEndMs));
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (!ReferenceEquals(_transportCts, cts))
                        {
                            return;
                        }

                        UpdateTransportCursor(uiTime);
                    });
                }

                // When song audio is available, continue transport until the song actually ends instead of
                // cutting off at the last visualized hitsound/timing point (timeline end).
                var reachedEnd = !_transportUsesSongClock && currentTime >= TimelineEndMs;
                if (reachedEnd)
                {
                    break;
                }

                // Some beatmaps end playback before the visualizer timeline end (or the audio source stops
                // slightly early at EOF). Once we have observed song playback and it has stopped, end the
                // transport cleanly so replay can start again without leaving transport state "running".
                var songEndedNaturally = _transportUsesSongClock && sawSongPlaying && !audioPlaybackService.IsSongPlaying;
                if (songEndedNaturally)
                {
                    break;
                }

                var nextDue = nextPointIndex < playbackPoints.Count
                    ? Math.Max(1, playbackPoints[nextPointIndex].TimeMs - currentTime)
                    : 4;
                await Task.Delay((int)Math.Clamp(nextDue, 1, 4), token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!ReferenceEquals(_transportCts, cts))
            {
                return;
            }

            StopTransportCore(resetPausedState: true);
            CursorTimeMs = Math.Clamp((int)Math.Ceiling(TimelineEndMs), 0, (int)Math.Ceiling(TimelineEndMs));
            PlaybackStatus = "Playback complete";
        });
    }

    private void UpdateTransportCursor(int currentTimeMs)
    {
        if (FollowPlaybackCursor && IsTransportPlaying)
        {
            var window = Math.Max(100, ViewWindowMs);
            const double playheadAnchorRatio = 0.38d;
            var desiredStart = currentTimeMs - (window * playheadAnchorRatio);
            ViewStartMs = Math.Clamp(desiredStart, 0, Math.Max(0, TimelineEndMs - window));
        }

        CursorTimeMs = currentTimeMs;
    }

    private void StopTransportCore(bool resetPausedState, bool stopSongPlayback = true)
    {
        if (resetPausedState)
        {
            IsTransportPaused = false;
        }

        IsTransportPlaying = false;
        _transportStopwatch?.Stop();
        _transportStopwatch = null;

        _transportCts?.Cancel();
        _transportCts?.Dispose();
        _transportCts = null;
        if (_transportUsesSongClock)
        {
            if (stopSongPlayback)
            {
                audioPlaybackService.StopSong();
            }
            else
            {
                audioPlaybackService.PauseSong();
            }
        }

        _transportUsesSongClock = false;
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
            catch
            {
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
        catch
        {
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
        OriginBeatmap = new SelectedMap { Path = beatmapPath };
        PreferredDirectory = Path.GetDirectoryName(beatmapPath) ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(beatmapPath) && File.Exists(beatmapPath))
        {
            LoadTimeline();
        }
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

    private void ApplyUpdatedPoints(IReadOnlyCollection<HitSoundVisualizerPoint> updatedPoints, int selectPointId)
    {
        if (!TryRebuildWorkingTimeline(updatedPoints, out var timeline, out var error))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", error);
            return;
        }

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
    }

    private void ApplyUpdatedSampleChanges(IReadOnlyCollection<HitSoundVisualizerSampleChange> updatedSampleChanges, int focusTimeMs)
    {
        if (!TryRebuildWorkingTimeline(Points.ToList(), out var timeline, out var error, updatedSampleChanges))
        {
            toastManager.ShowToast(NotificationType.Error, "Hitsound Visualizer", error);
            return;
        }

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
        SelectedSampleSetName = SampleSetToDisplay(point.SampleSet);
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
                HeaderNormalBankName = "Normal";
                HeaderAdditionBankName = "Auto";
                return;
            }

            var sameTimePoints = Points
                .Where(x => x.TimeMs == primaryPoint.TimeMs)
                .OrderBy(x => HitSoundSortOrder(x.HitSound))
                .ToList();

            var normalBank = sameTimePoints.FirstOrDefault(x => x.HitSound == HitSound.Normal)?.SampleSet
                             ?? primaryPoint.SampleSet;
            var additionBank = sameTimePoints.FirstOrDefault(x => x.HitSound != HitSound.Normal)?.SampleSet
                               ?? normalBank;

            HeaderNormalBankName = SampleSetToDisplay(normalBank);
            HeaderAdditionBankName = additionBank == normalBank
                ? "Auto"
                : SampleSetToDisplay(additionBank);
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

        if (string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sampleSet = ParseSampleSet(value);

        if (IsSampleRowContextActive)
        {
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
            return;
        }

        ApplySampleSetToSelectionSubset(isNormalBank, sampleSet);
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

    private void ApplySampleSetToSelectionSubset(bool affectNormalPoints, SampleSet sampleSet)
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

        bool IsTargetPoint(HitSoundVisualizerPoint point) =>
            selectedIds.Contains(point.Id) &&
            (affectNormalPoints ? point.HitSound == HitSound.Normal : point.HitSound != HitSound.Normal);

        if (!Points.Any(IsTargetPoint))
        {
            return;
        }

        var updated = Points
            .Select(point => IsTargetPoint(point)
                ? new HitSoundVisualizerPoint
                {
                    Id = point.Id,
                    TimeMs = point.TimeMs,
                    SampleSet = sampleSet,
                    HitSound = point.HitSound,
                    IsDraggable = false
                }
                : point)
            .ToList();

        var primaryId = SelectedPointId > 0 ? SelectedPointId : selectedIds.First();
        var selectedIdArray = selectedIds.ToArray();
        ApplyUpdatedPoints(updated, selectPointId: primaryId);
        ApplySelection(selectedIdArray, primaryId);
        UpdateSelectedPointSummary();
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

            events.Add(new SoundEvent(
                TimeSpan.FromMilliseconds(timeGroup.Key),
                distinctSounds,
                normalSample,
                additionSample));
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

    private void NormalizeViewWindow()
    {
        ViewWindowMs = Math.Clamp(ViewWindowMs, 100, Math.Max(100, TimelineEndMs));
        ViewStartMs = Math.Clamp(ViewStartMs, 0, Math.Max(0, TimelineEndMs - ViewWindowMs));
        CursorTimeMs = Math.Clamp(CursorTimeMs, 0, (int)Math.Ceiling(TimelineEndMs));
        TransportSeekMs = Math.Clamp(TransportSeekMs, 0, (int)Math.Ceiling(TimelineEndMs));
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

    private void PlayPointForTransport(HitSoundVisualizerPoint point, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            if (TryPlayPointSample(point))
            {
                return;
            }

            _ = PlayToneFallbackAsync(point, cancellationToken);
        }
        catch
        {
            // Ignore point playback failures during transport preview.
        }
    }

    private bool TryPlayPointSample(HitSoundVisualizerPoint point)
    {
        var sampleFilePath = ResolveSampleFilePath(point);
        return !string.IsNullOrWhiteSpace(sampleFilePath) && audioPlaybackService.PlayHitsound(sampleFilePath);
    }

    private string ResolveSampleFilePath(HitSoundVisualizerPoint point)
    {
        var sampleChange = SampleChanges
            .Where(x => x.TimeMs <= point.TimeMs)
            .OrderBy(x => x.TimeMs)
            .LastOrDefault();

        var index = Math.Max(1, sampleChange?.Index ?? 1);
        var mapsetPath = ResolveSampleFilePathFromDirectory(_loadedMapsetDirectoryPath, point.SampleSet, point.HitSound, index);
        if (!string.IsNullOrWhiteSpace(mapsetPath))
        {
            return mapsetPath;
        }

        return ResolveSampleFilePathFromDirectory(_legacySkinDirectoryPath, point.SampleSet, point.HitSound, index);
    }

    private string ResolveSampleFilePathFromDirectory(string directoryPath, SampleSet sampleSet, HitSound hitSound, int index)
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

        var suffix = hitSound switch
        {
            HitSound.Whistle => "hitwhistle",
            HitSound.Finish => "hitfinish",
            HitSound.Clap => "hitclap",
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
        catch
        {
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

            if (settings.HitSoundVisualizerSongVolumePercent == songVolume &&
                settings.HitSoundVisualizerHitSoundVolumePercent == hitsoundVolume)
            {
                return;
            }

            settings.HitSoundVisualizerSongVolumePercent = songVolume;
            settings.HitSoundVisualizerHitSoundVolumePercent = hitsoundVolume;
            settingsService.SaveMainSettings(settings);
        }
        catch
        {
            // Ignore settings persistence failures to avoid breaking volume interaction.
        }
    }

    private static int LoadPersistedSongVolumePercent(ISettingsService settingsService)
    {
        try
        {
            var settings = settingsService.GetMainSettings();
            return Math.Clamp(settings.HitSoundVisualizerSongVolumePercent, 0, 100);
        }
        catch
        {
            return 80;
        }
    }

    private static int LoadPersistedHitSoundVolumePercent(ISettingsService settingsService)
    {
        try
        {
            var settings = settingsService.GetMainSettings();
            return Math.Clamp(settings.HitSoundVisualizerHitSoundVolumePercent, 0, 100);
        }
        catch
        {
            return 100;
        }
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
