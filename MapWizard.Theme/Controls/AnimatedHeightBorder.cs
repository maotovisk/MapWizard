using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace MapWizard.Theme.Controls;

/// <summary>
/// Reveals its child by animating to the child's measured height. Unlike a fixed
/// MaxHeight animation, wrapped content always receives enough room and short
/// content uses the full duration instead of appearing to snap open.
/// </summary>
public sealed class AnimatedHeightBorder : Border
{
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<AnimatedHeightBorder, bool>(nameof(IsExpanded));

    private bool _heightUpdatePending;
    private readonly DispatcherTimer _settledMeasureTimer;
    private Size _lastMeasuredContentSize;
    private DateTime _animationEndsAt;
    private bool _isAttached;
    private bool _isTrackingLayout;

    public AnimatedHeightBorder()
    {
        _settledMeasureTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(340)
        };
        _settledMeasureTimer.Tick += OnSettledMeasureTimerTick;
        SizeChanged += OnSizeChanged;
    }

    protected override Type StyleKeyOverride => typeof(AnimatedHeightBorder);

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        UpdateLayoutTracking();
        ScheduleHeightUpdate();
        RestartSettledMeasureTimer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        UpdateLayoutTracking();
        _settledMeasureTimer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsExpandedProperty)
        {
            PseudoClasses.Set(":expanded", IsExpanded);
            UpdateLayoutTracking();
            ScheduleHeightUpdate();
            RestartSettledMeasureTimer();
        }
        else if (change.Property == ChildProperty || change.Property == PaddingProperty)
        {
            ScheduleHeightUpdate();
            RestartSettledMeasureTimer();
        }
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Height changes continuously during the transition. Only a width change
        // can alter wrapping and therefore the measured expansion target.
        if (!IsExpanded || Math.Abs(e.NewSize.Width - e.PreviousSize.Width) <= 0.5d)
        {
            return;
        }

        ScheduleHeightUpdate();
        RestartSettledMeasureTimer();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (!IsExpanded || DateTime.UtcNow < _animationEndsAt || Child is null)
        {
            return;
        }

        var content = GetMeasureTarget();
        if (Math.Abs(content.DesiredSize.Width - _lastMeasuredContentSize.Width) > 0.5d ||
            Math.Abs(content.DesiredSize.Height - _lastMeasuredContentSize.Height) > 0.5d)
        {
            ScheduleHeightUpdate();
        }
    }

    private void UpdateLayoutTracking()
    {
        var shouldTrack = _isAttached && IsExpanded;
        if (shouldTrack == _isTrackingLayout)
        {
            return;
        }

        if (shouldTrack)
        {
            LayoutUpdated += OnLayoutUpdated;
        }
        else
        {
            LayoutUpdated -= OnLayoutUpdated;
        }

        _isTrackingLayout = shouldTrack;
    }

    private void OnSettledMeasureTimerTick(object? sender, EventArgs e)
    {
        _settledMeasureTimer.Stop();
        ScheduleHeightUpdate();
    }

    private void RestartSettledMeasureTimer()
    {
        _settledMeasureTimer.Stop();
        if (IsExpanded && _isAttached)
        {
            _settledMeasureTimer.Start();
        }
    }

    private void ScheduleHeightUpdate()
    {
        if (_heightUpdatePending)
        {
            return;
        }

        _heightUpdatePending = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                _heightUpdatePending = false;
                UpdateExpansionHeight();
            },
            DispatcherPriority.Background);
    }

    private void UpdateExpansionHeight()
    {
        if (!IsExpanded || Child is null)
        {
            if (MaxHeight != 0d)
            {
                MaxHeight = 0d;
            }

            return;
        }

        var contentWidth = Math.Max(0d, Bounds.Width - Padding.Left - Padding.Right);
        if (contentWidth <= 0d)
        {
            return;
        }

        var content = GetMeasureTarget();
        content.Measure(new Size(contentWidth, double.PositiveInfinity));
        _lastMeasuredContentSize = content.DesiredSize;
        var targetHeight = content.DesiredSize.Height + Padding.Top + Padding.Bottom;

        if (Math.Abs(MaxHeight - targetHeight) > 0.5d)
        {
            MaxHeight = targetHeight;
            _animationEndsAt = DateTime.UtcNow.AddMilliseconds(320);
        }
    }

    private Control GetMeasureTarget()
    {
        return Child is ContentPresenter { Child: Control content }
            ? content
            : Child!;
    }
}
