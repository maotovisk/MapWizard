using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views.Settings;

public partial class SettingsView : UserControl
{
    private bool _isUpdatingSectionSelection;

    public SettingsView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.RefreshPersistedValues();
            }
        };
    }

    private void SettingsLayoutRoot_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var showCategoryRail = e.NewSize.Width >= 860;
        if (showCategoryRail == SettingsCategoryRail.IsVisible)
        {
            return;
        }

        SettingsCategoryRail.IsVisible = showCategoryRail;
        SettingsLayoutRoot.ColumnDefinitions[0].Width = new GridLength(showCategoryRail ? 170 : 0);
    }

    private void CategoryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isUpdatingSectionSelection || sender is not RadioButton { Tag: string sectionName })
        {
            return;
        }

        var section = GetSection(sectionName);
        var position = section.TranslatePoint(default, SettingsSections);
        if (position is null)
        {
            return;
        }

        var targetOffset = Math.Clamp(
            position.Value.Y,
            0,
            Math.Max(0, SettingsScrollViewer.Extent.Height - SettingsScrollViewer.Viewport.Height));
        SettingsScrollViewer.ScrollTo(new Vector(SettingsScrollViewer.Offset.X, targetOffset));
    }

    private void SettingsScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var sections = new (Control Section, RadioButton Button)[]
        {
            (GeneralSection, GeneralNav),
            (AppearanceSection, AppearanceNav),
            (AudioSection, AudioNav),
            (ExperimentalSection, ExperimentalNav),
            (InformationSection, InformationNav),
            (SupportSection, SupportNav)
        };

        var selected = sections[0].Button;
        var probe = SettingsScrollViewer.Offset.Y + 48;
        foreach (var (section, button) in sections)
        {
            var position = section.TranslatePoint(default, SettingsSections);
            if (position is not null && position.Value.Y <= probe)
            {
                selected = button;
            }
        }

        _isUpdatingSectionSelection = true;
        selected.IsChecked = true;
        _isUpdatingSectionSelection = false;
    }

    private Control GetSection(string sectionName) => sectionName switch
    {
        "Appearance" => AppearanceSection,
        "Audio" => AudioSection,
        "Experimental" => ExperimentalSection,
        "Information" => InformationSection,
        "Support" => SupportSection,
        _ => GeneralSection
    };
}
