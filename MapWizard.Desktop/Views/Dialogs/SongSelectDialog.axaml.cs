using System;
using Avalonia;
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

        var (firstVisibleIndex, lastVisibleIndex) = GetVisibleMapsetRange(scrollViewer);
        viewModel.TryLoadMoreFromScroll(
            scrollViewer.Offset.Y,
            scrollViewer.Viewport.Height,
            scrollViewer.Extent.Height,
            firstVisibleIndex,
            lastVisibleIndex);
    }

    private (int? firstVisibleIndex, int? lastVisibleIndex) GetVisibleMapsetRange(ScrollViewer scrollViewer)
    {
        if (MapsetItemsControl.ItemCount == 0)
        {
            return (null, null);
        }

        var viewportBottom = scrollViewer.Viewport.Height;
        int? firstVisibleIndex = null;
        int? lastVisibleIndex = null;

        foreach (var container in MapsetItemsControl.GetRealizedContainers())
        {
            if (!container.IsVisible || container.Bounds.Height <= 0d)
            {
                continue;
            }

            var topLeft = container.TranslatePoint(new Point(0d, 0d), scrollViewer);
            if (!topLeft.HasValue)
            {
                continue;
            }

            var top = topLeft.Value.Y;
            var bottom = top + container.Bounds.Height;
            if (bottom <= 0d || top >= viewportBottom)
            {
                continue;
            }

            var index = MapsetItemsControl.IndexFromContainer(container);
            if (index < 0)
            {
                continue;
            }

            firstVisibleIndex = firstVisibleIndex.HasValue
                ? Math.Min(firstVisibleIndex.Value, index)
                : index;
            lastVisibleIndex = lastVisibleIndex.HasValue
                ? Math.Max(lastVisibleIndex.Value, index)
                : index;
        }

        return (firstVisibleIndex, lastVisibleIndex);
    }
}
