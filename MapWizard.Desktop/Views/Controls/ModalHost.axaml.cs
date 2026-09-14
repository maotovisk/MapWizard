using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace MapWizard.Desktop.Views.Controls;

public partial class ModalHost : UserControl
{
    private static readonly RelativePoint CenterOrigin = new(0.5d, 0.5d, RelativeUnit.Relative);
    private int _transitionVersion;

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

    public ModalHost()
    {
        InitializeComponent();
        DialogCard.RenderTransformOrigin = CenterOrigin;
        BackdropBorder.PointerPressed += BackdropBorder_OnPointerPressed;
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

    public event EventHandler? CloseRequested;

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        var version = ++_transitionVersion;
        BackdropBorder.Opacity = 0d;
        DialogCard.Opacity = 0d;
        DialogCard.RenderTransform = TransformOperations.Parse("scale(0.96)");
        IsVisible = true;
        IsOpen = true;

        try
        {
            // Give the hidden start state a frame before changing animated
            // properties. Otherwise Avalonia paints the final state first.
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            if (version != _transitionVersion)
            {
                return;
            }
            BackdropBorder.Opacity = 0.6d;
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            if (version != _transitionVersion)
            {
                return;
            }
            DialogCard.Opacity = 1d;
            DialogCard.RenderTransform = TransformOperations.Parse("scale(1)");
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // CloseAsync owns the end state when opening is interrupted.
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        ++_transitionVersion;
        if (!IsOpen && !IsVisible)
        {
            return;
        }

        DialogCard.Opacity = 0d;
        DialogCard.RenderTransform = TransformOperations.Parse("scale(0.96)");

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
            BackdropBorder.Opacity = 0d;
            DialogCard.Opacity = 0d;
            DialogCard.RenderTransform = TransformOperations.Parse("scale(0.93)");
        }
    }

    public void Clear()
    {
        Title = null;
        DialogContent = null;
        FooterContent = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FooterContentProperty)
        {
            FooterBorder.IsVisible = FooterContent is not null;
        }
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
