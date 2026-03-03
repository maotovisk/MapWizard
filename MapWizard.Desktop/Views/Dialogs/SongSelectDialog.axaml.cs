using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views.Dialogs;

public partial class SongSelectDialog : UserControl
{
    private const double CenterScrollEdgeThreshold = 56d;

    public SongSelectDialog()
    {
        InitializeComponent();
        AddHandler(
            InputElement.PointerPressedEvent,
            OnAnyPointerPressed,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private void MapsetScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || DataContext is not SongSelectDialogViewModel viewModel)
        {
            return;
        }

        int? firstVisibleIndex = null;
        int? lastVisibleIndex = null;
        try
        {
            (firstVisibleIndex, lastVisibleIndex) = GetVisibleMapsetRange(scrollViewer);
        }
        catch (Exception)
        {
            // Containers can be re-realized while the window is being resized quickly.
            // Fall back to estimated viewport math in the view-model for this tick.
        }

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

        foreach (var container in MapsetItemsControl.GetRealizedContainers().ToArray())
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

    private void OnAnyPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual source || MapsetScrollViewer is null || MapsetItemsControl.ItemCount == 0)
        {
            return;
        }

        var container = FindMapsetContainer(source);
        if (container is null)
        {
            return;
        }

        var recentered = CenterContainerIfNearViewportEdge(MapsetScrollViewer, container);
        if (!recentered ||
            DataContext is not SongSelectDialogViewModel viewModel ||
            container.DataContext is not SongMapsetCardViewModel mapset ||
            mapset.IsExpanded ||
            !IsWithinCardHeaderHit(source))
        {
            return;
        }

        _ = EnsureExpandedAfterRecenteringAsync(viewModel, mapset);
    }

    private Control? FindMapsetContainer(Visual source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is Control control)
            {
                try
                {
                    if (MapsetItemsControl.IndexFromContainer(control) >= 0)
                    {
                        return control;
                    }
                }
                catch (Exception)
                {
                    // Ignore container checks while item controls are being re-realized.
                }
            }

            current = current.GetVisualParent();
        }

        return null;
    }

    private static bool CenterContainerIfNearViewportEdge(ScrollViewer scrollViewer, Control container)
    {
        if (container.Bounds.Height <= 0d || scrollViewer.Viewport.Height <= 0d)
        {
            return false;
        }

        var topLeft = container.TranslatePoint(new Point(0d, 0d), scrollViewer);
        if (!topLeft.HasValue)
        {
            return false;
        }

        var top = topLeft.Value.Y;
        var bottom = top + container.Bounds.Height;
        var viewportHeight = scrollViewer.Viewport.Height;

        var isNearTopEdge = top < CenterScrollEdgeThreshold;
        var isNearBottomEdge = bottom > viewportHeight - CenterScrollEdgeThreshold;
        if (!isNearTopEdge && !isNearBottomEdge)
        {
            return false;
        }

        var itemCenterInExtent = scrollViewer.Offset.Y + top + (container.Bounds.Height / 2d);
        var desiredOffsetY = itemCenterInExtent - (viewportHeight / 2d);

        var maxOffsetY = Math.Max(0d, scrollViewer.Extent.Height - viewportHeight);
        desiredOffsetY = Math.Clamp(desiredOffsetY, 0d, maxOffsetY);

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, desiredOffsetY);
        return true;
    }

    private async Task EnsureExpandedAfterRecenteringAsync(
        SongSelectDialogViewModel viewModel,
        SongMapsetCardViewModel mapset)
    {
        await Task.Delay(80);

        if (DataContext is not SongSelectDialogViewModel currentViewModel ||
            !ReferenceEquals(currentViewModel, viewModel) ||
            mapset.IsExpanded)
        {
            return;
        }

        if (viewModel.ToggleMapsetExpansionCommand.CanExecute(mapset))
        {
            viewModel.ToggleMapsetExpansionCommand.Execute(mapset);
        }
    }

    private static bool IsWithinCardHeaderHit(Visual source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is Button button && button.Classes.Contains("CardHeaderHit"))
            {
                return true;
            }

            current = current.GetVisualParent();
        }

        return false;
    }
}
