using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views;

public partial class HitsoundCopierView : UserControl
{
    public HitsoundCopierView()
    {
        InitializeComponent();
        DataContext = new HitsoundCopierViewModel();
    }
}