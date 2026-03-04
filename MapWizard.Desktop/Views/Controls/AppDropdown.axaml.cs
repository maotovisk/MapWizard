using System;
using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace MapWizard.Desktop.Views.Controls;

public partial class AppDropdown : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<AppDropdown, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<AppDropdown, object?>(
            nameof(SelectedItem),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<AppDropdown, string>(nameof(PlaceholderText), "Select...");

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<AppDropdown, string?>(nameof(DisplayMemberPath));

    public static readonly StyledProperty<double> FlyoutMinWidthProperty =
        AvaloniaProperty.Register<AppDropdown, double>(nameof(FlyoutMinWidth), 180d);

    public static readonly StyledProperty<double> FlyoutMaxHeightProperty =
        AvaloniaProperty.Register<AppDropdown, double>(nameof(FlyoutMaxHeight), 280d);

    public static readonly StyledProperty<string> SelectedTextProperty =
        AvaloniaProperty.Register<AppDropdown, string>(nameof(SelectedText), "Select...");

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public string? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public double FlyoutMinWidth
    {
        get => GetValue(FlyoutMinWidthProperty);
        set => SetValue(FlyoutMinWidthProperty, value);
    }

    public double FlyoutMaxHeight
    {
        get => GetValue(FlyoutMaxHeightProperty);
        set => SetValue(FlyoutMaxHeightProperty, value);
    }

    public string SelectedText
    {
        get => GetValue(SelectedTextProperty);
        private set => SetValue(SelectedTextProperty, value);
    }

    public AppDropdown()
    {
        InitializeComponent();
        UpdateSelectedText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemProperty ||
            change.Property == PlaceholderTextProperty ||
            change.Property == DisplayMemberPathProperty)
        {
            UpdateSelectedText();
        }
    }

    private void TriggerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // Button.Flyout opens automatically; keep handler to mirror custom dropdown controls and future extensibility.
    }

    private void OptionsListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FlyoutBase.GetAttachedFlyout(TriggerButton) is FlyoutBase flyout)
        {
            flyout.Hide();
        }
    }

    private void UpdateSelectedText()
    {
        SelectedText = GetItemDisplayText(SelectedItem) ?? PlaceholderText;
    }

    private string? GetItemDisplayText(object? item)
    {
        if (item is null)
        {
            return null;
        }

        var path = DisplayMemberPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return item.ToString();
        }

        try
        {
            var property = item.GetType().GetProperty(path, BindingFlags.Public | BindingFlags.Instance);
            var value = property?.GetValue(item);
            return value?.ToString() ?? item.ToString();
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return item.ToString();
        }
    }
}
