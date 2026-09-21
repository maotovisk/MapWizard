using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views.Dialogs;

public partial class SongSelectDialog : UserControl
{
    private static readonly TimeSpan ScrollWorkInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>One corrective pass once the expand/collapse animations (~300 ms)
    /// have settled, in case the up-front seek target estimate was off.</summary>
    private static readonly TimeSpan ExpandSettleDelay = TimeSpan.FromMilliseconds(400);

    private const double CenteredTolerance = 24d;

    private readonly DispatcherTimer _scrollWorkTimer;
    private bool _hasPendingScrollWork;

    /// <summary>Raised when the header close (X) button is clicked.</summary>
    public event EventHandler? PickerCloseRequested;

    private void ClosePickerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PickerCloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public SongSelectDialog()
    {
        _scrollWorkTimer = new DispatcherTimer { Interval = ScrollWorkInterval };
        _scrollWorkTimer.Tick += OnScrollWorkTimerTick;
        InitializeComponent();
        MapsetList.ScrollOwner = MapsetScrollViewer;
        AddHandler(
            InputElement.PointerPressedEvent,
            OnAnyPointerPressed,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private void MapsetScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        _hasPendingScrollWork = true;

        // Run the first update immediately, then keep processing at the timer's
        // cadence while scrolling is producing a high-frequency event stream.
        if (!_scrollWorkTimer.IsEnabled)
        {
            ProcessPendingScrollWork();
            _scrollWorkTimer.Start();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _scrollWorkTimer.Stop();
        _hasPendingScrollWork = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnScrollWorkTimerTick(object? sender, EventArgs e)
    {
        if (!_hasPendingScrollWork)
        {
            _scrollWorkTimer.Stop();
            return;
        }

        ProcessPendingScrollWork();
    }

    private void ProcessPendingScrollWork()
    {
        _hasPendingScrollWork = false;
        if (DataContext is not SongSelectDialogViewModel viewModel || MapsetScrollViewer is null)
        {
            return;
        }

        var (firstVisibleIndex, lastVisibleIndex) = MapsetList.GetVisibleRange();
        if (firstVisibleIndex < 0)
        {
            return;
        }

        viewModel.TryLoadMoreFromScroll(
            MapsetScrollViewer.Offset.Y,
            MapsetScrollViewer.Viewport.Height,
            MapsetScrollViewer.Extent.Height,
            firstVisibleIndex,
            lastVisibleIndex);
    }

    private void OnAnyPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual source || MapsetScrollViewer is null)
        {
            return;
        }

        var index = MapsetList.GetItemIndexForVisual(source);
        if (index < 0 ||
            DataContext is not SongSelectDialogViewModel viewModel ||
            viewModel.VisibleMapsets.Count <= index ||
            viewModel.VisibleMapsets[index] is not { } mapset ||
            mapset.IsExpanded ||
            !IsWithinCardHeaderHit(source))
        {
            return;
        }

        // One synchronized motion: the scroll glides to where the card will sit
        // once it has expanded (computed up front from the list's exact height
        // records), while the expansion animation runs over the same duration and
        // easing. The toggle executes here because the glide scrolls the content
        // out from under the pointer and would break the header button's
        // press/release pairing.
        if (MapsetList.TryComputeSeekTargetOffset(index, out var targetOffset))
        {
            MapsetList.BeginScrollToOffset(targetOffset);
        }

        if (viewModel.ToggleMapsetExpansionCommand.CanExecute(mapset))
        {
            viewModel.ToggleMapsetExpansionCommand.Execute(mapset);
        }

        _ = RefineCenterAfterToggleAsync(viewModel, mapset);
    }

    private async Task RefineCenterAfterToggleAsync(
        SongSelectDialogViewModel viewModel,
        SongMapsetCardViewModel mapset)
    {
        await Task.Delay(ExpandSettleDelay);

        if (DataContext is not SongSelectDialogViewModel currentViewModel ||
            !ReferenceEquals(currentViewModel, viewModel) ||
            MapsetScrollViewer is null ||
            !mapset.IsExpanded)
        {
            return;
        }

        var index = viewModel.VisibleMapsets.IndexOf(mapset);
        if (index < 0 || !MapsetList.TryComputeFullCardCenteredOffset(index, out var target))
        {
            return;
        }

        var maxOffset = Math.Max(0d, MapsetScrollViewer.Extent.Height - MapsetScrollViewer.Viewport.Height);
        target = Math.Clamp(target, 0d, maxOffset);

        if (Math.Abs(MapsetScrollViewer.Offset.Y - target) > CenteredTolerance)
        {
            MapsetList.BeginScrollToOffset(target, TimeSpan.FromMilliseconds(150));
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
