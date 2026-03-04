using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace MapWizard.Desktop.Views.Controls;

public partial class ModernColorPicker : UserControl
{
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ModernColorPicker, Color>(
            nameof(SelectedColor),
            Colors.White,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private bool _isUpdatingFromControl;
    private bool _isUpdatingFromHex;

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public ModernColorPicker()
    {
        InitializeComponent();

        RedSlider.PropertyChanged += SliderOnPropertyChanged;
        GreenSlider.PropertyChanged += SliderOnPropertyChanged;
        BlueSlider.PropertyChanged += SliderOnPropertyChanged;

        UpdateUiFromColor(SelectedColor);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedColorProperty && !_isUpdatingFromControl)
        {
            UpdateUiFromColor(change.GetNewValue<Color>());
        }
    }

    private void SliderOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RangeBase.ValueProperty || _isUpdatingFromControl)
        {
            return;
        }

        _isUpdatingFromControl = true;
        SelectedColor = Color.FromRgb(
            (byte)Math.Clamp((int)RedSlider.Value, 0, 255),
            (byte)Math.Clamp((int)GreenSlider.Value, 0, 255),
            (byte)Math.Clamp((int)BlueSlider.Value, 0, 255));
        _isUpdatingFromControl = false;

        UpdateUiFromColor(SelectedColor);
    }

    private void PresetColor_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string hex } || !TryParseHexColor(hex, out var color))
        {
            return;
        }

        SelectedColor = color;
        UpdateUiFromColor(color);
    }

    private void TriggerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        FlyoutBase.ShowAttachedFlyout(TriggerButton);
    }

    private void CloseFlyout_OnClick(object? sender, RoutedEventArgs e)
    {
        if (FlyoutBase.GetAttachedFlyout(TriggerButton) is FlyoutBase flyout)
        {
            flyout.Hide();
        }
    }

    private void ApplyHexColor_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyHexColor();
    }

    private void HexTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingFromHex)
        {
            return;
        }

        // Apply once the user reached a full #RRGGBB token.
        if (HexTextBox.Text?.Trim().Length == 7)
        {
            ApplyHexColor();
        }
    }

    private void ApplyHexColor()
    {
        var input = HexTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(input) || !TryParseHexColor(input, out var color))
        {
            return;
        }

        SelectedColor = color;
        UpdateUiFromColor(color);
    }

    private void UpdateUiFromColor(Color color)
    {
        _isUpdatingFromControl = true;
        _isUpdatingFromHex = true;

        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        var hexText = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        HexTextBox.Text = hexText;
        CompactHexText.Text = hexText;
        PopupPreviewSwatch.Background = new SolidColorBrush(color);
        CompactPreviewSwatch.Background = new SolidColorBrush(color);

        _isUpdatingFromHex = false;
        _isUpdatingFromControl = false;
    }

    private static bool TryParseHexColor(string input, out Color color)
    {
        color = Colors.White;

        var hex = input.Trim();
        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        if (hex.Length != 6 || !byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out var r)
                            || !byte.TryParse(hex.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
                            || !byte.TryParse(hex.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return false;
        }

        color = Color.FromRgb(r, g, b);
        return true;
    }
}
