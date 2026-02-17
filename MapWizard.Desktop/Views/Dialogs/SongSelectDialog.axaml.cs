using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views.Dialogs;

public partial class SongSelectDialog : UserControl
{
    public SongSelectDialog()
    {
        InitializeComponent();
    }

    private void MapsetScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || DataContext is not SongSelectDialogViewModel viewModel)
        {
            return;
        }

        viewModel.TryLoadMoreFromScroll(
            scrollViewer.Offset.Y,
            scrollViewer.Viewport.Height,
            scrollViewer.Extent.Height);
    }
}
