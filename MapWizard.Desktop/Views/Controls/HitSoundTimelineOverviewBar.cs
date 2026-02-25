using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using BeatmapParser.Enums;
using MapWizard.Desktop.Models.HitSoundVisualizer;

namespace MapWizard.Desktop.Views.Controls;

public class HitSoundTimelineOverviewBar : Control
{
    private enum OverviewInteractionMode
    {
        None,
        SeekCursor,
        PanView
    }

    private static readonly Pen NormalSampleChangeTintPen = new(new SolidColorBrush(Color.Parse("#4D9BC53D")), 1);
    private static readonly Pen SoftSampleChangeTintPen = new(new SolidColorBrush(Color.Parse("#4D5BC0EB")), 1);
    private static readonly Pen DrumSampleChangeTintPen = new(new SolidColorBrush(Color.Parse("#4DE55934")), 1);
    private const double SampleChangeOverviewMinSpacingPx = 1.5d;
    private const double OverviewCornerRadius = 7d;
    private bool _isScrubbing;
    private OverviewInteractionMode _interactionMode;
    private HitSoundVisualizerSampleChange[] _sampleChangesCache = [];

    public static readonly StyledProperty<double> TimelineEndMsProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, double>(nameof(TimelineEndMs), 1000d);

    public static readonly StyledProperty<double> ViewStartMsProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, double>(nameof(ViewStartMs));

    public static readonly StyledProperty<double> ViewEndMsProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, double>(nameof(ViewEndMs), 1000d);

    public static readonly StyledProperty<int> CursorTimeMsProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, int>(nameof(CursorTimeMs));

    public static readonly StyledProperty<IEnumerable<HitSoundVisualizerSampleChange>?> SampleChangesProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, IEnumerable<HitSoundVisualizerSampleChange>?>(nameof(SampleChanges));

    public static readonly StyledProperty<ICommand?> PanTimelineCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, ICommand?>(nameof(PanTimelineCommand));

    public static readonly StyledProperty<ICommand?> SeekTimeCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, ICommand?>(nameof(SeekTimeCommand));

    static HitSoundTimelineOverviewBar()
    {
        AffectsRender<HitSoundTimelineOverviewBar>(
            TimelineEndMsProperty,
            ViewStartMsProperty,
            ViewEndMsProperty,
            CursorTimeMsProperty,
            SampleChangesProperty);
    }

    public double TimelineEndMs
    {
        get => GetValue(TimelineEndMsProperty);
        set => SetValue(TimelineEndMsProperty, value);
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

    public IEnumerable<HitSoundVisualizerSampleChange>? SampleChanges
    {
        get => GetValue(SampleChangesProperty);
        set => SetValue(SampleChangesProperty, value);
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

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        var total = Math.Max(1d, TimelineEndMs);
        var trackRect = new Rect(0, 0, bounds.Width, bounds.Height);
        var trackRoundedRect = new RoundedRect(trackRect, OverviewCornerRadius);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#10151D")), null, trackRoundedRect);

        // subtle measure-ish stripes to improve spatial reading
        var stripeBrush = new SolidColorBrush(Color.Parse("#1A2230"));
        using (context.PushClip(trackRect.Deflate(1)))
        {
            for (var i = 0; i < 40; i++)
            {
                var x = (i / 40d) * bounds.Width;
                context.FillRectangle(stripeBrush, new Rect(x, 0, 1, bounds.Height));
            }

            DrawSampleChangeTint(context, bounds, total);
        }

        var viewStartRatio = Math.Clamp(ViewStartMs / total, 0, 1);
        var viewEndRatio = Math.Clamp(ViewEndMs / total, 0, 1);
        var viewRect = new Rect(
            viewStartRatio * bounds.Width,
            2,
            Math.Max(2, (viewEndRatio - viewStartRatio) * bounds.Width),
            Math.Max(2, bounds.Height - 4));

        var viewRoundedRect = new RoundedRect(viewRect, Math.Min(OverviewCornerRadius - 1d, Math.Max(2d, viewRect.Height / 2d)));
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#2A5D8F")), null, viewRoundedRect);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#8CC8FF")), 1.2), viewRoundedRect);

        var cursorRatio = Math.Clamp(CursorTimeMs / total, 0, 1);
        var cursorX = cursorRatio * bounds.Width;
        context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#FFD166")), 1.8), new Point(cursorX, 0), new Point(cursorX, bounds.Height));

        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#334255")), 1), trackRoundedRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SampleChangesProperty)
        {
            RebuildSampleChangeCache();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 320 : availableSize.Width;
        return new Size(width, 18);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isScrubbing = true;
        _interactionMode = ResolveInteractionMode(e.GetPosition(this));
        e.Pointer.Capture(this);
        HandlePointerAction(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isScrubbing || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        HandlePointerAction(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isScrubbing = false;
        _interactionMode = OverviewInteractionMode.None;
        if (e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
        }
    }

    private void HandlePointerAction(Point point)
    {
        switch (_interactionMode)
        {
            case OverviewInteractionMode.SeekCursor:
                SeekFromPoint(point);
                break;
            case OverviewInteractionMode.PanView:
                PanViewToPoint(point);
                break;
        }
    }

    private void SeekFromPoint(Point point)
    {
        var bounds = Bounds;
        if (bounds.Width <= 0)
        {
            return;
        }

        var ratio = Math.Clamp(point.X / bounds.Width, 0, 1);
        var timeMs = (int)Math.Round(ratio * Math.Max(1d, TimelineEndMs));
        if (SeekTimeCommand?.CanExecute(timeMs) == true)
        {
            SeekTimeCommand.Execute(timeMs);
        }
    }

    private void PanViewToPoint(Point point)
    {
        var bounds = Bounds;
        if (bounds.Width <= 0)
        {
            return;
        }

        var ratio = Math.Clamp(point.X / bounds.Width, 0, 1);
        var totalMs = Math.Max(1d, TimelineEndMs);
        var targetTimeMs = ratio * totalMs;
        var windowMs = Math.Max(1d, ViewEndMs - ViewStartMs);
        var maxStartMs = Math.Max(0d, totalMs - windowMs);
        var targetStartMs = Math.Clamp(targetTimeMs - (windowMs / 2d), 0d, maxStartMs);
        var deltaMs = targetStartMs - ViewStartMs;
        if (Math.Abs(deltaMs) < 0.5d)
        {
            return;
        }

        if (PanTimelineCommand?.CanExecute(deltaMs) == true)
        {
            PanTimelineCommand.Execute(deltaMs);
        }
    }

    private OverviewInteractionMode ResolveInteractionMode(Point point)
    {
        var bounds = Bounds;
        if (bounds.Width <= 0)
        {
            return OverviewInteractionMode.SeekCursor;
        }

        var ratio = Math.Clamp(point.X / bounds.Width, 0, 1);
        var timeMs = ratio * Math.Max(1d, TimelineEndMs);
        return timeMs >= ViewStartMs && timeMs <= ViewEndMs
            ? OverviewInteractionMode.SeekCursor
            : OverviewInteractionMode.PanView;
    }

    private void DrawSampleChangeTint(DrawingContext context, Rect bounds, double totalMs)
    {
        if (_sampleChangesCache.Length == 0 || totalMs <= 1d || bounds.Width <= 1d)
        {
            return;
        }

        var lastDrawnX = double.NegativeInfinity;
        var y1 = 1d;
        var y2 = Math.Max(1d, bounds.Height - 1d);
        for (var i = 0; i < _sampleChangesCache.Length; i++)
        {
            var marker = _sampleChangesCache[i];
            var ratio = Math.Clamp(marker.TimeMs / totalMs, 0d, 1d);
            var x = ratio * bounds.Width;
            if (x - lastDrawnX < SampleChangeOverviewMinSpacingPx)
            {
                continue;
            }

            lastDrawnX = x;
            context.DrawLine(SampleChangeTintPen(marker.SampleSet), new Point(x, y1), new Point(x, y2));
        }
    }

    private void RebuildSampleChangeCache()
    {
        if (SampleChanges is null)
        {
            _sampleChangesCache = [];
            return;
        }

        _sampleChangesCache = SampleChanges
            .Where(static x => x is not null)
            .OrderBy(static x => x.TimeMs)
            .ToArray();
    }

    private static Pen SampleChangeTintPen(SampleSet sampleSet)
    {
        return sampleSet switch
        {
            SampleSet.Soft => SoftSampleChangeTintPen,
            SampleSet.Drum => DrumSampleChangeTintPen,
            _ => NormalSampleChangeTintPen
        };
    }
}
