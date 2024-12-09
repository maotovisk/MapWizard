using System.Collections.Generic;
using System.Linq;
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

    private AvaloniaList<ViewModelBase> _views = [];
    
    private void SetCurrentViewMenu(string viewName)
    {
        foreach (var item in MenuItems)
        {
            MenuItems[item.Key] = false;
        }
        MenuItems[viewName] = true;
    }
    
    public MainWindowViewModel(HitsoundCopierViewModel hsVm, MetadataManagerViewModel mmVm, WelcomePageViewModel wpVm)
    {
        _views = 
        [   
            wpVm,
            hsVm,
            mmVm
        ];
        
        CurrentView = wpVm;
        
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
        CurrentView = _views.First(x => x is WelcomePageViewModel);
    }

    [RelayCommand]
    private void ShowHitsoundCopier()
    {
        SetCurrentViewMenu("HitsoundCopier");
        CurrentView = _views.First(x => x is HitsoundCopierViewModel);
    }
    
    [RelayCommand]
    private void ShowMetadataManager()
    {
        SetCurrentViewMenu("MetadataManager");
        CurrentView = _views.First(x => x is MetadataManagerViewModel);
    }
}