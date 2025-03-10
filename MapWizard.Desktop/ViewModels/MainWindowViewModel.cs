using System.Linq;
using System.Windows.Input;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace MapWizard.Desktop.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private AvaloniaList<MenuItem> _menuItems;

    private AvaloniaList<ViewModelBase> _views = [];

    private MenuItem _selectedMenuItem;
    public MenuItem SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (SetProperty(ref _selectedMenuItem, value))
            {
                foreach (var item in MenuItems)
                    item.IsSelected = item == value;

                value?.Command.Execute(null);

                SelectedIndex = MenuItems.IndexOf(value);
            }
        }
    }

    [ObservableProperty]
    private int _selectedIndex = 0;
    
    private void SetCurrentViewMenu(string viewName)
    {
        SelectedMenuItem = MenuItems.FirstOrDefault(x => x.Title == viewName);
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
        
        MenuItems = new AvaloniaList<MenuItem>
        {
            new MenuItem { Title = "Start", Icon = MaterialIconKind.Home, Command = ShowWelcomePageCommand },
            new MenuItem { Title = "Hitsound Copier", Icon = MaterialIconKind.ContentCopy, Command = ShowHitsoundCopierCommand },
            new MenuItem { Title = "Metadata Manager", Icon = MaterialIconKind.FileDocumentMultiple, Command = ShowMetadataManagerCommand }
        };

        SelectedMenuItem = MenuItems.FirstOrDefault();
    }
    
    [RelayCommand]
    private void ShowWelcomePage()
    {
        SetCurrentViewMenu("Start");
        CurrentView = _views.First(x => x is WelcomePageViewModel);
    }

    [RelayCommand]
    private void ShowHitsoundCopier()
    {
        SetCurrentViewMenu("Hitsound Copier");
        CurrentView = _views.First(x => x is HitsoundCopierViewModel);
    }
    
    [RelayCommand]
    private void ShowMetadataManager()
    {
        SetCurrentViewMenu("Metadata Manager");
        CurrentView = _views.First(x => x is MetadataManagerViewModel);
    }
}

public class MenuItem : ViewModelBase
{
    public string Title { get; set; }
    public MaterialIconKind Icon { get; set; }
    public ICommand Command { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}