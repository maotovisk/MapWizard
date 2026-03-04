using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views.Settings;

public partial class SettingsView : UserControl
{
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
}
