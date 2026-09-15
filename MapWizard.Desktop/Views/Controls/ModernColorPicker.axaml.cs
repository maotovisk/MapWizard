using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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

    public static readonly StyledProperty<IReadOnlyList<Color>> SuggestedColoursProperty =
        AvaloniaProperty.Register<ModernColorPicker, IReadOnlyList<Color>>(
            nameof(SuggestedColours),
            Array.Empty<Color>());

    private bool _isUpdatingFromControl;
    private bool _isUpdatingFromHex;
    private HsvColor _currentHsv;

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public IReadOnlyList<Color> SuggestedColours
    {
        get => GetValue(SuggestedColoursProperty);
        set => SetValue(SuggestedColoursProperty, value);
    }

    public ModernColorPicker()
    {
        InitializeComponent();

        ColourSpectrum.PropertyChanged += SpectrumOnPropertyChanged;
        HueSlider.PropertyChanged += HueSliderOnPropertyChanged;

        UpdateUiFromColor(SelectedColor);
        UpdateSuggestedColours();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedColorProperty && !_isUpdatingFromControl)
        {
            UpdateUiFromColor(change.GetNewValue<Color>());
        }

        else if (change.Property == SuggestedColoursProperty)
        {
            UpdateSuggestedColours();
        }
    }

    private void SpectrumOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ColorSpectrum.HsvColorProperty || _isUpdatingFromControl)
        {
            return;
        }

        SetColourFromPicker(ColourSpectrum.HsvColor);
    }

    private void HueSliderOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ColorSlider.HsvColorProperty || _isUpdatingFromControl)
        {
            return;
        }

        SetColourFromPicker(new HsvColor(1, HueSlider.HsvColor.H, _currentHsv.S, _currentHsv.V));
    }

    private void SetColourFromPicker(HsvColor hsv)
    {
        _currentHsv = new HsvColor(1, hsv.H, hsv.S, hsv.V);
        _isUpdatingFromControl = true;
        SelectedColor = _currentHsv.ToRgb();
        _isUpdatingFromControl = false;

        UpdateUiFromHsv(_currentHsv);
    }

    private void SuggestedColour_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Color color })
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

    private void HexTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyHexColor();
            e.Handled = true;
        }
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
        _currentHsv = new HsvColor(color);
        UpdateUiFromHsv(_currentHsv);
    }

    private void UpdateUiFromHsv(HsvColor hsv)
    {
        _isUpdatingFromControl = true;
        _isUpdatingFromHex = true;

        ColourSpectrum.HsvColor = hsv;
        HueSlider.HsvColor = hsv;
        var color = hsv.ToRgb();
        var hexText = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        HexTextBox.Text = hexText;
        CompactHexText.Text = hexText;
        PopupPreviewSwatch.Background = new SolidColorBrush(color);
        CompactPreviewSwatch.Background = new SolidColorBrush(color);

        _isUpdatingFromHex = false;
        _isUpdatingFromControl = false;
        UpdateSuggestedColours();
    }

    private void UpdateSuggestedColours()
    {
        if (SuggestedColoursItemsControl is null)
        {
            return;
        }

        var colours = SuggestedColours ?? Array.Empty<Color>();
        SuggestedColoursSection.IsVisible = colours.Count > 0;
        SuggestedColoursItemsControl.ItemsSource = colours
            .Take(8)
            .Select(color => new SuggestedColourSwatch(color, color.R == SelectedColor.R
                && color.G == SelectedColor.G && color.B == SelectedColor.B))
            .ToArray();
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

public sealed record SuggestedColourSwatch(Color Color, bool IsSelected)
{
    public IBrush Brush { get; } = new SolidColorBrush(Color);
    public string Hex { get; } = $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";
}
