using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views;

namespace MapWizard.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}