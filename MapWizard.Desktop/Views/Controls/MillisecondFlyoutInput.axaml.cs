using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using MapWizard.Desktop.Utils;

namespace MapWizard.Desktop.Views.Controls;

public partial class MillisecondFlyoutInput : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MillisecondFlyoutInput, double>(
            nameof(Value),
            0,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string> DisplayValueProperty =
        AvaloniaProperty.Register<MillisecondFlyoutInput, string>(nameof(DisplayValue), "0 ms");

    private bool _isUpdatingFromControl;

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string DisplayValue
    {
        get => GetValue(DisplayValueProperty);
        set => SetValue(DisplayValueProperty, value);
    }

    public MillisecondFlyoutInput()
    {
        InitializeComponent();
        UpdateUiFromValue(Value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty && !_isUpdatingFromControl)
        {
            UpdateUiFromValue(change.GetNewValue<double>());
        }
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

    private void Flyout_OnOpened(object? sender, EventArgs e)
    {
        InputTextBox.Text = FormatValueWithoutUnit(Value);
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }

    private async void InputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsPasteGesture(e))
        {
            e.Handled = true;
            await PasteWithTimestampNormalizationAsync();
            return;
        }

        if (e.Key != Key.Enter)
        {
            return;
        }

        ApplyValueAndClose();
        e.Handled = true;
    }

    private async void InputTextBox_OnPastingFromClipboard(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        await PasteWithTimestampNormalizationAsync();
    }

    private void ApplyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ApplyValueAndClose();
    }

    private void ApplyValueAndClose()
    {
        var rawText = InputTextBox.Text;
        if (MillisecondParser.TryParseMillisecondInput(rawText, out var parsed))
        {
            _isUpdatingFromControl = true;
            Value = parsed;
            _isUpdatingFromControl = false;
            UpdateUiFromValue(parsed);
        }
        else
        {
            InputTextBox.Text = FormatValueWithoutUnit(Value);
            InputTextBox.SelectAll();
            return;
        }

        CloseFlyout_OnClick(this, new RoutedEventArgs());
    }

    private void UpdateUiFromValue(double value)
    {
        DisplayValue = $"{FormatValueWithoutUnit(value)} ms";

        if (InputTextBox is not null)
        {
            InputTextBox.Text = FormatValueWithoutUnit(value);
        }
    }

    private static string FormatValueWithoutUnit(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private async Task PasteWithTimestampNormalizationAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        var clipboardText = await clipboard.TryGetTextAsync();
        if (clipboardText is null)
        {
            return;
        }

        var replacement = MillisecondParser.TryParseMillisecondInput(clipboardText, out var parsed)
            ? FormatValueWithoutUnit(parsed)
            : clipboardText;

        InsertTextAtSelection(replacement);
    }

    private void InsertTextAtSelection(string text)
    {
        var currentText = InputTextBox.Text ?? string.Empty;
        var start = Math.Min(InputTextBox.SelectionStart, InputTextBox.SelectionEnd);
        var end = Math.Max(InputTextBox.SelectionStart, InputTextBox.SelectionEnd);

        var safeStart = Math.Clamp(start, 0, currentText.Length);
        var safeEnd = Math.Clamp(end, 0, currentText.Length);

        var merged = string.Concat(
            currentText.AsSpan(0, safeStart),
            text,
            currentText.AsSpan(safeEnd));

        InputTextBox.Text = merged;

        var caret = safeStart + text.Length;
        InputTextBox.SelectionStart = caret;
        InputTextBox.SelectionEnd = caret;
        InputTextBox.CaretIndex = caret;
    }

    private static bool IsPasteGesture(KeyEventArgs e)
    {
        if (e.Key != Key.V)
        {
            return false;
        }

        var mods = e.KeyModifiers;
        return mods.HasFlag(KeyModifiers.Control) || mods.HasFlag(KeyModifiers.Meta);
    }

}
