using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MapWizard.Desktop.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private AvaloniaDictionary<string, bool> _menuItems;
    
    private void SetCurrentViewMenu(string viewName)
    {
        foreach (var item in MenuItems)
        {
            MenuItems[item.Key] = false;
        }
        MenuItems[viewName] = true;
    }
    
    public MainWindowViewModel()
    { 
        CurrentView = new WelcomePageViewModel();
        MenuItems = new AvaloniaDictionary<string, bool>()
        {
            { "WelcomePage", true },
            { "HitsoundCopier", false },
            { "MetadataManager", false }
        };
    }
    
    [RelayCommand]
    private void ShowWelcomePage()
    {
        SetCurrentViewMenu("WelcomePage");
        CurrentView = new WelcomePageViewModel();
    }

    [RelayCommand]
    private void ShowHitsoundCopier()
    {
        SetCurrentViewMenu("HitsoundCopier");
        CurrentView = new HitsoundCopierViewModel();
    }
    
    [RelayCommand]
    private void ShowMetadataManager()
    {
        SetCurrentViewMenu("MetadataManager");
        CurrentView = new MetadataManagerViewModel();
    }
}