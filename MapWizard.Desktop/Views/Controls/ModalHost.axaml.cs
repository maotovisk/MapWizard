using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
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
    private const double OverlayBackgroundBlurRadius = 10d;
    private int _transitionVersion;
    private Visual? _blurredTarget;
    private IEffect? _previousBackgroundEffect;
    private BlurEffect? _backgroundBlurEffect;

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
        var animateBackgroundBlur = Presentation == ModalPresentation.MapPickerOverlay;
        if (animateBackgroundBlur)
        {
            PrepareBackgroundBlur();
        }

        var blurAnimation = animateBackgroundBlur
            ? AnimateBackgroundBlurAsync(
                OverlayBackgroundBlurRadius,
                TimeSpan.FromMilliseconds(260),
                version,
                cancellationToken)
            : Task.CompletedTask;

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
        BackdropBorder.Margin = isMapPickerOverlay
            ? new Thickness(0d, 42d, 0d, 0d)
            : default;
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

        if (ReferenceEquals(target, _blurredTarget) && _backgroundBlurEffect is not null)
        {
            return;
        }

        ClearBackgroundBlur();
        _blurredTarget = target;
        _previousBackgroundEffect = target.Effect;
        _backgroundBlurEffect = new BlurEffect { Radius = 0d };
        target.Effect = _backgroundBlurEffect;
    }

    private async Task AnimateBackgroundBlurAsync(
        double targetRadius,
        TimeSpan duration,
        int version,
        CancellationToken cancellationToken)
    {
        var blur = _backgroundBlurEffect;
        if (blur is null)
        {
            return;
        }

        var startRadius = blur.Radius;
        const int steps = 16;
        var stepDuration = TimeSpan.FromTicks(duration.Ticks / steps);

        try
        {
            for (var step = 1; step <= steps; step++)
            {
                await Task.Delay(stepDuration, cancellationToken);
                if (version != _transitionVersion || !ReferenceEquals(blur, _backgroundBlurEffect))
                {
                    return;
                }

                var progress = (double)step / steps;
                var easedProgress = progress * progress * (3d - (2d * progress));
                blur.Radius = startRadius + ((targetRadius - startRadius) * easedProgress);
            }
        }
        catch (OperationCanceledException)
        {
            // A close or a replacement transition continues from the current radius.
        }
    }

    private void ClearBackgroundBlur()
    {
        if (_blurredTarget is not null)
        {
            _blurredTarget.Effect = _previousBackgroundEffect;
        }

        _blurredTarget = null;
        _previousBackgroundEffect = null;
        _backgroundBlurEffect = null;
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
