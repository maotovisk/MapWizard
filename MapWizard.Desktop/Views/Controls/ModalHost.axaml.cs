using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
    private const double BackgroundBlurRadius = 10d;

    // The frosted plane is frozen once per open instead of keeping a live
    // BlurEffect on the shell: the live effect re-composites a full window-sized
    // surface on every frame that animates above it, which measured ~10 MB of
    // native memory per frame during the open transition and while scrolling,
    // none of which is returned to the OS.
    private int _transitionVersion;
    private Visual? _blurredTarget;
    private bool _previousBlurTargetVisibility = true;
    private RenderTargetBitmap? _backgroundSnapshot;

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
            // The frosted plane crossfades in with the backdrop: the blur edge
            // becomes smooth without ever keeping a live effect attached.
            BackdropBlurImage.Opacity = 1d;
            await Task.Delay(TimeSpan.FromMilliseconds(80), cancellationToken);
            if (version != _transitionVersion)
            {
                return;
            }
            DialogCard.Opacity = 1d;
            DialogCard.RenderTransform = GetOpenTransform();
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // CloseAsync owns the end state when opening is interrupted.
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _transitionVersion++;
        if (!IsOpen && !IsVisible)
        {
            return;
        }

        DialogCard.Opacity = 0d;
        DialogCard.RenderTransform = GetClosedTransform();
        // The frosted plane crossfades out while the card fades, so the unblur
        // edge is smooth instead of a hard cut.
        BackdropBlurImage.Opacity = 0d;

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);
            BackdropBorder.Opacity = 0d;
            await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);
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
        if (IsOpen && _blurredTarget is not null &&
            (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 0.5d ||
             Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 0.5d))
        {
            CaptureBackgroundSnapshot();
        }

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
        if (target is null || target.Bounds.Width <= 0d || target.Bounds.Height <= 0d)
        {
            return;
        }

        ClearBackgroundBlur();
        _blurredTarget = target;
        _previousBlurTargetVisibility = target.IsVisible;
        CaptureBackgroundSnapshot();
    }

    private void CaptureBackgroundSnapshot()
    {
        var target = _blurredTarget;
        if (target is null)
        {
            return;
        }

        // The frozen copy replaces the live target while the modal is open, so
        // the target has to be visible for the offscreen pass to draw anything.
        target.IsVisible = true;
        var snapshot = CreateBlurredSnapshot(target);
        if (snapshot is null)
        {
            target.IsVisible = _previousBlurTargetVisibility;
            _blurredTarget = null;
            return;
        }

        target.IsVisible = false;
        _backgroundSnapshot?.Dispose();
        _backgroundSnapshot = snapshot;
        BackdropBlurImage.Source = snapshot;

        // On the first capture the plane starts faded out; OpenAsync fades it in
        // after the first frame so the blur crossfades with the backdrop. A
        // re-capture while open (window resize) swaps the source without a fade.
        if (!BackdropBlurImage.IsVisible)
        {
            BackdropBlurImage.Opacity = 0d;
            BackdropBlurImage.IsVisible = true;
        }
    }

    private static RenderTargetBitmap? CreateBlurredSnapshot(Visual target)
    {
        try
        {
            var bounds = target.Bounds;
            var size = new PixelSize(
                Math.Max(1, (int)Math.Ceiling(bounds.Width)),
                Math.Max(1, (int)Math.Ceiling(bounds.Height)));

            // Both passes are 1:1 in logical pixels. Scaling the bitmap through the
            // render target's DPI is not reliable here: an extended-client-area window
            // renders the target unscaled into the smaller bitmap, which shows up as a
            // magnified, cropped backdrop.
            var frozen = new RenderTargetBitmap(size);
            frozen.Render(target);

            var blurred = new RenderTargetBitmap(size);
            blurred.Render(CreateBlurLayer(frozen, bounds.Size));

            frozen.Dispose();
            return blurred;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return null;
        }
    }

    /// <summary>Lays a frozen copy of the shell out off-tree with the blur applied, ready to be rasterized.</summary>
    private static Image CreateBlurLayer(IImage source, Size bounds)
    {
        var layer = new Image
        {
            Source = source,
            Stretch = Stretch.Fill,
            Width = bounds.Width,
            Height = bounds.Height,
            Effect = new BlurEffect { Radius = BackgroundBlurRadius }
        };
        layer.Measure(bounds);
        layer.Arrange(new Rect(bounds));
        return layer;
    }

    private void ClearBackgroundBlur()
    {
        if (_blurredTarget is not null)
        {
            _blurredTarget.IsVisible = _previousBlurTargetVisibility;
            _blurredTarget = null;
        }

        _previousBlurTargetVisibility = true;
        BackdropBlurImage.Source = null;
        BackdropBlurImage.Opacity = 0d;
        BackdropBlurImage.IsVisible = false;
        _backgroundSnapshot?.Dispose();
        _backgroundSnapshot = null;
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
