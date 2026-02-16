using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using MapWizard.Tools.ComboColourStudio;

namespace MapWizard.Desktop.Views.Controls;

public partial class ColourPointModeDropdown : UserControl
{
    public static readonly StyledProperty<ColourPointMode> SelectedModeProperty =
        AvaloniaProperty.Register<ColourPointModeDropdown, ColourPointMode>(
            nameof(SelectedMode),
            ColourPointMode.Normal,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string> SelectedModeLabelProperty =
        AvaloniaProperty.Register<ColourPointModeDropdown, string>(nameof(SelectedModeLabel), "Sequence");

    public ColourPointMode SelectedMode
    {
        get => GetValue(SelectedModeProperty);
        set => SetValue(SelectedModeProperty, value);
    }

    public string SelectedModeLabel
    {
        get => GetValue(SelectedModeLabelProperty);
        set => SetValue(SelectedModeLabelProperty, value);
    }

    public ColourPointModeDropdown()
    {
        InitializeComponent();
        UpdateSelectedLabel();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedModeProperty)
        {
            UpdateSelectedLabel();
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

    private void SequenceOption_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectedMode = ColourPointMode.Normal;
        CloseFlyout_OnClick(sender, e);
    }

    private void BurstOption_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectedMode = ColourPointMode.Burst;
        CloseFlyout_OnClick(sender, e);
    }

    private void UpdateSelectedLabel()
    {
        SelectedModeLabel = SelectedMode == ColourPointMode.Burst ? "Burst" : "Sequence";
    }
}
