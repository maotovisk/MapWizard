using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views;

public partial class MetadataManagerView : UserControl
{
    public MetadataManagerView(MetadataManagerViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}