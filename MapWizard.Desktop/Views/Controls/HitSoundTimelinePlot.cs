using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using MapWizard.Desktop.Models.HitSoundVisualizer;
using BeatmapParser.Enums;

namespace MapWizard.Desktop.Views.Controls;

public class HitSoundTimelinePlot : Control
{
    private const double DefaultRowHeight = 28d;
    private const double PointRadius = 4d;
    private const int SampleRowIndex = 0;
    private bool _isMiddlePanning;
    private bool _isRangeSelecting;
    private bool _isSelectionDragActive;
    private bool _isAdditiveSelection;
    private Point _selectionStartPosition;
    private Point _selectionCurrentPosition;
    private Point _lastPanPosition;

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
        AvaloniaProperty.Register<HitSoundTimelinePlot, int>(nameof(SnapDivisorDenominator), 8);

    public static readonly StyledProperty<int> SelectedPointIdProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, int>(nameof(SelectedPointId), -1);

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

    public static readonly StyledProperty<ICommand?> SeekTimeCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(SeekTimeCommand));

    public static readonly StyledProperty<ICommand?> PanTimelineCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(PanTimelineCommand));

    public static readonly StyledProperty<ICommand?> ZoomTimelineCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelinePlot, ICommand?>(nameof(ZoomTimelineCommand));

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

    public int SelectedPointId
    {
        get => GetValue(SelectedPointIdProperty);
        set => SetValue(SelectedPointIdProperty, value);
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

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        var totalRows = 13;
        var rowHeight = DefaultRowHeight;
        var timelineWidth = bounds.Width;
        var timelineHeight = Math.Max(bounds.Height, totalRows * rowHeight);
        var timelineRect = new Rect(0, 0, timelineWidth, timelineHeight);
        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);

        context.FillRectangle(new SolidColorBrush(Color.Parse("#14181D")), timelineRect);

        DrawRowBackgrounds(context, timelineWidth, rowHeight, totalRows);
        DrawTicks(context, timelineWidth, timelineHeight, windowMs);
        DrawCursor(context, timelineWidth, timelineHeight, windowMs);
        DrawSampleChanges(context, timelineWidth, rowHeight, windowMs);
        DrawPoints(context, timelineWidth, rowHeight, windowMs);
        DrawSelectionBox(context, timelineWidth, rowHeight, windowMs);
        DrawGridLines(context, timelineWidth, rowHeight, totalRows);
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

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);
        var clickPosition = point.Position;
        var clickedTimeMs = (int)Math.Round(ViewStartMs + (clickPosition.X / bounds.Width) * windowMs);

        var visiblePoints = (Points ?? [])
            .Where(p => p.TimeMs >= ViewStartMs && p.TimeMs <= ViewEndMs)
            .ToList();

        HitSoundVisualizerPoint? nearest = null;
        var bestDistance = double.MaxValue;
        foreach (var item in visiblePoints)
        {
            var x = TimeToX(item.TimeMs, bounds.Width, windowMs);
            var y = RowCenterY(GetRowIndex(item), DefaultRowHeight);
            var distance = Math.Sqrt(Math.Pow(clickPosition.X - x, 2) + Math.Pow(clickPosition.Y - y, 2));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = item;
            }
        }

        if (nearest is not null && bestDistance <= 10d)
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
            _selectionCurrentPosition = ClampPointToBounds(point.Position, bounds);
            _isSelectionDragActive = Math.Abs(_selectionCurrentPosition.X - _selectionStartPosition.X) > 4d ||
                                     Math.Abs(_selectionCurrentPosition.Y - _selectionStartPosition.Y) > 4d;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

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

                var selectedIds = (Points ?? [])
                    .Where(p => p.TimeMs >= minTime && p.TimeMs <= maxTime)
                    .Where(p =>
                    {
                        var row = GetRowIndex(p);
                        return row >= minRow && row <= maxRow;
                    })
                    .OrderBy(p => p.TimeMs)
                    .Select(p => p.Id)
                    .ToArray();

                var selectionCommand = _isAdditiveSelection ? AddPointsToSelectionCommand : SelectPointsCommand;
                if (selectionCommand?.CanExecute(selectedIds) == true)
                {
                    selectionCommand.Execute(selectedIds);
                }
            }
            else if (!_isAdditiveSelection)
            {
                var clickedTimeMs = XToTime(_selectionCurrentPosition.X, bounds.Width, windowMs);
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
        InvalidateVisual();
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
        var height = DefaultRowHeight * 13d;
        var width = double.IsInfinity(availableSize.Width) ? 600d : availableSize.Width;
        return new Size(width, height);
    }


    private void DrawRowBackgrounds(DrawingContext context, double width, double rowHeight, int totalRows)
    {
        for (var row = 0; row < totalRows; row++)
        {
            var color = row == 0
                ? Color.Parse("#1F2430")
                : row % 2 == 0
                    ? Color.Parse("#151B22")
                    : Color.Parse("#131920");

            context.FillRectangle(new SolidColorBrush(color), new Rect(0, row * rowHeight, width, rowHeight));
        }
    }

    private void DrawGridLines(DrawingContext context, double width, double rowHeight, int totalRows)
    {
        var pen = new Pen(new SolidColorBrush(Color.Parse("#2B3440")), 1);
        for (var row = 0; row <= totalRows; row++)
        {
            var y = row * rowHeight;
            context.DrawLine(pen, new Point(0, y), new Point(width, y));
        }

        context.DrawRectangle(new Pen(new SolidColorBrush(Color.Parse("#324050")), 1), new Rect(0, 0, width, totalRows * rowHeight));
    }

    private void DrawTicks(DrawingContext context, double width, double height, double windowMs)
    {
        if (SnapTicks is null)
        {
            return;
        }

        var selectedDivisor = Math.Clamp(SnapDivisorDenominator, 1, 16);
        var visibleTicks = SnapTicks
            .Where(tick => tick.TimeMs >= ViewStartMs && tick.TimeMs <= ViewEndMs)
            .Where(tick => tick.Denominator <= selectedDivisor && selectedDivisor % Math.Max(1, tick.Denominator) == 0)
            .OrderBy(tick => tick.TimeMs)
            .ToList();

        if (visibleTicks.Count == 0)
        {
            return;
        }

        var tickMode = ResolveTickRenderMode(visibleTicks, width, windowMs);

        foreach (var tick in visibleTicks)
        {
            if (!ShouldRenderTickForMode(tick, tickMode))
            {
                continue;
            }

            var x = TimeToX(tick.TimeMs, width, windowMs);
            var color = SnapTickColor(tick.Denominator);
            var thickness = tick.Denominator switch
            {
                1 => 1.6,
                2 or 3 or 4 => 1.2,
                _ => 1.0
            };
            context.DrawLine(new Pen(new SolidColorBrush(color), thickness), new Point(x, 0), new Point(x, height));
        }
    }

    private void DrawCursor(DrawingContext context, double width, double height, double windowMs)
    {
        if (CursorTimeMs < ViewStartMs || CursorTimeMs > ViewEndMs)
        {
            return;
        }

        var x = TimeToX(CursorTimeMs, width, windowMs);
        context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#F4C95D")), 1.5), new Point(x, 0), new Point(x, height));
    }

    private void DrawSampleChanges(DrawingContext context, double width, double rowHeight, double windowMs)
    {
        if (SampleChanges is null)
        {
            return;
        }

        foreach (var marker in SampleChanges)
        {
            if (marker.TimeMs < ViewStartMs || marker.TimeMs > ViewEndMs)
            {
                continue;
            }

            var x = TimeToX(marker.TimeMs, width, windowMs);
            var y = RowCenterY(SampleRowIndex, rowHeight);
            var brush = new SolidColorBrush(SampleSetColor(marker.SampleSet));
            var outline = new Pen(new SolidColorBrush(Color.Parse("#091018")), 1);

            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(new Point(x, y - 6), true);
                geometryContext.LineTo(new Point(x + 6, y));
                geometryContext.LineTo(new Point(x, y + 6));
                geometryContext.LineTo(new Point(x - 6, y));
                geometryContext.EndFigure(true);
            }

            context.DrawGeometry(brush, outline, geometry);
        }
    }

    private void DrawPoints(DrawingContext context, double width, double rowHeight, double windowMs)
    {
        if (Points is null)
        {
            return;
        }

        var selectedIds = (SelectedPointIds ?? []).ToHashSet();

        foreach (var point in Points)
        {
            if (point.TimeMs < ViewStartMs || point.TimeMs > ViewEndMs)
            {
                continue;
            }

            var rowIndex = GetRowIndex(point);
            var x = TimeToX(point.TimeMs, width, windowMs);
            var y = RowCenterY(rowIndex, rowHeight);
            var fillColor = SampleSetColor(point.SampleSet);
            var fillBrush = new SolidColorBrush(fillColor);

            if (point.IsDraggable)
            {
                context.DrawRectangle(
                    fillBrush,
                    new Pen(new SolidColorBrush(Color.Parse("#0C1117")), 1),
                    new Rect(x - PointRadius, y - PointRadius, PointRadius * 2, PointRadius * 2));
            }
            else
            {
                context.DrawEllipse(
                    fillBrush,
                    new Pen(new SolidColorBrush(Color.Parse("#0C1117")), 1),
                    new Point(x, y),
                    PointRadius,
                    PointRadius);
            }

            if (selectedIds.Contains(point.Id))
            {
                context.DrawEllipse(
                    null,
                    new Pen(new SolidColorBrush(point.Id == SelectedPointId ? Color.Parse("#FFD166") : Color.Parse("#FFFFFF")), point.Id == SelectedPointId ? 2 : 1.2),
                    new Point(x, y),
                    PointRadius + 3,
                    PointRadius + 3);
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
        var sampleOffset = point.SampleSet switch
        {
            SampleSet.Soft => 4,
            SampleSet.Drum => 8,
            _ => 0
        };

        var soundOffset = point.HitSound switch
        {
            HitSound.Whistle => 1,
            HitSound.Finish => 2,
            HitSound.Clap => 3,
            _ => 0
        };

        return 1 + sampleOffset + soundOffset;
    }

    private static int YToRowIndex(double y, double rowHeight)
    {
        return Math.Clamp((int)(y / Math.Max(1d, rowHeight)), 0, 12);
    }

    private static Point ClampPointToBounds(Point point, Rect bounds)
    {
        return new Point(
            Math.Clamp(point.X, 0, Math.Max(0, bounds.Width)),
            Math.Clamp(point.Y, 0, Math.Max(0, bounds.Height)));
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

        if (medianSpacing < 2.5)
        {
            return TickRenderMode.MeasuresOnly;
        }

        if (medianSpacing < 4.5)
        {
            return TickRenderMode.StrongOnly;
        }

        if (medianSpacing < 7)
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

    private enum TickRenderMode
    {
        Full,
        MidOnly,
        StrongOnly,
        MeasuresOnly
    }
}
