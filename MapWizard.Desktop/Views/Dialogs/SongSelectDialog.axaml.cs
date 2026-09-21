using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
        // Tunnel so arrow keys are claimed before ScrollViewer's built-in
        // keyboard scrolling eats them when a card button or chip holds focus.
        AddHandler(KeyDownEvent, OnDialogKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Focusable = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Own keyboard focus so arrow navigation works even when no inner
        // control is focused. Deferred one dispatcher pass because the modal
        // host may not have been made visible yet at attach time.
        Dispatcher.UIThread.Post(() => Focus());
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
        MapsetList.KeyboardFocusIndex = index;
        ToggleMapsetWithSeek(viewModel, mapset);
    }

    /// <summary>
    /// Expands/collapses a mapset with the same synchronized scroll glide used by
    /// pointer presses; shared by the mouse and keyboard paths.
    /// </summary>
    private void ToggleMapsetWithSeek(SongSelectDialogViewModel viewModel, SongMapsetCardViewModel mapset)
    {
        var index = viewModel.VisibleMapsets.IndexOf(mapset);
        if (index >= 0 && MapsetList.TryComputeSeekTargetOffset(index, out var targetOffset))
        {
            MapsetList.BeginScrollToOffset(targetOffset);
        }

        if (viewModel.ToggleMapsetExpansionCommand.CanExecute(mapset))
        {
            viewModel.ToggleMapsetExpansionCommand.Execute(mapset);
        }

        _ = RefineCenterAfterToggleAsync(viewModel, mapset);
    }

    // ---------------------------------------------------------------- keyboard

    private void OnDialogKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not SongSelectDialogViewModel viewModel ||
            e.Source is not Visual source ||
            IsInsideTextBox(source))
        {
            return;
        }

        var cardIndex = MapsetList.GetItemIndexForVisual(source);
        var chip = FindAncestorDifficultyChip(source);

        switch (e.Key)
        {
            case Key.Down:
            case Key.Up:
            case Key.PageDown:
            case Key.PageUp:
            case Key.Home:
            case Key.End:
                MoveCardFocus(viewModel, e.Key, cardIndex);
                e.Handled = true;
                break;

            case Key.Left:
            case Key.Right:
                if (chip is not null)
                {
                    MoveChipFocus(chip, e.Key == Key.Right ? 1 : -1);
                }
                else if (cardIndex >= 0 || ResolveFocusedIndex(viewModel) >= 0)
                {
                    ExpandOrCollapseFocusedMapset(viewModel, expand: e.Key == Key.Right);
                }
                else
                {
                    return;
                }

                e.Handled = true;
                break;

            case Key.Enter:
            case Key.Space:
                if (FindAncestorButton(source) is not { } button)
                {
                    // Focus is on the dialog itself: toggle the highlighted card.
                    ToggleFocusedMapset(viewModel);
                    e.Handled = true;
                }
                else if (button.Classes.Contains("CardHeaderHit"))
                {
                    // Route the header activation through the seek path so
                    // keyboard expansion centers the card like a mouse click.
                    if (cardIndex >= 0 &&
                        cardIndex < viewModel.VisibleMapsets.Count &&
                        viewModel.VisibleMapsets[cardIndex] is { } mapset)
                    {
                        MapsetList.KeyboardFocusIndex = cardIndex;
                        ToggleMapsetWithSeek(viewModel, mapset);
                    }

                    e.Handled = true;
                }
                // Any other button (toolbar, difficulty chip, select-all) keeps
                // its own Enter/Space activation.
                break;
        }
    }

    private void MoveCardFocus(SongSelectDialogViewModel viewModel, Key key, int sourceIndex)
    {
        var count = viewModel.VisibleMapsets.Count;
        if (count == 0)
        {
            return;
        }

        // Navigation continues from the current highlight. The card under the
        // logical focus is only used as the anchor while there is no highlight:
        // focus usually stays on the card that was clicked or tabbed into, so
        // re-syncing from it on every keypress would pin the movement to that
        // card instead of walking the list up and down.
        var current = ResolveFocusedIndex(viewModel);
        if (current < 0 && sourceIndex >= 0 && sourceIndex < count)
        {
            current = sourceIndex;
        }

        int target;
        switch (key)
        {
            case Key.Down:
                target = current < 0 ? 0 : Math.Min(current + 1, count - 1);
                break;
            case Key.Up:
                target = current < 0 ? count - 1 : Math.Max(current - 1, 0);
                break;
            case Key.PageDown:
                target = current < 0 ? 0 : Math.Min(current + PageStep(), count - 1);
                break;
            case Key.PageUp:
                target = current < 0 ? count - 1 : Math.Max(current - PageStep(), 0);
                break;
            case Key.Home:
                target = 0;
                break;
            case Key.End:
                target = count - 1;
                break;
            default:
                return;
        }

        MapsetList.KeyboardFocusIndex = target;
        MapsetList.EnsureItemVisible(target);
    }

    private void ExpandOrCollapseFocusedMapset(SongSelectDialogViewModel viewModel, bool expand)
    {
        var index = ResolveFocusedIndex(viewModel);
        if (index < 0 ||
            viewModel.VisibleMapsets[index] is not { } mapset ||
            mapset.IsExpanded == expand)
        {
            return;
        }

        ToggleMapsetWithSeek(viewModel, mapset);
    }

    private void ToggleFocusedMapset(SongSelectDialogViewModel viewModel)
    {
        var index = ResolveFocusedIndex(viewModel);
        if (index < 0 || viewModel.VisibleMapsets[index] is not { } mapset)
        {
            return;
        }

        ToggleMapsetWithSeek(viewModel, mapset);
    }

    private int ResolveFocusedIndex(SongSelectDialogViewModel viewModel)
    {
        var index = MapsetList.KeyboardFocusIndex;
        return index >= 0 && index < viewModel.VisibleMapsets.Count ? index : -1;
    }

    private int PageStep()
    {
        var height = MapsetList.Bounds.Height;
        var step = height > 0d ? (int)(height / 110d) : 6;
        return Math.Max(1, step);
    }

    private void MoveChipFocus(ToggleButton chip, int delta)
    {
        var cardIndex = MapsetList.GetItemIndexForVisual(chip);
        var container = cardIndex >= 0 ? MapsetList.GetRealizedContainer(cardIndex) : null;
        if (container is null)
        {
            return;
        }

        // WrapPanel fills lines in source order, so visual order matches the
        // difficulties collection order.
        var chips = container.GetVisualDescendants()
            .OfType<ToggleButton>()
            .Where(button => button.Classes.Contains("DifficultyChip"))
            .ToList();
        var next = Math.Clamp(chips.IndexOf(chip) + delta, 0, chips.Count - 1);
        if (chips.ElementAtOrDefault(next) is { } target)
        {
            target.Focus();
        }
    }

    private static ToggleButton? FindAncestorDifficultyChip(Visual source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is ToggleButton button && button.Classes.Contains("DifficultyChip"))
            {
                return button;
            }

            current = current.GetVisualParent();
        }

        return null;
    }

    private static Button? FindAncestorButton(Visual source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is Button button)
            {
                return button;
            }

            current = current.GetVisualParent();
        }

        return null;
    }

    private static bool IsInsideTextBox(Visual source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is TextBox)
            {
                return true;
            }

            current = current.GetVisualParent();
        }

        return false;
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
