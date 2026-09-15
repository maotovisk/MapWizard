using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SukiUI.Theme;

namespace MapWizard.Desktop.Controls;

/// <summary>
/// A <see cref="ScrollViewer"/> with compositor-driven wheel-detent scrolling
/// for both content and scrollbar thumbs, plus an edge fade. Drop-in replacement for
/// <see cref="ScrollViewer"/> anywhere in the app.
/// </summary>
public class SmoothScrollViewer : ScrollViewer
{
    private const double ScrollEpsilon = 1d;

    private static bool _isGlobalSmoothScrollingEnabled = true;
    private static event EventHandler? GlobalSmoothScrollingChanged;

    public static readonly StyledProperty<bool> IsSmoothScrollingEnabledProperty =
        AvaloniaProperty.Register<SmoothScrollViewer, bool>(nameof(IsSmoothScrollingEnabled), true);

    public static readonly StyledProperty<bool> IsScrollFadeEnabledProperty =
        AvaloniaProperty.Register<SmoothScrollViewer, bool>(nameof(IsScrollFadeEnabled), true);

    public static readonly StyledProperty<double> FadeSizeProperty =
        AvaloniaProperty.Register<SmoothScrollViewer, double>(nameof(FadeSize), 28d);

    private IDisposable? _presenterBoundsSubscription;
    private ScrollContentPresenter? _presenter;
    private ScrollBar? _horizontalScrollBar;
    private ScrollBar? _verticalScrollBar;
    private CompositionVisual? _smoothContentVisual;
    private CompositionVisual? _horizontalThumbVisual;
    private CompositionVisual? _verticalThumbVisual;
    private bool _ownsContentAnimation;
    private bool _ownsHorizontalThumbAnimation;
    private bool _ownsVerticalThumbAnimation;
    private readonly DispatcherTimer _wheelAnimationTimer;
    private bool _isWheelAnimationActive;
    private PointerWheelEventArgs? _pendingWheelEvent;
    private bool _isWheelDispatching;

    public SmoothScrollViewer()
    {
        ScrollChanged += OnScrollChanged;
        _wheelAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(380) };
        _wheelAnimationTimer.Tick += (_, _) => DetachSmoothScrolling();
        AddHandler(PointerWheelChangedEvent, OnPointerWheelStart, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerWheelChangedEvent, OnPointerWheelAfter, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    public static bool IsGlobalSmoothScrollingEnabled => _isGlobalSmoothScrollingEnabled;

    public static void SetGlobalSmoothScrollingEnabled(bool enabled)
    {
        if (_isGlobalSmoothScrollingEnabled == enabled)
        {
            return;
        }

        _isGlobalSmoothScrollingEnabled = enabled;
        GlobalSmoothScrollingChanged?.Invoke(null, EventArgs.Empty);
    }

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
        DetachSmoothScrolling();
        _horizontalThumbVisual = null;
        _verticalThumbVisual = null;
        _presenter = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter");
        _horizontalScrollBar = e.NameScope.Find<ScrollBar>("PART_HorizontalScrollBar");
        _verticalScrollBar = e.NameScope.Find<ScrollBar>("PART_VerticalScrollBar");
        if (_presenter is not null)
        {
            _presenterBoundsSubscription = _presenter
                .GetObservable(BoundsProperty)
                .Subscribe(_ => UpdateFade());
        }

        ResolveContentVisual();
        ResolveThumbVisuals();
        UpdateFade();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        GlobalSmoothScrollingChanged -= OnGlobalSmoothScrollingChanged;
        DetachSmoothScrolling();
        _smoothContentVisual = null;
        _horizontalThumbVisual = null;
        _verticalThumbVisual = null;
        _presenterBoundsSubscription?.Dispose();
        _presenterBoundsSubscription = null;
        _presenter = null;
        _horizontalScrollBar = null;
        _verticalScrollBar = null;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        GlobalSmoothScrollingChanged += OnGlobalSmoothScrollingChanged;
        ResolveContentVisual();
        ResolveThumbVisuals();
    }

    private void OnGlobalSmoothScrollingChanged(object? sender, EventArgs e)
    {
        if (!IsGlobalSmoothScrollingEnabled)
        {
            DetachSmoothScrolling();
        }
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
        else if (change.Property == OffsetProperty &&
                 _isWheelAnimationActive && !_isWheelDispatching)
        {
            // Dragging the bar or programmatic navigation takes precedence.
            DetachSmoothScrolling();
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateFade();
    }

    private void OnPointerWheelStart(object? sender, PointerWheelEventArgs e)
    {
        if (!IsSmoothScrollingEnabled || !IsGlobalSmoothScrollingEnabled)
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
        if (_horizontalThumbVisual is null || _verticalThumbVisual is null)
        {
            ResolveThumbVisuals();
        }

        _ownsContentAnimation |= EnsureWheelAnimation(_smoothContentVisual);
        _ownsHorizontalThumbAnimation |= EnsureWheelAnimation(_horizontalThumbVisual);
        _ownsVerticalThumbAnimation |= EnsureWheelAnimation(_verticalThumbVisual);
        _isWheelAnimationActive = true;
        _wheelAnimationTimer.Stop();
        _wheelAnimationTimer.Start();
        _pendingWheelEvent = e;
        _isWheelDispatching = true;
    }

    private void OnPointerWheelAfter(object? sender, PointerWheelEventArgs e)
    {
        if (!ReferenceEquals(_pendingWheelEvent, e))
        {
            return;
        }

        _pendingWheelEvent = null;
        _isWheelDispatching = false;
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

    private void ResolveThumbVisuals()
    {
        _horizontalThumbVisual ??= ResolveThumbVisual(_horizontalScrollBar);
        _verticalThumbVisual ??= ResolveThumbVisual(_verticalScrollBar);
    }

    private static CompositionVisual? ResolveThumbVisual(ScrollBar? scrollBar)
    {
        scrollBar?.ApplyTemplate();
        var thumb = scrollBar?.GetVisualDescendants()
            .OfType<Track>()
            .FirstOrDefault()?.Thumb;
        return thumb is null ? null : ElementComposition.GetElementVisual(thumb);
    }

    private static bool EnsureWheelAnimation(CompositionVisual? visual)
    {
        if (visual is not null && visual.ImplicitAnimations is null)
        {
            Scrollable.MakeScrollable(visual);
            return true;
        }

        return false;
    }

    private void DetachSmoothScrolling()
    {
        _wheelAnimationTimer.Stop();
        _isWheelAnimationActive = false;
        _isWheelDispatching = false;
        _pendingWheelEvent = null;
        if (_ownsContentAnimation && _smoothContentVisual is not null)
        {
            _smoothContentVisual.ImplicitAnimations = null;
        }
        if (_ownsHorizontalThumbAnimation && _horizontalThumbVisual is not null)
        {
            _horizontalThumbVisual.ImplicitAnimations = null;
        }
        if (_ownsVerticalThumbAnimation && _verticalThumbVisual is not null)
        {
            _verticalThumbVisual.ImplicitAnimations = null;
        }
        _ownsContentAnimation = false;
        _ownsHorizontalThumbAnimation = false;
        _ownsVerticalThumbAnimation = false;
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
