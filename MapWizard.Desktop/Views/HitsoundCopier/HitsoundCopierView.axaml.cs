using Avalonia;
using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views;

public partial class HitsoundCopierView : UserControl
{
    public HitsoundCopierView(HitsoundCopierViewModel viewModel)
    {
        // use DI to get the view model
        DataContext = viewModel;
        InitializeComponent();
    }
}