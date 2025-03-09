using Avalonia;
using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views;

public partial class HitsoundCopierView : UserControl
{
    public HitsoundCopierView(HitsoundCopierViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}