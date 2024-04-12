using ReactiveUI;

namespace MapWizard.Desktop.ViewModels
{
    public class WelcomePageViewModel : ViewModelBase
    {
        public string Message { get; set; }

        public WelcomePageViewModel()
        {
            Message = "Welcome to MapWizard, select a tool to get started!";
        }
    }
}