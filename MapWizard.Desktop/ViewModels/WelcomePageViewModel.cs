using Avalonia;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Views;

namespace MapWizard.Desktop.ViewModels;

public partial class WelcomePageViewModel : ViewModelBase
{
    public string Message { get; set; }

    public WelcomePageViewModel()
    {
        Message = "Welcome to MapWizard, select a tool to get started!";
    }
}
