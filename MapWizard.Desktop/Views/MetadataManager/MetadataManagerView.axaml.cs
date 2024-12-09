using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views;

public partial class MetadataManagerView : UserControl
{
    public MetadataManagerView(MetadataManagerViewModel viewModel)
    {
        // use DI to get the view model
        DataContext = viewModel;
        InitializeComponent();
    }
}