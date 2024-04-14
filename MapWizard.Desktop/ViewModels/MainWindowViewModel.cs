
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MapWizard.Desktop.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase currentView;

    public MainWindowViewModel()
    { 
        CurrentView = new WelcomePageViewModel();
    }

    [RelayCommand]
    private void ShowWelcomePage()
    {
        CurrentView = new WelcomePageViewModel();
    }

    [RelayCommand]
    private void ShowHitsoundCopier()
    {
        CurrentView = new HitsoundCopierViewModel();
    }
}