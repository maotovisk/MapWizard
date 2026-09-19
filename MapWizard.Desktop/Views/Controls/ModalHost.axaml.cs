using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.Views.Controls;

public partial class ModalHost : UserControl
{
    private static readonly RelativePoint CenterOrigin = new(0.5d, 0.5d, RelativeUnit.Relative);
    private static readonly RelativePoint RightCenterOrigin = new(1d, 0.5d, RelativeUnit.Relative);
    private const double OverlayLeftBreathingRoom = 110d;
    private const double OverlayRightInset = 14d;
    private const double OverlayMinimumWidth = 460d;
    private const double OverlayMaximumWidth = 620d;
    private const double BackgroundBlurRadius = 10d;
    private int _transitionVersion;
    private Visual? _blurredTarget;
    private IEffect? _previousBackgroundEffect;
    private Visual? _backgroundCacheTarget;
    private CacheMode? _previousBackgroundCacheMode;
    private CancellationTokenSource? _backgroundBlurAnimationCancellation;
    private double _backgroundBlurRadius;

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ModalHost, bool>(nameof(IsOpen));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModalHost, string?>(nameof(Title));

    public static readonly StyledProperty<object?> DialogContentProperty =
        AvaloniaProperty.Register<ModalHost, object?>(nameof(DialogContent));

    public static readonly StyledProperty<object?> FooterContentProperty =
        AvaloniaProperty.Register<ModalHost, object?>(nameof(FooterContent));

    public static readonly StyledProperty<bool> ShowCloseButtonProperty =
        AvaloniaProperty.Register<ModalHost, bool>(nameof(ShowCloseButton), true);

    public static readonly StyledProperty<bool> CloseOnBackdropClickProperty =
        AvaloniaProperty.Register<ModalHost, bool>(nameof(CloseOnBackdropClick), true);

    public static readonly StyledProperty<bool> CloseOnEscapeProperty =
        AvaloniaProperty.Register<ModalHost, bool>(nameof(CloseOnEscape), true);

    public static readonly StyledProperty<ModalPresentation> PresentationProperty =
        AvaloniaProperty.Register<ModalHost, ModalPresentation>(nameof(Presentation));

    public static readonly StyledProperty<Visual?> BackgroundBlurTargetProperty =
        AvaloniaProperty.Register<ModalHost, Visual?>(nameof(BackgroundBlurTarget));

    public ModalHost()
    {
        InitializeComponent();
        DialogCard.RenderTransformOrigin = CenterOrigin;
        BackdropBorder.PointerPressed += BackdropBorder_OnPointerPressed;
        SizeChanged += OnHostSizeChanged;
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        private set => SetValue(IsOpenProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? DialogContent
    {
        get => GetValue(DialogContentProperty);
        set => SetValue(DialogContentProperty, value);
    }

    public object? FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    public bool ShowCloseButton
    {
        get => GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    public bool CloseOnBackdropClick
    {
        get => GetValue(CloseOnBackdropClickProperty);
        set => SetValue(CloseOnBackdropClickProperty, value);
    }

    public bool CloseOnEscape
    {
        get => GetValue(CloseOnEscapeProperty);
        set => SetValue(CloseOnEscapeProperty, value);
    }

    public ModalPresentation Presentation
    {
        get => GetValue(PresentationProperty);
        set => SetValue(PresentationProperty, value);
    }

    public Visual? BackgroundBlurTarget
    {
        get => GetValue(BackgroundBlurTargetProperty);
        set => SetValue(BackgroundBlurTargetProperty, value);
    }

    public event EventHandler? CloseRequested;

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        var version = ++_transitionVersion;
        UpdatePresentationGeometry();
        BackdropBorder.Opacity = 0d;
        DialogCard.Opacity = 0d;
        DialogCard.RenderTransform = GetClosedTransform();
        IsVisible = true;
        IsOpen = true;
        PrepareBackgroundBlur();
        var blurAnimation = AnimateBackgroundBlurAsync(
            BackgroundBlurRadius,
            TimeSpan.FromMilliseconds(260),
            version,
            cancellationToken);

        try
        {
            // Give the hidden start state a frame before changing animated
            // properties. Otherwise Avalonia paints the final state first.
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            if (version != _transitionVersion)
            {
                return;
            }
            BackdropBorder.Opacity = Presentation == ModalPresentation.MapPickerOverlay ? 0.2d : 0.6d;
            await Task.Delay(TimeSpan.FromMilliseconds(80), cancellationToken);
            if (version != _transitionVersion)
            {
                return;
            }
            DialogCard.Opacity = 1d;
            DialogCard.RenderTransform = GetOpenTransform();
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            await blurAnimation;
        }
        catch (OperationCanceledException)
        {
            // CloseAsync owns the end state when opening is interrupted.
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        var version = ++_transitionVersion;
        if (!IsOpen && !IsVisible)
        {
            return;
        }

        var blurAnimation = AnimateBackgroundBlurAsync(
            0d,
            TimeSpan.FromMilliseconds(220),
            version,
            cancellationToken);
        DialogCard.Opacity = 0d;
        DialogCard.RenderTransform = GetClosedTransform();

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);
            BackdropBorder.Opacity = 0d;
            await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);
            await blurAnimation;
        }
        catch (OperationCanceledException)
        {
            // Fall through to the hidden end state below.
        }
        finally
        {
            IsOpen = false;
            IsVisible = false;
            ClearBackgroundBlur();
            BackdropBorder.Opacity = 0d;
            DialogCard.Opacity = 0d;
            DialogCard.RenderTransform = GetClosedTransform();
        }
    }

    public void Clear()
    {
        Title = null;
        DialogContent = null;
        FooterContent = null;
        Presentation = ModalPresentation.Dialog;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FooterContentProperty)
        {
            FooterBorder.IsVisible = FooterContent is not null;
        }
        else if (change.Property == PresentationProperty)
        {
            UpdatePresentationGeometry();
        }
    }

    private void OnHostSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (Presentation == ModalPresentation.MapPickerOverlay &&
            Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 0.5d)
        {
            UpdateOverlayWidth();
        }
    }

    private void UpdatePresentationGeometry()
    {
        var isMapPickerOverlay = Presentation == ModalPresentation.MapPickerOverlay;
        DialogCard.Classes.Set("MapPickerOverlay", isMapPickerOverlay);
        DialogCard.RenderTransformOrigin = isMapPickerOverlay ? RightCenterOrigin : CenterOrigin;
        HeaderGrid.IsVisible = !isMapPickerOverlay;
        // The dark plane now covers the entire window; the blur target (the
        // window shell) handles the seam, and the top islands float above it.
        BackdropBorder.Margin = default;
        DialogContentPresenter.Margin = isMapPickerOverlay
            ? default
            : new Thickness(0d, 12d, 0d, 0d);
        FooterBorder.Margin = isMapPickerOverlay
            ? new Thickness(0d, 10d, 4d, 0d)
            : new Thickness(0d, 12d, 0d, 0d);

        if (isMapPickerOverlay)
        {
            UpdateOverlayWidth();
        }
        else
        {
            DialogCard.Width = double.NaN;
        }
    }

    private void UpdateOverlayWidth()
    {
        var availableWidth = Math.Max(
            0d,
            Bounds.Width - OverlayLeftBreathingRoom - OverlayRightInset);
        var preferredWidth = Math.Clamp(
            Bounds.Width * 0.46d,
            OverlayMinimumWidth,
            OverlayMaximumWidth);
        DialogCard.Width = Math.Min(preferredWidth, availableWidth);
    }

    private TransformOperations GetClosedTransform()
    {
        return Presentation == ModalPresentation.MapPickerOverlay
            ? TransformOperations.Parse("translateX(64px)")
            : TransformOperations.Parse("scale(0.96)");
    }

    private TransformOperations GetOpenTransform()
    {
        return Presentation == ModalPresentation.MapPickerOverlay
            ? TransformOperations.Parse("translateX(0px)")
            : TransformOperations.Parse("scale(1)");
    }

    private void PrepareBackgroundBlur()
    {
        var target = BackgroundBlurTarget;
        if (target is null)
        {
            return;
        }

        if (ReferenceEquals(target, _blurredTarget))
        {
            return;
        }

        ClearBackgroundBlur();
        _blurredTarget = target;
        _previousBackgroundEffect = target.Effect;
        _backgroundBlurRadius = 0d;

        // Cache the shell's content below the effect-bearing border. Caching
        // the border itself would also cache the animated effect, making the
        // transition appear to jump directly to its final frame.
        if (target is Decorator { Child: { } child })
        {
            _backgroundCacheTarget = child;
            _previousBackgroundCacheMode = child.CacheMode;
            child.CacheMode ??= new BitmapCache();
        }

        target.Effect = new BlurEffect { Radius = 0d };
    }

    private async Task AnimateBackgroundBlurAsync(
        double targetRadius,
        TimeSpan duration,
        int version,
        CancellationToken cancellationToken)
    {
        var target = _blurredTarget;
        if (target is null)
        {
            return;
        }

        // Reading before cancellation captures the currently animated effect,
        // allowing a close that interrupts an open to reverse without a jump.
        var startRadius = target.Effect is IBlurEffect blur
            ? blur.Radius
            : _backgroundBlurRadius;

        _backgroundBlurAnimationCancellation?.Cancel();
        var animationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundBlurAnimationCancellation = animationCancellation;

        // Keep the destination as the base value. When RunAsync removes its
        // animation value, the rendered effect therefore remains at the end.
        target.Effect = new BlurEffect { Radius = targetRadius };

        try
        {
            await CreateBackgroundBlurAnimation(startRadius, targetRadius, duration)
                .RunAsync(target, animationCancellation.Token);

            if (version == _transitionVersion && ReferenceEquals(target, _blurredTarget))
            {
                _backgroundBlurRadius = targetRadius;
                target.Effect = new BlurEffect { Radius = targetRadius };
            }
        }
        catch (OperationCanceledException)
        {
            // A replacement transition continues from the animated radius.
        }
        finally
        {
            if (ReferenceEquals(_backgroundBlurAnimationCancellation, animationCancellation))
            {
                _backgroundBlurAnimationCancellation = null;
            }

            animationCancellation.Dispose();
        }
    }

    private static Animation CreateBackgroundBlurAnimation(
        double startRadius,
        double endRadius,
        TimeSpan duration)
    {
        return new Animation
        {
            Duration = duration,
            Easing = new CubicEaseInOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(
                            Visual.EffectProperty,
                            new BlurEffect { Radius = startRadius }),
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(
                            Visual.EffectProperty,
                            new BlurEffect { Radius = endRadius }),
                    },
                },
            },
        };
    }

    private void ClearBackgroundBlur()
    {
        _backgroundBlurAnimationCancellation?.Cancel();
        _backgroundBlurAnimationCancellation = null;

        if (_blurredTarget is not null)
        {
            _blurredTarget.Effect = _previousBackgroundEffect;
        }

        if (_backgroundCacheTarget is not null)
        {
            _backgroundCacheTarget.CacheMode = _previousBackgroundCacheMode;
        }

        _blurredTarget = null;
        _previousBackgroundEffect = null;
        _backgroundCacheTarget = null;
        _previousBackgroundCacheMode = null;
        _backgroundBlurRadius = 0d;
    }

    private void BackdropBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsOpen || !CloseOnBackdropClick)
        {
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!IsOpen)
        {
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (!IsOpen || !CloseOnEscape || e.Key != Key.Escape)
        {
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }
}
