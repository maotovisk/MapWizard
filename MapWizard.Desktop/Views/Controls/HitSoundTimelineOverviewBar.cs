using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace MapWizard.Desktop.Views.Controls;

public class HitSoundTimelineOverviewBar : Control
{
    private bool _isScrubbing;

    public static readonly StyledProperty<double> TimelineEndMsProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, double>(nameof(TimelineEndMs), 1000d);

    public static readonly StyledProperty<double> ViewStartMsProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, double>(nameof(ViewStartMs));

    public static readonly StyledProperty<double> ViewEndMsProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, double>(nameof(ViewEndMs), 1000d);

    public static readonly StyledProperty<int> CursorTimeMsProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, int>(nameof(CursorTimeMs));

    public static readonly StyledProperty<ICommand?> SeekTimeCommandProperty =
        AvaloniaProperty.Register<HitSoundTimelineOverviewBar, ICommand?>(nameof(SeekTimeCommand));

    static HitSoundTimelineOverviewBar()
    {
        AffectsRender<HitSoundTimelineOverviewBar>(
            TimelineEndMsProperty,
            ViewStartMsProperty,
            ViewEndMsProperty,
            CursorTimeMsProperty);
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

    public ICommand? SeekTimeCommand
    {
        get => GetValue(SeekTimeCommandProperty);
        set => SetValue(SeekTimeCommandProperty, value);
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
        context.FillRectangle(new SolidColorBrush(Color.Parse("#10151D")), trackRect);

        // subtle measure-ish stripes to improve spatial reading
        var stripeBrush = new SolidColorBrush(Color.Parse("#1A2230"));
        for (var i = 0; i < 40; i++)
        {
            var x = (i / 40d) * bounds.Width;
            context.FillRectangle(stripeBrush, new Rect(x, 0, 1, bounds.Height));
        }

        var viewStartRatio = Math.Clamp(ViewStartMs / total, 0, 1);
        var viewEndRatio = Math.Clamp(ViewEndMs / total, 0, 1);
        var viewRect = new Rect(
            viewStartRatio * bounds.Width,
            2,
            Math.Max(2, (viewEndRatio - viewStartRatio) * bounds.Width),
            Math.Max(2, bounds.Height - 4));

        context.FillRectangle(new SolidColorBrush(Color.Parse("#2A5D8F")), viewRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.Parse("#8CC8FF")), 1.2), viewRect);

        var cursorRatio = Math.Clamp(CursorTimeMs / total, 0, 1);
        var cursorX = cursorRatio * bounds.Width;
        context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#FFD166")), 1.8), new Point(cursorX, 0), new Point(cursorX, bounds.Height));

        context.DrawRectangle(new Pen(new SolidColorBrush(Color.Parse("#334255")), 1), trackRect);
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
        e.Pointer.Capture(this);
        SeekFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isScrubbing || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        SeekFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isScrubbing = false;
        if (e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
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
}
