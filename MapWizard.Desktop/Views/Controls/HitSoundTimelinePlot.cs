using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using MapWizard.Desktop.Models.HitSoundVisualizer;
using BeatmapParser.Enums;

namespace MapWizard.Desktop.Views.Controls;

public class HitSoundTimelinePlot : Control
{
    private const double DefaultRowHeight = 34d;
    private const double PointRadius = 5d;
    private const int SampleRowIndex = 0;
    private const int HitSoundRowStartIndex = 1;
    private const int TotalRows = 5;
    private const double PointHitTestRadius = 10d;
    private const double SampleChangeHitTestRadius = 10d;
    private const double SampleChangeBarMinWidth = 18d;
    private const double SampleChangeBarHorizontalPadding = 2d;
    private const double SampleChangeBarVerticalPadding = 5d;
    private const double SampleChangeBarCornerRadius = 4d;
    private const double SampleChangeTextPadding = 6d;
    private const double SampleChangeTextFontSize = 10d;
    private const double SampleChangeDenseLineSpacingPx = 2d;
    private const double SampleChangeDenseModeMaxAvgSpacingPx = 36d;
    private const double SnapTickTargetMinSpacingPx = 3d;
    private const double SnapTickMaxSimplifiedSpacingPx = 14d;
    private const double SnapTickDenseStrongTargetMinSpacingPx = 5d;
    private const double SnapTickDenseMeasuresTargetMinSpacingPx = 9d;
    private const double SnapTickDenseMeasuresMaxSpacingPx = 20d;
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#14181D"));
    private static readonly IBrush SampleRowBrush = new SolidColorBrush(Color.Parse("#1F2430"));
    private static readonly IBrush EvenRowBrush = new SolidColorBrush(Color.Parse("#151B22"));
    private static readonly IBrush OddRowBrush = new SolidColorBrush(Color.Parse("#131920"));
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.Parse("#2B3440")), 1);
    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.Parse("#324050")), 1);
    private static readonly Pen PointOutlinePen = new(new SolidColorBrush(Color.Parse("#0C1117")), 1);
    private static readonly IBrush LightBackgroundBrush = new SolidColorBrush(Color.Parse("#F6F8FB"));
    private static readonly IBrush LightSampleRowBrush = new SolidColorBrush(Color.Parse("#E8EEF6"));
    private static readonly IBrush LightEvenRowBrush = new SolidColorBrush(Color.Parse("#F8FAFD"));
    private static readonly IBrush LightOddRowBrush = new SolidColorBrush(Color.Parse("#F1F5FA"));
    private static readonly Pen LightGridPen = new(new SolidColorBrush(Color.Parse("#CCD8E6")), 1);
    private static readonly Pen LightBorderPen = new(new SolidColorBrush(Color.Parse("#B8C7D8")), 1);
    private static readonly Pen LightPointOutlinePen = new(new SolidColorBrush(Color.Parse("#E6ECF4")), 1);
    private static readonly Pen DenseNormalPen = new(new SolidColorBrush(Color.Parse("#9BC53D")), 1.4);
    private static readonly Pen DenseSoftPen = new(new SolidColorBrush(Color.Parse("#5BC0EB")), 1.4);
    private static readonly Pen DenseDrumPen = new(new SolidColorBrush(Color.Parse("#E55934")), 1.4);
    private static readonly Pen DenseAutoPen = new(new SolidColorBrush(Color.Parse("#8A96A3")), 1.4);
    private static readonly IBrush NormalPointBrush = new SolidColorBrush(Color.Parse("#9BC53D"));
    private static readonly IBrush SoftPointBrush = new SolidColorBrush(Color.Parse("#5BC0EB"));
    private static readonly IBrush DrumPointBrush = new SolidColorBrush(Color.Parse("#E55934"));
    private static readonly IBrush AutoPointBrush = new SolidColorBrush(Color.Parse("#8A96A3"));
    private static readonly Pen PrimarySelectionPen = new(new SolidColorBrush(Color.Parse("#FFD166")), 2);
    private static readonly Pen SecondarySelectionPen = new(new SolidColorBrush(Color.Parse("#FFFFFF")), 1.2);
    private static readonly Pen LightSecondarySelectionPen = new(new SolidColorBrush(Color.Parse("#1F2937")), 1.2);
    private static readonly Pen SampleChangeSelectionPen = new(new SolidColorBrush(Color.Parse("#89E5FF")), 1.8);
    private static readonly Pen SampleChangeSelectionAccentPen = new(new SolidColorBrush(Color.Parse("#F5FBFF")), 1);
    private static readonly Pen LightSampleChangeSelectionAccentPen = new(new SolidColorBrush(Color.Parse("#0F172A")), 1);
    private static readonly IBrush SampleChangeLabelBrush = new SolidColorBrush(Color.Parse("#E6EDF7"));
    private static readonly IBrush LightSampleChangeLabelBrush = new SolidColorBrush(Color.Parse("#0F172A"));
    private static readonly Typeface SampleChangeLabelTypeface = new("Consolas");
    private static readonly Pen NormalSampleChangeLinePen = new(new SolidColorBrush(Color.Parse("#9BC53D")), 1);
    private static readonly Pen SoftSampleChangeLinePen = new(new SolidColorBrush(Color.Parse("#5BC0EB")), 1);
    private static readonly Pen DrumSampleChangeLinePen = new(new SolidColorBrush(Color.Parse("#E55934")), 1);
    private static readonly Pen CursorPen = new(new SolidColorBrush(Color.Parse("#F4C95D")), 1.5);
    private static readonly Pen GhostPreviewPen = new(new SolidColorBrush(Color.Parse("#B3FFD166")), 1.4);
    private static readonly IBrush GhostPreviewFillBrush = new SolidColorBrush(Color.Parse("#33FFD166"));
    private static readonly Pen MeasureTickPen = new(new SolidColorBrush(Color.Parse("#FFFFFF")), 1.6);
    private static readonly Pen LightMeasureTickPen = new(new SolidColorBrush(Color.Parse("#334155")), 1.6);
    private static readonly Pen HalfTickPen = new(new SolidColorBrush(Color.Parse("#FF5A5A")), 1.2);
    private static readonly Pen TripletTickPen = new(new SolidColorBrush(Color.Parse("#C99BFF")), 1.2);
    private static readonly Pen QuarterTickPen = new(new SolidColorBrush(Color.Parse("#4EA3FF")), 1.2);
    private static readonly Pen SixthTickPen = new(new SolidColorBrush(Color.Parse("#8E5CFF")), 1.0);
    private static readonly Pen EighthTickPen = new(new SolidColorBrush(Color.Parse("#F0D94B")), 1.0);
    private static readonly Pen GenericTickPen = new(new SolidColorBrush(Color.Parse("#7A848F")), 1.0);
    private static readonly Pen LightGenericTickPen = new(new SolidColorBrush(Color.Parse("#64748B")), 1.0);
    private bool _isMiddlePanning;
    private bool _isRangeSelecting;
    private bool _isSelectionDragActive;
    private bool _isAdditiveSelection;
    private Point _selectionStartPosition;
    private Point _selectionCurrentPosition;
    private Point _lastPanPosition;
    private bool _showPlacementGhost;
    private int _placementGhostRowIndex = -1;
    private int _placementGhostTimeMs = -1;
    private HitSoundVisualizerPoint[] _pointsCache = [];
    private HitSoundVisualizerSampleChange[] _sampleChangesCache = [];
    private HitSoundVisualizerSnapTick[] _snapTicksCache = [];
    private HashSet<int> _selectedPointIdsCache = [];

    public static readonly StyledProperty<IEnumerable<HitSoundVisualizerPoint>?> PointsProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, IEnumerable<HitSoundVisualizerPoint>?>(nameof(Points));

    public static readonly StyledProperty<IEnumerable<HitSoundVisualizerSampleChange>?> SampleChangesProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, IEnumerable<HitSoundVisualizerSampleChange>?>(nameof(SampleChanges));

    public static readonly StyledProperty<IEnumerable<HitSoundVisualizerSnapTick>?> SnapTicksProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, IEnumerable<HitSoundVisualizerSnapTick>?>(nameof(SnapTicks));

    public static readonly StyledProperty<double> ViewStartMsProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, double>(nameof(ViewStartMs));

    public static readonly StyledProperty<double> ViewEndMsProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, double>(nameof(ViewEndMs), 1000d);

    public static readonly StyledProperty<int> CursorTimeMsProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, int>(nameof(CursorTimeMs));

    public static readonly StyledProperty<int> SnapDivisorDenominatorProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, int>(nameof(SnapDivisorDenominator), 4);

    public static readonly StyledProperty<bool> IsPlaybackRunningProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, bool>(nameof(IsPlaybackRunning));

    public static readonly StyledProperty<int> SelectedPointIdProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, int>(nameof(SelectedPointId), -1);

    public static readonly StyledProperty<int> SelectedSampleChangeTimeMsProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, int>(nameof(SelectedSampleChangeTimeMs), -1);

    public static readonly StyledProperty<IEnumerable<int>?> SelectedPointIdsProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, IEnumerable<int>?>(nameof(SelectedPointIds));

    public static readonly StyledProperty<ICommand?> SelectPointCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(SelectPointCommand));

    public static readonly StyledProperty<ICommand?> SelectPointsCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(SelectPointsCommand));

    public static readonly StyledProperty<ICommand?> AddPointsToSelectionCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(AddPointsToSelectionCommand));

    public static readonly StyledProperty<ICommand?> TogglePointSelectionCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(TogglePointSelectionCommand));

    public static readonly StyledProperty<ICommand?> AddPointOnSnapLineCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(AddPointOnSnapLineCommand));

    public static readonly StyledProperty<ICommand?> SeekTimeCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(SeekTimeCommand));

    public static readonly StyledProperty<ICommand?> PanTimelineCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(PanTimelineCommand));

    public static readonly StyledProperty<ICommand?> ZoomTimelineCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(ZoomTimelineCommand));

    public static readonly StyledProperty<ICommand?> OpenContextEditorCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(OpenContextEditorCommand));

    public static readonly StyledProperty<ICommand?> DeleteSelectedPointCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(DeleteSelectedPointCommand));

    public static readonly StyledProperty<ICommand?> DeleteSampleChangeCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(DeleteSampleChangeCommand));

    static HitSoundTimelinePlot()
    {
        AffectsRender<HitSoundTimelinePlot>(
            PointsProperty,
            SampleChangesProperty,
            SnapTicksProperty,
            ViewStartMsProperty,
            ViewEndMsProperty,
            CursorTimeMsProperty,
            SnapDivisorDenominatorProperty,
            SelectedPointIdProperty,
            SelectedSampleChangeTimeMsProperty,
            SelectedPointIdsProperty);
    }

    public IEnumerable<HitSoundVisualizerPoint>? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IEnumerable<HitSoundVisualizerSampleChange>? SampleChanges
    {
        get => GetValue(SampleChangesProperty);
        set => SetValue(SampleChangesProperty, value);
    }

    public IEnumerable<HitSoundVisualizerSnapTick>? SnapTicks
    {
        get => GetValue(SnapTicksProperty);
        set => SetValue(SnapTicksProperty, value);
    }

    public double ViewStartMs
    {
        get => GetValue(ViewStartMsProperty);
        set => SetValue(ViewStartMsProperty, value);
    }

    public double ViewEndMs
    {
        get => GetValue(ViewEndMsProperty);
        set => SetValue(ViewEndMsProperty, value);
    }

    public int CursorTimeMs
    {
        get => GetValue(CursorTimeMsProperty);
        set => SetValue(CursorTimeMsProperty, value);
    }

    public int SnapDivisorDenominator
    {
        get => GetValue(SnapDivisorDenominatorProperty);
        set => SetValue(SnapDivisorDenominatorProperty, value);
    }

    public bool IsPlaybackRunning
    {
        get => GetValue(IsPlaybackRunningProperty);
        set => SetValue(IsPlaybackRunningProperty, value);
    }

    public int SelectedPointId
    {
        get => GetValue(SelectedPointIdProperty);
        set => SetValue(SelectedPointIdProperty, value);
    }

    public int SelectedSampleChangeTimeMs
    {
        get => GetValue(SelectedSampleChangeTimeMsProperty);
        set => SetValue(SelectedSampleChangeTimeMsProperty, value);
    }

    public IEnumerable<int>? SelectedPointIds
    {
        get => GetValue(SelectedPointIdsProperty);
        set => SetValue(SelectedPointIdsProperty, value);
    }

    public ICommand? SelectPointCommand
    {
        get => GetValue(SelectPointCommandProperty);
        set => SetValue(SelectPointCommandProperty, value);
    }

    public ICommand? SelectPointsCommand
    {
        get => GetValue(SelectPointsCommandProperty);
        set => SetValue(SelectPointsCommandProperty, value);
    }

    public ICommand? AddPointsToSelectionCommand
    {
        get => GetValue(AddPointsToSelectionCommandProperty);
        set => SetValue(AddPointsToSelectionCommandProperty, value);
    }

    public ICommand? TogglePointSelectionCommand
    {
        get => GetValue(TogglePointSelectionCommandProperty);
        set => SetValue(TogglePointSelectionCommandProperty, value);
    }

    public ICommand? AddPointOnSnapLineCommand
    {
        get => GetValue(AddPointOnSnapLineCommandProperty);
        set => SetValue(AddPointOnSnapLineCommandProperty, value);
    }

    public ICommand? SeekTimeCommand
    {
        get => GetValue(SeekTimeCommandProperty);
        set => SetValue(SeekTimeCommandProperty, value);
    }

    public ICommand? PanTimelineCommand
    {
        get => GetValue(PanTimelineCommandProperty);
        set => SetValue(PanTimelineCommandProperty, value);
    }

    public ICommand? ZoomTimelineCommand
    {
        get => GetValue(ZoomTimelineCommandProperty);
        set => SetValue(ZoomTimelineCommandProperty, value);
    }

    public ICommand? OpenContextEditorCommand
    {
        get => GetValue(OpenContextEditorCommandProperty);
        set => SetValue(OpenContextEditorCommandProperty, value);
    }

    public ICommand? DeleteSelectedPointCommand
    {
        get => GetValue(DeleteSelectedPointCommandProperty);
        set => SetValue(DeleteSelectedPointCommandProperty, value);
    }

    public ICommand? DeleteSampleChangeCommand
    {
        get => GetValue(DeleteSampleChangeCommandProperty);
        set => SetValue(DeleteSampleChangeCommandProperty, value);
    }

    private bool UseLightPalette => ActualThemeVariant == ThemeVariant.Light;

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        var rowHeight = DefaultRowHeight;
        var timelineWidth = bounds.Width;
        var timelineHeight = Math.Max(bounds.Height, TotalRows * rowHeight);
        var timelineRect = new Rect(0, 0, timelineWidth, timelineHeight);
        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);

        context.FillRectangle(CurrentBackgroundBrush(), timelineRect);

        DrawRowBackgrounds(context, timelineWidth, rowHeight, TotalRows);
        DrawTicks(context, timelineWidth, timelineHeight, windowMs);
        DrawCursor(context, timelineWidth, timelineHeight, windowMs);
        DrawSampleChanges(context, timelineWidth, rowHeight, windowMs);
        DrawPoints(context, timelineWidth, rowHeight, windowMs);
        DrawPlacementGhost(context, timelineWidth, rowHeight, windowMs);
        DrawSelectionBox(context, timelineWidth, rowHeight, windowMs);
        DrawGridLines(context, timelineWidth, rowHeight, TotalRows);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PointsProperty)
        {
            RebuildPointsCache();
        }
        else if (change.Property == SampleChangesProperty)
        {
            RebuildSampleChangesCache();
        }
        else if (change.Property == SnapTicksProperty)
        {
            RebuildSnapTicksCache();
        }
        else if (change.Property == SelectedPointIdsProperty)
        {
            RebuildSelectedPointIdsCache();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        if (point.Properties.IsMiddleButtonPressed)
        {
            _isMiddlePanning = true;
            _lastPanPosition = point.Position;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsRightButtonPressed)
        {
            HandleRightClick(point.Position, bounds, e.KeyModifiers);
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);
        var clickPosition = point.Position;
        var clickedRow = YToRowIndex(clickPosition.Y, DefaultRowHeight);
        var clickedTimeMs = ResolveClickedTimeMs(clickPosition.X, bounds.Width, windowMs, snapTolerancePx: 12d);
        var (nearest, bestDistance) = FindNearestPoint(clickPosition, bounds.Width, windowMs);
        var (nearestSampleChange, sampleChangeDistance) = clickedRow == SampleRowIndex
            ? FindNearestSampleChange(clickPosition, bounds.Width, windowMs)
            : (null, double.MaxValue);

        if (clickedRow == SampleRowIndex &&
            nearestSampleChange is not null &&
            sampleChangeDistance <= SampleChangeHitTestRadius)
        {
            if (SeekTimeCommand?.CanExecute(nearestSampleChange.TimeMs) == true)
            {
                SeekTimeCommand.Execute(nearestSampleChange.TimeMs);
            }

            var contextRequest = new HitSoundTimelineContextRequest
            {
                TimeMs = nearestSampleChange.TimeMs,
                PointId = -1,
                SampleChangeTimeMs = nearestSampleChange.TimeMs,
                IsSampleRow = true
            };

            if (OpenContextEditorCommand?.CanExecute(contextRequest) == true)
            {
                OpenContextEditorCommand.Execute(contextRequest);
            }

            e.Handled = true;
            return;
        }

        if (e.ClickCount >= 2 && clickedRow != SampleRowIndex)
        {
            var (visibleStart, visibleEnd) = GetVisiblePointRange();
            var rowIds = new List<int>();
            for (var i = visibleStart; i < visibleEnd; i++)
            {
                var p = _pointsCache[i];
                if (GetRowIndex(p) == clickedRow)
                {
                    rowIds.Add(p.Id);
                }
            }

            var rowIdArray = rowIds.ToArray();
            if (rowIdArray.Length > 0 && SelectPointsCommand?.CanExecute(rowIdArray) == true)
            {
                SelectPointsCommand.Execute(rowIdArray);
            }

            if (SeekTimeCommand?.CanExecute(clickedTimeMs) == true)
            {
                SeekTimeCommand.Execute(clickedTimeMs);
            }

            e.Handled = true;
            return;
        }

        if (nearest is not null && bestDistance <= PointHitTestRadius)
        {
            var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            if (isCtrl)
            {
                if (TogglePointSelectionCommand?.CanExecute(nearest.Id) == true)
                {
                    TogglePointSelectionCommand.Execute(nearest.Id);
                }
            }
            else if (SelectPointCommand?.CanExecute(nearest.Id) == true)
            {
                SelectPointCommand.Execute(nearest.Id);
            }

            if (!isCtrl && SeekTimeCommand?.CanExecute(nearest.TimeMs) == true)
            {
                SeekTimeCommand.Execute(nearest.TimeMs);
            }

            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            clickedRow != SampleRowIndex &&
            TryGetNearestSnapTickTimeMs(clickPosition.X, bounds.Width, windowMs, 10d, out var snappedTimeMs))
        {
            var addRequest = new HitSoundTimelineRowAddRequest
            {
                TimeMs = snappedTimeMs,
                RowIndex = clickedRow
            };

            if (AddPointOnSnapLineCommand?.CanExecute(addRequest) == true)
            {
                AddPointOnSnapLineCommand.Execute(addRequest);
            }

            if (SeekTimeCommand?.CanExecute(snappedTimeMs) == true)
            {
                SeekTimeCommand.Execute(snappedTimeMs);
            }

            e.Handled = true;
            return;
        }

        _isRangeSelecting = true;
        _isSelectionDragActive = false;
        _isAdditiveSelection = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        _selectionStartPosition = clickPosition;
        _selectionCurrentPosition = clickPosition;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var point = e.GetCurrentPoint(this);
        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);

        if (_isMiddlePanning && point.Properties.IsMiddleButtonPressed)
        {
            ClearPlacementGhost();
            var dx = point.Position.X - _lastPanPosition.X;
            _lastPanPosition = point.Position;
            var deltaMs = -(dx / Math.Max(1d, bounds.Width)) * windowMs;

            if (PanTimelineCommand?.CanExecute(deltaMs) == true)
            {
                PanTimelineCommand.Execute(deltaMs);
            }

            e.Handled = true;
            return;
        }

        if (_isRangeSelecting && point.Properties.IsLeftButtonPressed)
        {
            ClearPlacementGhost();
            _selectionCurrentPosition = ClampPointToBounds(point.Position, bounds);
            _isSelectionDragActive = Math.Abs(_selectionCurrentPosition.X - _selectionStartPosition.X) > 4d ||
                                     Math.Abs(_selectionCurrentPosition.Y - _selectionStartPosition.Y) > 4d;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        UpdatePlacementGhost(point.Position, bounds, e.KeyModifiers);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isRangeSelecting)
        {
            var bounds = Bounds;
            var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);
            var releasedPos = ClampPointToBounds(e.GetPosition(this), bounds);
            _selectionCurrentPosition = releasedPos;

            if (_isSelectionDragActive)
            {
                var startTime = XToTime(_selectionStartPosition.X, bounds.Width, windowMs);
                var endTime = XToTime(_selectionCurrentPosition.X, bounds.Width, windowMs);
                var minTime = Math.Min(startTime, endTime);
                var maxTime = Math.Max(startTime, endTime);
                var startRow = YToRowIndex(_selectionStartPosition.Y, DefaultRowHeight);
                var endRow = YToRowIndex(_selectionCurrentPosition.Y, DefaultRowHeight);
                var minRow = Math.Min(startRow, endRow);
                var maxRow = Math.Max(startRow, endRow);

                var selectedIdsList = new List<int>();
                var startIndex = LowerBoundPoints(minTime);
                var endIndexExclusive = UpperBoundPoints(maxTime);
                for (var i = startIndex; i < endIndexExclusive; i++)
                {
                    var p = _pointsCache[i];
                    var row = GetRowIndex(p);
                    if (row >= minRow && row <= maxRow)
                    {
                        selectedIdsList.Add(p.Id);
                    }
                }
                var selectedIds = selectedIdsList.ToArray();

                var selectionCommand = _isAdditiveSelection ? AddPointsToSelectionCommand : SelectPointsCommand;
                if (selectionCommand?.CanExecute(selectedIds) == true)
                {
                    selectionCommand.Execute(selectedIds);
                }
            }
            else if (!_isAdditiveSelection)
            {
                var clickedTimeMs = ResolveClickedTimeMs(_selectionCurrentPosition.X, bounds.Width, windowMs, 12d);
                if (SeekTimeCommand?.CanExecute(clickedTimeMs) == true)
                {
                    SeekTimeCommand.Execute(clickedTimeMs);
                }
            }

            _isRangeSelecting = false;
            _isSelectionDragActive = false;
            _isAdditiveSelection = false;
            InvalidateVisual();
        }

        _isMiddlePanning = false;
        ClearPlacementGhost();
        if (e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isRangeSelecting = false;
        _isSelectionDragActive = false;
        _isAdditiveSelection = false;
        _isMiddlePanning = false;
        ClearPlacementGhost();
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ClearPlacementGhost();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        
        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);
        var cursorTime = (int)Math.Round(ViewStartMs + (e.GetPosition(this).X / bounds.Width) * windowMs);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            var factor = e.Delta.Y > 0 ? 0.85d : 1.15d;
            var request = new HitSoundTimelineZoomRequest
            {
                AnchorTimeMs = cursorTime,
                ZoomFactor = factor
            };

            if (ZoomTimelineCommand?.CanExecute(request) == true)
            {
                ZoomTimelineCommand.Execute(request);
                e.Handled = true;
            }

            return;
        }

        var wheelDelta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;
        var panDeltaMs = -wheelDelta * windowMs * 0.1d;
        if (PanTimelineCommand?.CanExecute(panDeltaMs) == true)
        {
            PanTimelineCommand.Execute(panDeltaMs);
            e.Handled = true;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = DefaultRowHeight * TotalRows;
        var width = double.IsInfinity(availableSize.Width) ? 600d : availableSize.Width;
        return new Size(width, height);
    }

    private void RebuildPointsCache()
    {
        _pointsCache = Points switch
        {
            null => [],
            HitSoundVisualizerPoint[] array => array,
            IReadOnlyCollection<HitSoundVisualizerPoint> collection when collection.Count == 0 => [],
            _ => (Points ?? []).ToArray()
        };
    }

    private void RebuildSampleChangesCache()
    {
        if (SampleChanges is null)
        {
            _sampleChangesCache = [];
            return;
        }

        _sampleChangesCache = (SampleChanges as IEnumerable<HitSoundVisualizerSampleChange> ?? [])
            .OrderBy(x => x.TimeMs)
            .ToArray();
    }

    private void RebuildSnapTicksCache()
    {
        if (SnapTicks is null)
        {
            _snapTicksCache = [];
            return;
        }

        _snapTicksCache = (SnapTicks as IEnumerable<HitSoundVisualizerSnapTick> ?? [])
            .OrderBy(x => x.TimeMs)
            .ToArray();
    }

    private void RebuildSelectedPointIdsCache()
    {
        _selectedPointIdsCache = SelectedPointIds is null
            ? []
            : SelectedPointIds.Where(id => id > 0).ToHashSet();
    }

    private (int StartIndex, int EndIndexExclusive) GetVisiblePointRange()
    {
        if (_pointsCache.Length == 0)
        {
            return (0, 0);
        }

        var minTimeMs = (int)Math.Floor(ViewStartMs);
        var maxTimeMs = (int)Math.Ceiling(ViewEndMs);
        var start = LowerBoundPoints(minTimeMs);
        var end = UpperBoundPoints(maxTimeMs);
        return (start, end);
    }

    private (int StartIndex, int EndIndexExclusive) GetVisibleSampleChangeRange()
    {
        if (_sampleChangesCache.Length == 0)
        {
            return (0, 0);
        }

        var minTimeMs = (int)Math.Floor(ViewStartMs);
        var maxTimeMs = (int)Math.Ceiling(ViewEndMs);

        // Include one preceding change so segment bars that start before the viewport still render into it.
        var start = Math.Max(0, LowerBoundSampleChanges(minTimeMs) - 1);
        var end = UpperBoundSampleChanges(maxTimeMs);
        return (start, Math.Max(start, end));
    }

    private (int StartIndex, int EndIndexExclusive) GetVisibleSnapTickRange()
    {
        if (_snapTicksCache.Length == 0)
        {
            return (0, 0);
        }

        var minTimeMs = (int)Math.Floor(ViewStartMs);
        var maxTimeMs = (int)Math.Ceiling(ViewEndMs);
        var start = LowerBoundSnapTicks(minTimeMs);
        var end = UpperBoundSnapTicks(maxTimeMs);
        return (start, end);
    }

    private int LowerBoundPoints(int targetTimeMs)
    {
        var lo = 0;
        var hi = _pointsCache.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (_pointsCache[mid].TimeMs < targetTimeMs)
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

    private int UpperBoundPoints(int targetTimeMs)
    {
        var lo = 0;
        var hi = _pointsCache.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (_pointsCache[mid].TimeMs <= targetTimeMs)
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

    private int LowerBoundSampleChanges(int targetTimeMs)
    {
        var lo = 0;
        var hi = _sampleChangesCache.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (_sampleChangesCache[mid].TimeMs < targetTimeMs)
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

    private int UpperBoundSampleChanges(int targetTimeMs)
    {
        var lo = 0;
        var hi = _sampleChangesCache.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (_sampleChangesCache[mid].TimeMs <= targetTimeMs)
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

    private int LowerBoundSnapTicks(int targetTimeMs)
    {
        var lo = 0;
        var hi = _snapTicksCache.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (_snapTicksCache[mid].TimeMs < targetTimeMs)
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

    private int UpperBoundSnapTicks(int targetTimeMs)
    {
        var lo = 0;
        var hi = _snapTicksCache.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (_snapTicksCache[mid].TimeMs <= targetTimeMs)
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


    private void DrawRowBackgrounds(DrawingContext context, double width, double rowHeight, int totalRows)
    {
        for (var row = 0; row < totalRows; row++)
        {
            var brush = row == 0
                ? CurrentSampleRowBrush()
                : row % 2 == 0
                    ? CurrentEvenRowBrush()
                    : CurrentOddRowBrush();

            context.FillRectangle(brush, new Rect(0, row * rowHeight, width, rowHeight));
        }
    }

    private void DrawGridLines(DrawingContext context, double width, double rowHeight, int totalRows)
    {
        for (var row = 0; row <= totalRows; row++)
        {
            var y = row * rowHeight;
            context.DrawLine(CurrentGridPen(), new Point(0, y), new Point(width, y));
        }

        context.DrawRectangle(CurrentBorderPen(), new Rect(0, 0, width, totalRows * rowHeight));
    }

    private void DrawTicks(DrawingContext context, double width, double height, double windowMs)
    {
        if (_snapTicksCache.Length == 0)
        {
            return;
        }

        var selectedDivisor = Math.Clamp(SnapDivisorDenominator, 1, 16);
        var (tickStart, tickEnd) = GetVisibleSnapTickRange();
        var visibleTicks = new List<HitSoundVisualizerSnapTick>(Math.Max(0, tickEnd - tickStart));
        for (var i = tickStart; i < tickEnd; i++)
        {
            var tick = _snapTicksCache[i];
            if (tick.Denominator > selectedDivisor || selectedDivisor % Math.Max(1, tick.Denominator) != 0)
            {
                continue;
            }

            visibleTicks.Add(tick);
        }

        if (visibleTicks.Count == 0)
        {
            return;
        }

        var tickMode = ResolveTickRenderMode(visibleTicks, width, windowMs);

        var tickCandidates = new List<(HitSoundVisualizerSnapTick Tick, double X)>(visibleTicks.Count);
        foreach (var tick in visibleTicks)
        {
            if (!ShouldRenderTickForMode(tick, tickMode))
            {
                continue;
            }

            var x = TimeToX(tick.TimeMs, width, windowMs);
            tickCandidates.Add((tick, x));
        }

        if (tickCandidates.Count == 0)
        {
            return;
        }

        var ticksToRender = SimplifyTickCandidatesForDensity(tickCandidates, width, tickMode);
        foreach (var (tick, x) in ticksToRender)
        {
            context.DrawLine(SnapTickPen(tick.Denominator), new Point(x, 0), new Point(x, height));
        }
    }

    private void DrawCursor(DrawingContext context, double width, double height, double windowMs)
    {
        if (CursorTimeMs < ViewStartMs || CursorTimeMs > ViewEndMs)
        {
            return;
        }

        var x = TimeToX(CursorTimeMs, width, windowMs);
        context.DrawLine(CursorPen, new Point(x, 0), new Point(x, height));
    }

    private void DrawSampleChanges(DrawingContext context, double width, double rowHeight, double windowMs)
    {
        if (_sampleChangesCache.Length == 0)
        {
            return;
        }

        var allSampleChanges = _sampleChangesCache;
        if (allSampleChanges.Length == 0)
        {
            return;
        }

        var rowTop = (SampleRowIndex * rowHeight) + SampleChangeBarVerticalPadding;
        var barHeight = Math.Max(8d, rowHeight - (SampleChangeBarVerticalPadding * 2d));

        var visibleOnlyStart = LowerBoundSampleChanges((int)Math.Floor(ViewStartMs));
        var visibleOnlyEnd = UpperBoundSampleChanges((int)Math.Ceiling(ViewEndMs));
        var visibleChangeCount = Math.Max(0, visibleOnlyEnd - visibleOnlyStart);
        if (ShouldUseDenseSampleChangeMarkers(visibleChangeCount, width))
        {
            DrawDenseSampleChangeMarkers(context, width, windowMs, rowTop, barHeight, visibleOnlyStart, visibleOnlyEnd);
            return;
        }

        var (sampleStart, sampleEnd) = GetVisibleSampleChangeRange();
        for (var i = sampleStart; i < sampleEnd && i < allSampleChanges.Length; i++)
        {
            var marker = allSampleChanges[i];
            var nextTimeMs = i + 1 < allSampleChanges.Length
                ? allSampleChanges[i + 1].TimeMs
                : (int)Math.Ceiling(ViewEndMs);
            var segmentEndMs = Math.Max(nextTimeMs, marker.TimeMs);
            var overlapsViewport = segmentEndMs > ViewStartMs && marker.TimeMs <= ViewEndMs;
            if (!overlapsViewport)
            {
                continue;
            }

            var isSelected = marker.TimeMs == SelectedSampleChangeTimeMs;
            var barRect = CreateSampleChangeBarRect(marker.TimeMs, nextTimeMs, width, windowMs, rowTop, barHeight);
            if (barRect.Width <= 0.5d || barRect.Right <= 0 || barRect.X >= width)
            {
                continue;
            }

            DrawSampleChangeBar(context, marker, barRect, isSelected);
        }
    }

    private void DrawDenseSampleChangeMarkers(
        DrawingContext context,
        double width,
        double windowMs,
        double rowTop,
        double barHeight,
        int startIndex,
        int endIndexExclusive)
    {
        if (startIndex >= endIndexExclusive)
        {
            return;
        }

        var lastDrawnX = double.NegativeInfinity;

        for (var i = startIndex; i < endIndexExclusive && i < _sampleChangesCache.Length; i++)
        {
            var marker = _sampleChangesCache[i];
            var x = TimeToX(marker.TimeMs, width, windowMs);
            if (x < 0 || x > width)
            {
                continue;
            }

            var isSelected = marker.TimeMs == SelectedSampleChangeTimeMs;
            if (!isSelected && x - lastDrawnX < SampleChangeDenseLineSpacingPx)
            {
                continue;
            }

            lastDrawnX = x;
            var pen = SampleChangeLinePen(marker.SampleSet);
            context.DrawLine(pen, new Point(x, rowTop), new Point(x, rowTop + barHeight));

            if (isSelected)
            {
                context.DrawLine(SampleChangeSelectionPen, new Point(x, rowTop - 1d), new Point(x, rowTop + barHeight + 1d));
            }
        }
    }

    private void DrawPoints(DrawingContext context, double width, double rowHeight, double windowMs)
    {
        if (_pointsCache.Length == 0)
        {
            return;
        }

        var (pointStart, pointEnd) = GetVisiblePointRange();
        var visibleCount = Math.Max(0, pointEnd - pointStart);

        if (visibleCount == 0)
        {
            return;
        }

        var selectedIds = _selectedPointIdsCache;
        var denseMode = visibleCount > Math.Max(300, width * 2.25d) || (windowMs / Math.Max(1d, width)) > 24d;
        var lastDrawnXByRowAndSample = denseMode
            ? InitializeLastDrawnX()
            : null;
        var renderRadius = denseMode ? 3d : PointRadius;

        for (var i = pointStart; i < pointEnd; i++)
        {
            var point = _pointsCache[i];
            var rowIndex = GetRowIndex(point);
            var x = TimeToX(point.TimeMs, width, windowMs);
            var y = RowCenterY(rowIndex, rowHeight);
            var isSelected = selectedIds.Contains(point.Id);
            if (denseMode && !isSelected && lastDrawnXByRowAndSample is not null)
            {
                var sampleSlot = SampleSetSlot(point.SampleSet);
                if (x - lastDrawnXByRowAndSample[rowIndex, sampleSlot] < 1.1d)
                {
                    continue;
                }

                lastDrawnXByRowAndSample[rowIndex, sampleSlot] = x;
            }

            if (denseMode)
            {
                context.DrawLine(
                    DenseSampleSetPen(point),
                    new Point(x, y - 5),
                    new Point(x, y + 5));
            }
            else
            {
                context.DrawEllipse(
                    SampleSetBrush(point),
                    CurrentPointOutlinePen(),
                    new Point(x, y),
                    renderRadius,
                    renderRadius);
            }

            if (isSelected)
            {
                context.DrawEllipse(
                    null,
                    point.Id == SelectedPointId ? PrimarySelectionPen : CurrentSecondarySelectionPen(),
                    new Point(x, y),
                    renderRadius + 3,
                    renderRadius + 3);
            }
        }
    }

    private void DrawSelectionBox(DrawingContext context, double width, double rowHeight, double windowMs)
    {
        if (!_isRangeSelecting || !_isSelectionDragActive)
        {
            return;
        }

        var minX = Math.Clamp(Math.Min(_selectionStartPosition.X, _selectionCurrentPosition.X), 0, width);
        var maxX = Math.Clamp(Math.Max(_selectionStartPosition.X, _selectionCurrentPosition.X), 0, width);
        var startRow = YToRowIndex(_selectionStartPosition.Y, rowHeight);
        var endRow = YToRowIndex(_selectionCurrentPosition.Y, rowHeight);
        var minRow = Math.Min(startRow, endRow);
        var maxRow = Math.Max(startRow, endRow);
        var y = minRow * rowHeight;
        var rect = new Rect(minX, y, Math.Max(1, maxX - minX), Math.Max(1, ((maxRow - minRow) + 1) * rowHeight));

        context.FillRectangle(new SolidColorBrush(Color.Parse("#66FFD166")), rect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.Parse("#FFD166")), 1.5), rect);
    }

    private void DrawPlacementGhost(DrawingContext context, double width, double rowHeight, double windowMs)
    {
        if (!_showPlacementGhost || _placementGhostRowIndex <= SampleRowIndex)
        {
            return;
        }

        if (_placementGhostTimeMs < ViewStartMs || _placementGhostTimeMs > ViewEndMs)
        {
            return;
        }

        var x = TimeToX(_placementGhostTimeMs, width, windowMs);
        var y = RowCenterY(_placementGhostRowIndex, rowHeight);
        context.DrawEllipse(GhostPreviewFillBrush, GhostPreviewPen, new Point(x, y), PointRadius + 1.5, PointRadius + 1.5);
        context.DrawLine(GhostPreviewPen, new Point(x, y - 8), new Point(x, y + 8));
    }

    private double TimeToX(double timeMs, double width, double windowMs)
    {
        return ((timeMs - ViewStartMs) / windowMs) * width;
    }

    private int XToTime(double x, double width, double windowMs)
    {
        if (width <= 0)
        {
            return (int)Math.Round(ViewStartMs);
        }

        return (int)Math.Round(ViewStartMs + (Math.Clamp(x, 0, width) / width) * windowMs);
    }

    private static double RowCenterY(int rowIndex, double rowHeight)
    {
        return (rowIndex * rowHeight) + (rowHeight / 2d);
    }

    private static int GetRowIndex(HitSoundVisualizerPoint point)
    {
        var soundOffset = point.HitSound switch
        {
            HitSound.Whistle => 1,
            HitSound.Finish => 2,
            HitSound.Clap => 3,
            _ => 0
        };

        return HitSoundRowStartIndex + soundOffset;
    }

    private static int YToRowIndex(double y, double rowHeight)
    {
        return Math.Clamp((int)(y / Math.Max(1d, rowHeight)), 0, TotalRows - 1);
    }

    private int ResolveClickedTimeMs(double clickX, double width, double windowMs, double snapTolerancePx)
    {
        var rawTimeMs = XToTime(clickX, width, windowMs);
        if (IsPlaybackRunning)
        {
            return rawTimeMs;
        }

        return TryGetNearestSnapTickTimeMs(clickX, width, windowMs, snapTolerancePx, out var snappedTimeMs)
            ? snappedTimeMs
            : rawTimeMs;
    }

    private static Point ClampPointToBounds(Point point, Rect bounds)
    {
        return new Point(
            Math.Clamp(point.X, 0, Math.Max(0, bounds.Width)),
            Math.Clamp(point.Y, 0, Math.Max(0, bounds.Height)));
    }

    private void UpdatePlacementGhost(Point pointerPosition, Rect bounds, KeyModifiers keyModifiers)
    {
        if (IsPlaybackRunning || !keyModifiers.HasFlag(KeyModifiers.Control))
        {
            ClearPlacementGhost();
            return;
        }

        var rowIndex = YToRowIndex(pointerPosition.Y, DefaultRowHeight);
        if (rowIndex == SampleRowIndex)
        {
            ClearPlacementGhost();
            return;
        }

        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);
        if (!TryGetNearestSnapTickTimeMs(pointerPosition.X, bounds.Width, windowMs, 10d, out var snappedTimeMs))
        {
            ClearPlacementGhost();
            return;
        }

        var changed = !_showPlacementGhost || _placementGhostRowIndex != rowIndex || _placementGhostTimeMs != snappedTimeMs;
        _showPlacementGhost = true;
        _placementGhostRowIndex = rowIndex;
        _placementGhostTimeMs = snappedTimeMs;

        if (changed)
        {
            InvalidateVisual();
        }
    }

    private void ClearPlacementGhost()
    {
        if (!_showPlacementGhost)
        {
            return;
        }

        _showPlacementGhost = false;
        _placementGhostRowIndex = -1;
        _placementGhostTimeMs = -1;
        InvalidateVisual();
    }

    private IBrush CurrentBackgroundBrush() => UseLightPalette ? LightBackgroundBrush : BackgroundBrush;

    private IBrush CurrentSampleRowBrush() => UseLightPalette ? LightSampleRowBrush : SampleRowBrush;

    private IBrush CurrentEvenRowBrush() => UseLightPalette ? LightEvenRowBrush : EvenRowBrush;

    private IBrush CurrentOddRowBrush() => UseLightPalette ? LightOddRowBrush : OddRowBrush;

    private Pen CurrentGridPen() => UseLightPalette ? LightGridPen : GridPen;

    private Pen CurrentBorderPen() => UseLightPalette ? LightBorderPen : BorderPen;

    private Pen CurrentPointOutlinePen() => UseLightPalette ? LightPointOutlinePen : PointOutlinePen;

    private Pen CurrentSecondarySelectionPen() => UseLightPalette ? LightSecondarySelectionPen : SecondarySelectionPen;

    private Pen CurrentSampleChangeSelectionAccentPen() => UseLightPalette ? LightSampleChangeSelectionAccentPen : SampleChangeSelectionAccentPen;

    private IBrush CurrentSampleChangeLabelBrush() => UseLightPalette ? LightSampleChangeLabelBrush : SampleChangeLabelBrush;

    private Pen CurrentMeasureTickPen() => UseLightPalette ? LightMeasureTickPen : MeasureTickPen;

    private Pen CurrentGenericTickPen() => UseLightPalette ? LightGenericTickPen : GenericTickPen;

    private static IBrush SampleSetBrush(HitSoundVisualizerPoint point)
    {
        if (point.IsAutoSampleSet)
        {
            return AutoPointBrush;
        }

        return SampleSetBrush(point.SampleSet);
    }

    private static IBrush SampleSetBrush(SampleSet sampleSet)
    {
        return sampleSet switch
        {
            SampleSet.Soft => SoftPointBrush,
            SampleSet.Drum => DrumPointBrush,
            _ => NormalPointBrush
        };
    }

    private static Pen DenseSampleSetPen(HitSoundVisualizerPoint point)
    {
        if (point.IsAutoSampleSet)
        {
            return DenseAutoPen;
        }

        return DenseSampleSetPen(point.SampleSet);
    }

    private static Pen DenseSampleSetPen(SampleSet sampleSet)
    {
        return sampleSet switch
        {
            SampleSet.Soft => DenseSoftPen,
            SampleSet.Drum => DenseDrumPen,
            _ => DenseNormalPen
        };
    }

    private static int SampleSetSlot(SampleSet sampleSet)
    {
        return sampleSet switch
        {
            SampleSet.Soft => 1,
            SampleSet.Drum => 2,
            _ => 0
        };
    }

    private static double[,] InitializeLastDrawnX()
    {
        var values = new double[TotalRows, 3];
        for (var row = 0; row < TotalRows; row++)
        {
            for (var sample = 0; sample < 3; sample++)
            {
                values[row, sample] = double.NegativeInfinity;
            }
        }

        return values;
    }

    private (HitSoundVisualizerPoint? Point, double Distance) FindNearestPoint(Point clickPosition, double width, double windowMs)
    {
        HitSoundVisualizerPoint? nearest = null;
        var bestDistance = double.MaxValue;

        var (startIndex, endIndex) = GetVisiblePointRange();
        for (var i = startIndex; i < endIndex; i++)
        {
            var item = _pointsCache[i];
            var x = TimeToX(item.TimeMs, width, windowMs);
            var y = RowCenterY(GetRowIndex(item), DefaultRowHeight);
            var distance = Math.Sqrt(Math.Pow(clickPosition.X - x, 2) + Math.Pow(clickPosition.Y - y, 2));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = item;
            }
        }

        return (nearest, bestDistance);
    }

    private (HitSoundVisualizerSampleChange? Marker, double Distance) FindNearestSampleChange(Point clickPosition, double width, double windowMs)
    {
        HitSoundVisualizerSampleChange? nearest = null;
        var bestDistance = double.MaxValue;
        var allSampleChanges = _sampleChangesCache;
        if (allSampleChanges.Length == 0)
        {
            return (null, bestDistance);
        }

        var rowTop = (SampleRowIndex * DefaultRowHeight) + SampleChangeBarVerticalPadding;
        var barHeight = Math.Max(8d, DefaultRowHeight - (SampleChangeBarVerticalPadding * 2d));

        var (startIndex, endIndex) = GetVisibleSampleChangeRange();
        for (var i = startIndex; i < endIndex && i < allSampleChanges.Length; i++)
        {
            var marker = allSampleChanges[i];
            var nextTimeMs = i + 1 < allSampleChanges.Length
                ? allSampleChanges[i + 1].TimeMs
                : (int)Math.Ceiling(ViewEndMs);
            var segmentEndMs = Math.Max(nextTimeMs, marker.TimeMs);
            if (!(segmentEndMs > ViewStartMs && marker.TimeMs <= ViewEndMs))
            {
                continue;
            }

            var distance = DistanceToSampleChangeBar(
                clickPosition,
                marker.TimeMs,
                nextTimeMs,
                width,
                windowMs,
                rowTop,
                barHeight);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = marker;
            }
        }

        return (nearest, bestDistance);
    }

    private void DrawSampleChangeBar(DrawingContext context, HitSoundVisualizerSampleChange marker, Rect barRect, bool isSelected)
    {
        var baseColor = SampleSetColor(marker.SampleSet);
        var fillBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.FromArgb(190, baseColor.R, baseColor.G, baseColor.B), 0d),
                new GradientStop(Color.FromArgb(72, baseColor.R, baseColor.G, baseColor.B), 0.72d),
                new GradientStop(Color.FromArgb(12, baseColor.R, baseColor.G, baseColor.B), 1d)
            }
        };

        var outlinePen = new Pen(new SolidColorBrush(Color.FromArgb(170, baseColor.R, baseColor.G, baseColor.B)), 1);
        var roundedRect = new RoundedRect(barRect, SampleChangeBarCornerRadius);
        context.DrawRectangle(fillBrush, outlinePen, roundedRect);

        context.DrawLine(
            new Pen(new SolidColorBrush(Color.FromArgb(220, baseColor.R, baseColor.G, baseColor.B)), 1.6),
            new Point(barRect.X + 0.5d, barRect.Y + 1d),
            new Point(barRect.X + 0.5d, barRect.Bottom - 1d));

        if (isSelected)
        {
            var selectionRect = new Rect(barRect.X - 1d, barRect.Y - 1d, barRect.Width + 2d, barRect.Height + 2d);
            context.DrawRectangle(null, SampleChangeSelectionPen, new RoundedRect(selectionRect, SampleChangeBarCornerRadius + 1d));
            context.DrawRectangle(null, CurrentSampleChangeSelectionAccentPen(), roundedRect);
        }

        var label = FormatSampleChangeLabel(marker);
        var text = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            SampleChangeLabelTypeface,
            SampleChangeTextFontSize,
            CurrentSampleChangeLabelBrush());

        var maxTextWidth = barRect.Width - (SampleChangeTextPadding * 2d);
        if (maxTextWidth < 28d || text.Width > maxTextWidth)
        {
            return;
        }

        var textX = barRect.X + SampleChangeTextPadding;
        var textY = barRect.Y + Math.Max(0d, (barRect.Height - text.Height) / 2d) - 0.5d;
        using (context.PushClip(new Rect(barRect.X + 1d, barRect.Y + 1d, Math.Max(0d, barRect.Width - 2d), Math.Max(0d, barRect.Height - 2d))))
        {
            context.DrawText(text, new Point(textX, textY));
        }
    }

    private Rect CreateSampleChangeBarRect(
        int timeMs,
        int nextTimeMs,
        double width,
        double windowMs,
        double rowTop,
        double barHeight)
    {
        var startX = TimeToX(timeMs, width, windowMs);
        var nextX = TimeToX(Math.Max(timeMs, nextTimeMs), width, windowMs) - SampleChangeBarHorizontalPadding;
        var rightX = Math.Max(startX + SampleChangeBarMinWidth, nextX);
        var clippedLeft = Math.Clamp(startX, 0, width);
        var clippedRight = Math.Clamp(rightX, 0, width);
        if (clippedRight < clippedLeft)
        {
            (clippedLeft, clippedRight) = (clippedRight, clippedLeft);
        }

        return new Rect(clippedLeft, rowTop, Math.Max(0d, clippedRight - clippedLeft), barHeight);
    }

    private double DistanceToSampleChangeBar(
        Point clickPosition,
        int timeMs,
        int nextTimeMs,
        double width,
        double windowMs,
        double rowTop,
        double barHeight)
    {
        var rect = CreateSampleChangeBarRect(timeMs, nextTimeMs, width, windowMs, rowTop, barHeight);
        if (rect.Width <= 0.5d)
        {
            return double.MaxValue;
        }

        var dx = clickPosition.X < rect.X
            ? rect.X - clickPosition.X
            : clickPosition.X > rect.Right
                ? clickPosition.X - rect.Right
                : 0d;
        var dy = clickPosition.Y < rect.Y
            ? rect.Y - clickPosition.Y
            : clickPosition.Y > rect.Bottom
                ? clickPosition.Y - rect.Bottom
                : 0d;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static Color SampleSetColor(SampleSet sampleSet)
    {
        return sampleSet switch
        {
            SampleSet.Soft => Color.Parse("#5BC0EB"),
            SampleSet.Drum => Color.Parse("#E55934"),
            _ => Color.Parse("#9BC53D")
        };
    }

    private static bool ShouldUseDenseSampleChangeMarkers(int visibleChangeCount, double width)
    {
        if (visibleChangeCount <= 0 || width <= 1)
        {
            return false;
        }

        var avgSpacingPx = width / visibleChangeCount;
        return avgSpacingPx <= SampleChangeDenseModeMaxAvgSpacingPx;
    }

    private static Pen SampleChangeLinePen(SampleSet sampleSet)
    {
        return sampleSet switch
        {
            SampleSet.Soft => SoftSampleChangeLinePen,
            SampleSet.Drum => DrumSampleChangeLinePen,
            _ => NormalSampleChangeLinePen
        };
    }

    private static string FormatSampleChangeLabel(HitSoundVisualizerSampleChange marker)
    {
        var bank = marker.SampleSet switch
        {
            SampleSet.Soft => "soft",
            SampleSet.Drum => "drum",
            _ => "normal"
        };

        return $"{bank}{Math.Max(1, marker.Index)} - {NormalizeDisplayVolumePercent(marker.Volume)}%";
    }

    private static int NormalizeDisplayVolumePercent(int volume)
    {
        if (volume is > 0 and <= 1)
        {
            return volume * 100;
        }

        return Math.Clamp(volume, 0, 100);
    }

    private void HandleRightClick(Point clickPosition, Rect bounds, KeyModifiers keyModifiers)
    {
        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);
        var clickedRow = YToRowIndex(clickPosition.Y, DefaultRowHeight);
        var isSampleRow = clickedRow == SampleRowIndex;
        var isCtrlDelete = keyModifiers.HasFlag(KeyModifiers.Control);
        var clickedTimeMs = ResolveClickedTimeMs(clickPosition.X, bounds.Width, windowMs, 12d);
        var (nearestPoint, pointDistance) = isSampleRow
            ? (null, double.MaxValue)
            : FindNearestPoint(clickPosition, bounds.Width, windowMs);
        var (nearestSampleChange, sampleChangeDistance) = isSampleRow
            ? FindNearestSampleChange(clickPosition, bounds.Width, windowMs)
            : (null, double.MaxValue);

        var hasPointTarget = nearestPoint is not null && pointDistance <= PointHitTestRadius;
        var hasSampleChangeTarget = nearestSampleChange is not null && sampleChangeDistance <= SampleChangeHitTestRadius;

        if (isCtrlDelete)
        {
            if (hasPointTarget && nearestPoint is not null)
            {
                if (SelectPointCommand?.CanExecute(nearestPoint.Id) == true)
                {
                    SelectPointCommand.Execute(nearestPoint.Id);
                }

                if (DeleteSelectedPointCommand?.CanExecute(null) == true)
                {
                    DeleteSelectedPointCommand.Execute(null);
                }

                return;
            }

            if (hasSampleChangeTarget && nearestSampleChange is not null)
            {
                var deleteContextRequest = new HitSoundTimelineContextRequest
                {
                    TimeMs = nearestSampleChange.TimeMs,
                    PointId = -1,
                    SampleChangeTimeMs = nearestSampleChange.TimeMs,
                    IsSampleRow = true
                };

                if (OpenContextEditorCommand?.CanExecute(deleteContextRequest) == true)
                {
                    OpenContextEditorCommand.Execute(deleteContextRequest);
                }

                if (DeleteSampleChangeCommand?.CanExecute(null) == true)
                {
                    DeleteSampleChangeCommand.Execute(null);
                }

                return;
            }

            return;
        }

        if (hasPointTarget && nearestPoint is not null)
        {
            var selectedIds = (SelectedPointIds ?? []).ToHashSet();
            if (!selectedIds.Contains(nearestPoint.Id))
            {
                if (SelectPointCommand?.CanExecute(nearestPoint.Id) == true)
                {
                    SelectPointCommand.Execute(nearestPoint.Id);
                }
            }

            clickedTimeMs = nearestPoint.TimeMs;
        }
        else if (hasSampleChangeTarget && nearestSampleChange is not null)
        {
            clickedTimeMs = nearestSampleChange.TimeMs;
        }

        if (SeekTimeCommand?.CanExecute(clickedTimeMs) == true)
        {
            SeekTimeCommand.Execute(clickedTimeMs);
        }

        var contextRequest = new HitSoundTimelineContextRequest
        {
            TimeMs = clickedTimeMs,
            PointId = hasPointTarget && nearestPoint is not null ? nearestPoint.Id : -1,
            SampleChangeTimeMs = hasSampleChangeTarget && nearestSampleChange is not null
                ? nearestSampleChange.TimeMs
                : null,
            IsSampleRow = isSampleRow
        };

        if (OpenContextEditorCommand?.CanExecute(contextRequest) == true)
        {
            OpenContextEditorCommand.Execute(contextRequest);
        }
    }

    private bool TryGetNearestSnapTickTimeMs(
        double clickX,
        double width,
        double windowMs,
        double tolerancePx,
        out int snappedTimeMs)
    {
        snappedTimeMs = 0;
        if (_snapTicksCache.Length == 0 || width <= 1 || windowMs <= 0)
        {
            return false;
        }

        var selectedDivisor = Math.Clamp(SnapDivisorDenominator, 1, 16);
        var bestDistancePx = double.MaxValue;
        HitSoundVisualizerSnapTick? bestTick = null;

        var (startIndex, endIndex) = GetVisibleSnapTickRange();
        for (var i = startIndex; i < endIndex; i++)
        {
            var tick = _snapTicksCache[i];

            if (tick.Denominator > selectedDivisor || selectedDivisor % Math.Max(1, tick.Denominator) != 0)
            {
                continue;
            }

            var x = TimeToX(tick.TimeMs, width, windowMs);
            var distancePx = Math.Abs(x - clickX);
            if (distancePx < bestDistancePx)
            {
                bestDistancePx = distancePx;
                bestTick = tick;
            }
        }

        if (bestTick is null || bestDistancePx > tolerancePx)
        {
            return false;
        }

        snappedTimeMs = bestTick.TimeMs;
        return true;
    }

    private static Color SnapTickColor(int denominator)
    {
        return denominator switch
        {
            1 => Color.Parse("#FFFFFF"), // 1/1
            2 => Color.Parse("#FF5A5A"), // 1/2
            3 => Color.Parse("#C99BFF"), // 1/3
            4 => Color.Parse("#4EA3FF"), // 1/4
            6 => Color.Parse("#8E5CFF"), // 1/6
            8 => Color.Parse("#F0D94B"), // 1/8
            16 => Color.Parse("#7A848F"), // 1/16

            // Triplet-family fallback for 1/12, 1/15, etc.
            _ when denominator % 6 == 0 => Color.Parse("#8E5CFF"),
            _ when denominator % 3 == 0 => Color.Parse("#C99BFF"),
            _ => Color.Parse("#7A848F")
        };
    }

    private Pen SnapTickPen(int denominator)
    {
        return denominator switch
        {
            1 => CurrentMeasureTickPen(),
            2 => HalfTickPen,
            3 => TripletTickPen,
            4 => QuarterTickPen,
            6 => SixthTickPen,
            8 => EighthTickPen,
            _ when denominator % 6 == 0 => SixthTickPen,
            _ when denominator % 3 == 0 => TripletTickPen,
            _ => CurrentGenericTickPen()
        };
    }

    private static TickRenderMode ResolveTickRenderMode(
        IReadOnlyList<HitSoundVisualizerSnapTick> visibleTicks,
        double width,
        double windowMs)
    {
        if (visibleTicks.Count < 2 || width <= 1 || windowMs <= 0)
        {
            return TickRenderMode.Full;
        }

        var spacings = new List<double>(Math.Min(visibleTicks.Count - 1, 512));
        for (var i = 1; i < visibleTicks.Count && spacings.Count < 512; i++)
        {
            var dt = visibleTicks[i].TimeMs - visibleTicks[i - 1].TimeMs;
            if (dt <= 0)
            {
                continue;
            }

            var spacingPx = (dt / windowMs) * width;
            if (spacingPx > 0)
            {
                spacings.Add(spacingPx);
            }
        }

        if (spacings.Count == 0)
        {
            return TickRenderMode.Full;
        }

        spacings.Sort();
        var medianSpacing = spacings[spacings.Count / 2];

        if (medianSpacing < 3.5)
        {
            return TickRenderMode.MeasuresOnly;
        }

        if (medianSpacing < 6)
        {
            return TickRenderMode.StrongOnly;
        }

        if (medianSpacing < 9)
        {
            return TickRenderMode.MidOnly;
        }

        return TickRenderMode.Full;
    }

    private static bool ShouldRenderTickForMode(HitSoundVisualizerSnapTick tick, TickRenderMode mode)
    {
        return mode switch
        {
            TickRenderMode.MeasuresOnly => tick.IsMeasureLine,
            TickRenderMode.StrongOnly => tick.Denominator is 1 or 2 or 3 or 4,
            TickRenderMode.MidOnly => tick.Denominator is 1 or 2 or 3 or 4 or 6 or 8,
            _ => true
        };
    }

    private static IReadOnlyList<(HitSoundVisualizerSnapTick Tick, double X)> SimplifyTickCandidatesForDensity(
        IReadOnlyList<(HitSoundVisualizerSnapTick Tick, double X)> candidates,
        double width,
        TickRenderMode tickMode)
    {
        if (candidates.Count < 2 || width <= 1)
        {
            return candidates;
        }

        var baseTargetSpacingPx = tickMode switch
        {
            TickRenderMode.MeasuresOnly => SnapTickDenseMeasuresTargetMinSpacingPx,
            TickRenderMode.StrongOnly => SnapTickDenseStrongTargetMinSpacingPx,
            _ => SnapTickTargetMinSpacingPx
        };
        var maxSpacingPx = tickMode == TickRenderMode.MeasuresOnly
            ? SnapTickDenseMeasuresMaxSpacingPx
            : SnapTickMaxSimplifiedSpacingPx;

        var targetMaxTicks = Math.Max(32d, width / baseTargetSpacingPx);
        if (candidates.Count <= targetMaxTicks)
        {
            return candidates;
        }

        var overloadRatio = candidates.Count / targetMaxTicks;
        var minSpacingPx = Math.Clamp(
            baseTargetSpacingPx * Math.Sqrt(overloadRatio),
            baseTargetSpacingPx,
            maxSpacingPx);

        var simplified = new List<(HitSoundVisualizerSnapTick Tick, double X)>(Math.Min(candidates.Count, (int)Math.Ceiling(width)));
        foreach (var candidate in candidates)
        {
            if (simplified.Count == 0)
            {
                simplified.Add(candidate);
                continue;
            }

            var last = simplified[^1];
            if (candidate.X - last.X >= minSpacingPx)
            {
                simplified.Add(candidate);
                continue;
            }

            if (GetTickRenderPriority(candidate.Tick) > GetTickRenderPriority(last.Tick))
            {
                simplified[^1] = candidate;
            }
        }

        return simplified;
    }

    private static int GetTickRenderPriority(HitSoundVisualizerSnapTick tick)
    {
        var priority = tick.IsMeasureLine ? 10_000 : 0;
        priority += tick.Denominator switch
        {
            1 => 2_000,
            2 or 3 or 4 => 1_000,
            6 or 8 => 500,
            _ => 0
        };

        // Prefer lower denominators when density pruning needs to choose.
        priority += Math.Max(0, 256 - Math.Min(255, tick.Denominator));
        return priority;
    }

    private enum TickRenderMode
    {
        Full,
        MidOnly,
        StrongOnly,
        MeasuresOnly
    }
}
