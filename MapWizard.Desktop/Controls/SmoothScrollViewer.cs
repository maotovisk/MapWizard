using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using SukiUI.Theme;

namespace MapWizard.Desktop.Controls;

/// <summary>
/// A <see cref="ScrollViewer"/> with GPU-composited smooth scrolling (using the
/// same implicit offset-animation technique as SukiUI's settings pages) and an
/// edge fade that hints at hidden content. Drop-in replacement for
/// <see cref="ScrollViewer"/> anywhere in the app.
/// </summary>
public class SmoothScrollViewer : ScrollViewer
{
    private const double ScrollEpsilon = 1d;

    public static readonly StyledProperty<bool> IsSmoothScrollingEnabledProperty =
        AvaloniaProperty.Register<SmoothScrollViewer, bool>(nameof(IsSmoothScrollingEnabled), true);

    public static readonly StyledProperty<bool> IsScrollFadeEnabledProperty =
        AvaloniaProperty.Register<SmoothScrollViewer, bool>(nameof(IsScrollFadeEnabled), true);

    public static readonly StyledProperty<double> FadeSizeProperty =
        AvaloniaProperty.Register<SmoothScrollViewer, double>(nameof(FadeSize), 28d);

    private IDisposable? _presenterBoundsSubscription;
    private ScrollContentPresenter? _presenter;
    private CompositionVisual? _smoothContentVisual;
    private readonly DispatcherTimer _wheelAnimationTimer;
    private bool _wheelScrollPending;
    private bool _isWheelAnimationActive;

    public SmoothScrollViewer()
    {
        ScrollChanged += OnScrollChanged;
        _wheelAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(380) };
        _wheelAnimationTimer.Tick += (_, _) => DetachSmoothScrolling();
        AddHandler(PointerWheelChangedEvent, OnPointerWheel, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    public bool IsSmoothScrollingEnabled
    {
        get => GetValue(IsSmoothScrollingEnabledProperty);
        set => SetValue(IsSmoothScrollingEnabledProperty, value);
    }

    public bool IsScrollFadeEnabled
    {
        get => GetValue(IsScrollFadeEnabledProperty);
        set => SetValue(IsScrollFadeEnabledProperty, value);
    }

    public double FadeSize
    {
        get => GetValue(FadeSizeProperty);
        set => SetValue(FadeSizeProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _presenterBoundsSubscription?.Dispose();
        _presenterBoundsSubscription = null;
        _presenter = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter");
        if (_presenter is not null)
        {
            _presenterBoundsSubscription = _presenter
                .GetObservable(BoundsProperty)
                .Subscribe(_ => UpdateFade());
        }

        ResolveContentVisual();
        UpdateFade();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ResolveContentVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DetachSmoothScrolling();
        _smoothContentVisual = null;
        _presenterBoundsSubscription?.Dispose();
        _presenterBoundsSubscription = null;
        _presenter = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentControl.ContentProperty)
        {
            ResolveContentVisual();
        }
        else if (change.Property == IsSmoothScrollingEnabledProperty)
        {
            if (!IsSmoothScrollingEnabled)
            {
                DetachSmoothScrolling();
            }
        }
        else if (change.Property == IsScrollFadeEnabledProperty ||
                 change.Property == FadeSizeProperty)
        {
            UpdateFade();
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (!_wheelScrollPending)
        {
            DetachSmoothScrolling();
        }

        UpdateFade();
    }

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!IsSmoothScrollingEnabled)
        {
            return;
        }

        // Avalonia reports physical wheel detents as whole deltas. Fractional
        // deltas come from precision devices and must track the finger directly.
        if (!IsWholeDetent(e.Delta.X) || !IsWholeDetent(e.Delta.Y))
        {
            DetachSmoothScrolling();
            return;
        }

        if (_smoothContentVisual is null)
        {
            ResolveContentVisual();
        }

        if (_smoothContentVisual is not null)
        {
            if (!_isWheelAnimationActive)
            {
                Scrollable.MakeScrollable(_smoothContentVisual);
                _isWheelAnimationActive = true;
            }
            _wheelScrollPending = true;
            _wheelAnimationTimer.Stop();
            _wheelAnimationTimer.Start();
            Dispatcher.UIThread.Post(() => _wheelScrollPending = false, DispatcherPriority.Background);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => DetachSmoothScrolling();

    private static bool IsWholeDetent(double value) => Math.Abs(value - Math.Round(value)) < 0.001d;

    private void ResolveContentVisual()
    {
        DetachSmoothScrolling();
        _smoothContentVisual = null;
        if (((_presenter?.Child ?? Content) as Visual) is { } contentVisual)
        {
            _smoothContentVisual = ElementComposition.GetElementVisual(contentVisual);
        }
    }

    private void DetachSmoothScrolling()
    {
        _wheelAnimationTimer.Stop();
        _isWheelAnimationActive = false;
        if (_smoothContentVisual is not null)
        {
            _smoothContentVisual.ImplicitAnimations = null;
        }
    }

    private void UpdateFade()
    {
        if (_presenter is null)
        {
            return;
        }

        if (!IsScrollFadeEnabled)
        {
            _presenter.OpacityMask = null;
            return;
        }

        var canScrollVertically = Extent.Height > Viewport.Height + ScrollEpsilon;
        if (!canScrollVertically)
        {
            _presenter.OpacityMask = null;
            return;
        }

        var height = _presenter.Bounds.Height;
        if (height <= 0d)
        {
            return;
        }

        var fade = Math.Min(FadeSize, height / 2d);
        var ratio = fade / height;
        var hasHiddenContentAbove = Offset.Y > ScrollEpsilon;
        var hasHiddenContentBelow = Offset.Y + Viewport.Height < Extent.Height - ScrollEpsilon;

        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0d, 0d, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0d, 1d, RelativeUnit.Relative)
        };

        if (hasHiddenContentAbove)
        {
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 0d));
            brush.GradientStops.Add(new GradientStop(Colors.Black, ratio));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop(Colors.Black, 0d));
        }

        if (hasHiddenContentBelow)
        {
            brush.GradientStops.Add(new GradientStop(Colors.Black, 1d - ratio));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 1d));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop(Colors.Black, 1d));
        }

        _presenter.OpacityMask = brush;
    }
}
