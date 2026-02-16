using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Views.Controls;

public partial class ComboColourDropdown : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<ComboColourOption>?> OptionsProperty =
        AvaloniaProperty.Register<ComboColourDropdown, IReadOnlyList<ComboColourOption>?>(nameof(Options));

    public static readonly StyledProperty<int> SelectedComboNumberProperty =
        AvaloniaProperty.Register<ComboColourDropdown, int>(
            nameof(SelectedComboNumber),
            1,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<ComboColourOption?> SelectedOptionProperty =
        AvaloniaProperty.Register<ComboColourDropdown, ComboColourOption?>(nameof(SelectedOption));

    public static readonly StyledProperty<ICommand?> RemoveTokenCommandProperty =
        AvaloniaProperty.Register<ComboColourDropdown, ICommand?>(nameof(RemoveTokenCommand));

    public static readonly StyledProperty<object?> RemoveTokenCommandParameterProperty =
        AvaloniaProperty.Register<ComboColourDropdown, object?>(nameof(RemoveTokenCommandParameter));

    public static readonly StyledProperty<IBrush> SelectedPreviewBrushProperty =
        AvaloniaProperty.Register<ComboColourDropdown, IBrush>(
            nameof(SelectedPreviewBrush),
            new SolidColorBrush(Colors.White));

    public static readonly StyledProperty<string> SelectedLabelProperty =
        AvaloniaProperty.Register<ComboColourDropdown, string>(nameof(SelectedLabel), "C1");

    private bool _isUpdating;

    public IReadOnlyList<ComboColourOption>? Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public int SelectedComboNumber
    {
        get => GetValue(SelectedComboNumberProperty);
        set => SetValue(SelectedComboNumberProperty, value);
    }

    public ComboColourOption? SelectedOption
    {
        get => GetValue(SelectedOptionProperty);
        set => SetValue(SelectedOptionProperty, value);
    }

    public ICommand? RemoveTokenCommand
    {
        get => GetValue(RemoveTokenCommandProperty);
        set => SetValue(RemoveTokenCommandProperty, value);
    }

    public object? RemoveTokenCommandParameter
    {
        get => GetValue(RemoveTokenCommandParameterProperty);
        set => SetValue(RemoveTokenCommandParameterProperty, value);
    }

    public IBrush SelectedPreviewBrush
    {
        get => GetValue(SelectedPreviewBrushProperty);
        set => SetValue(SelectedPreviewBrushProperty, value);
    }

    public string SelectedLabel
    {
        get => GetValue(SelectedLabelProperty);
        set => SetValue(SelectedLabelProperty, value);
    }

    public ComboColourDropdown()
    {
        InitializeComponent();
        UpdateSelectedUiState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OptionsProperty || change.Property == SelectedComboNumberProperty)
        {
            SyncSelectedOption();
        }

        if (change.Property == SelectedOptionProperty && !_isUpdating && change.GetNewValue<ComboColourOption?>() is { } option)
        {
            _isUpdating = true;
            SelectedComboNumber = option.Number;
            _isUpdating = false;
        }

        if (change.Property == SelectedOptionProperty)
        {
            UpdateSelectedUiState();
        }
    }

    private void SyncSelectedOption()
    {
        if (_isUpdating)
        {
            return;
        }

        var options = Options;
        if (options is null || options.Count == 0)
        {
            SelectedOption = null;
            UpdateSelectedUiState();
            return;
        }

        var match = options.FirstOrDefault(option => option.Number == SelectedComboNumber) ?? options[0];

        _isUpdating = true;
        SelectedOption = match;

        if (SelectedComboNumber != match.Number)
        {
            SelectedComboNumber = match.Number;
        }

        _isUpdating = false;
        UpdateSelectedUiState();
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

    private void ComboOption_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is null)
        {
            return;
        }

        if (button.Tag is int number)
        {
            SelectedComboNumber = number;
        }

        CloseFlyout_OnClick(sender, e);
    }

    private void UpdateSelectedUiState()
    {
        if (SelectedOption is null)
        {
            SelectedPreviewBrush = new SolidColorBrush(Colors.White);
            SelectedLabel = "C?";
            return;
        }

        SelectedPreviewBrush = SelectedOption.PreviewBrush;
        SelectedLabel = $"C{SelectedOption.Number}";
    }
}
