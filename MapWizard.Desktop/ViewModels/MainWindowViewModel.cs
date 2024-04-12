using System.Reactive;
using ReactiveUI;

namespace MapWizard.Desktop.ViewModels;
public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase currentView;
    public ViewModelBase CurrentView
    {
        get => currentView;
        set => this.RaiseAndSetIfChanged(ref currentView, value);
    }

    public ReactiveCommand<Unit, Unit> ShowWelcomePageCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowHitsoundCopierCommand { get; }

    public MainWindowViewModel()
    {
        CurrentView = new WelcomePageViewModel();
        currentView = CurrentView;
        ShowWelcomePageCommand = ReactiveCommand.Create(ShowWelcomePage);
        ShowHitsoundCopierCommand = ReactiveCommand.Create(ShowHitsoundCopier);
    }

    public void ShowWelcomePage()
    {
        CurrentView = new WelcomePageViewModel();
    }

    public void ShowHitsoundCopier()
    {
        CurrentView = new HitsoundCopierViewModel();
    }
}