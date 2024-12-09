using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views;

public partial class WelcomePageView : UserControl
{
    public WelcomePageView(WelcomePageViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}