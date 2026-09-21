using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views.Dialogs;
using MapWizard.Theme.Controls;

namespace MapWizard.Desktop.Controls;

public sealed class ViewportRangeChangedEventArgs : EventArgs
{
    public ViewportRangeChangedEventArgs(int firstVisibleIndex, int lastVisibleIndex)
    {
        FirstVisibleIndex = firstVisibleIndex;
        LastVisibleIndex = lastVisibleIndex;
    }

    public int FirstVisibleIndex { get; }

    public int LastVisibleIndex { get; }
}

/// <summary>
/// Purpose-built virtualized list for the song select picker.
///
/// Unlike <c>VirtualizingStackPanel</c>, item heights are recorded per item instead
/// of being estimated, so the extent and every index-to-offset mapping is exact no
/// matter how deep the list is scrolled. The picker's click-to-seek flow relies on
/// that exactness: the seek target (which must account for the card expanding and
/// for other cards collapsing) is computed up front from the height records, height
/// changes are applied to the bookkeeping immediately as they animate, and items
/// that change height while fully above the viewport get an explicit offset
/// compensation instead of relying on scroll anchoring.
///
/// Place directly inside a ScrollViewer; SmoothScrollViewer supplies smooth wheel
/// scrolling through its compositor animation of the content visual.
/// </summary>
public class SongSelectVirtualizedList : Panel
{
    public static readonly StyledProperty<IList?> ItemsSourceProperty =
        AvaloniaProperty.Register<SongSelectVirtualizedList, IList?>(nameof(ItemsSource));

    public static readonly StyledProperty<Control?> HeaderContentProperty =
        AvaloniaProperty.Register<SongSelectVirtualizedList, Control?>(nameof(HeaderContent));

    /// <summary>Measured height of a collapsed mapset card.</summary>
    private const double CollapsedItemHeight = 96d;

    /// <summary>Height used for items whose real height has never been measured.</summary>
    private const double EstimatedItemHeight = CollapsedItemHeight;

    /// <summary>Realized beyond the viewport by this many pixels on each side.</summary>
    private const double ViewportBuffer = 320d;

    /// <summary>The seek glide matches the expansion animation (300 ms CubicEaseOut),
    /// so the card grows while the list glides — one synchronized motion.</summary>
    private static readonly TimeSpan SeekScrollDuration = TimeSpan.FromMilliseconds(300);

    private const double LayoutEpsilon = 0.5d;

    private double _offsetY;
    private double _viewportHeight;
    private double _headerHeight;
    private double _extentHeight;
    private readonly List<double> _itemHeights = [];
    private readonly Dictionary<int, Control> _realizedByIndex = [];
    private readonly Dictionary<Control, int> _indexByContainer = [];
    private readonly Stack<Control> _containerPool = [];
    private INotifyCollectionChanged? _observedItems;
    private bool _remeasureScheduled;
    private int _lastFirstVisibleIndex = -1;
    private int _lastLastVisibleIndex = -1;

    private DispatcherTimer? _scrollAnimationTimer;
    private double _scrollAnimationFrom;
    private double _scrollAnimationTarget;
    private long _scrollAnimationStartTicks;
    private long _scrollAnimationDurationTicks;

    static SongSelectVirtualizedList()
    {
        AffectsMeasure<SongSelectVirtualizedList>(ItemsSourceProperty);
        AffectsMeasure<SongSelectVirtualizedList>(HeaderContentProperty);
        ItemsSourceProperty.Changed.AddClassHandler<SongSelectVirtualizedList>((panel, _) => panel.OnItemsSourceChanged());
        HeaderContentProperty.Changed.AddClassHandler<SongSelectVirtualizedList>((panel, e) => panel.OnHeaderContentChanged(e));
    }

    public SongSelectVirtualizedList()
    {
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    /// <summary>Raised whenever the range of items intersecting the viewport changes.</summary>
    public event EventHandler<ViewportRangeChangedEventArgs>? ViewportRangeChanged;

    public IList? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Control? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    /// <summary>The scroller hosting this panel; used to compensate for height
    /// changes of items that are fully above the viewport.</summary>
    public ScrollViewer? ScrollOwner { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The collection subscription must survive detach/attach cycles: re-attach
        // idempotently here, otherwise appends are missed after re-attachment.
        AttachItemObservation();
        InvalidateMeasure();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        StopScrollAnimation();
        DetachItemObservation();
        UnrealizeAll(returnToPool: false);
        _containerPool.Clear();
        base.OnDetachedFromVisualTree(e);
    }

    // ------------------------------------------------------------------ items

    private void OnItemsSourceChanged()
    {
        DetachItemObservation();
        UnrealizeAll(returnToPool: true);
        _itemHeights.Clear();
        _lastFirstVisibleIndex = -1;
        _lastLastVisibleIndex = -1;
        _extentHeight = _headerHeight;

        if (ItemsSource is INotifyCollectionChanged notify)
        {
            _observedItems = notify;
            notify.CollectionChanged += OnItemsCollectionChanged;
        }

        SyncHeightRecords();
        AttachItemObservation();
        InvalidateMeasure();
    }

    private void OnHeaderContentChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // HeaderContent must join the panel's children to participate in layout,
        // styles and the logical tree.
        if (e.OldValue is Control oldHeader)
        {
            Children.Remove(oldHeader);
        }

        if (e.NewValue is Control newHeader)
        {
            Children.Add(newHeader);
        }

        InvalidateMeasure();
    }

    private void AttachItemObservation()
    {
        DetachItemObservation();

        if (ItemsSource is not { } items)
        {
            return;
        }

        if (ItemsSource is INotifyCollectionChanged notify)
        {
            _observedItems = notify;
            notify.CollectionChanged += OnItemsCollectionChanged;
        }

        foreach (var item in items)
        {
            if (item is SongMapsetCardViewModel mapset)
            {
                mapset.PropertyChanged += OnItemPropertyChanged;
            }
        }
    }

    private void DetachItemObservation()
    {
        if (_observedItems is { } observed)
        {
            observed.CollectionChanged -= OnItemsCollectionChanged;
            _observedItems = null;
        }

        if (ItemsSource is not { } items)
        {
            return;
        }

        foreach (var item in items)
        {
            if (item is SongMapsetCardViewModel mapset)
            {
                mapset.PropertyChanged -= OnItemPropertyChanged;
            }
        }
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } added:
                // Appends are the picker's paging path; extend the records without
                // disturbing the realized window.
                for (var i = 0; i < added.Count; i++)
                {
                    _itemHeights.Add(EstimatedItemHeight);
                    _extentHeight += EstimatedItemHeight;
                    if (added[i] is SongMapsetCardViewModel mapset)
                    {
                        mapset.PropertyChanged += OnItemPropertyChanged;
                    }
                }

                break;
            default:
                // Resets and structural changes: rebuild bookkeeping wholesale.
                DetachItemObservation();
                UnrealizeAll(returnToPool: true);
                _itemHeights.Clear();
                _extentHeight = _headerHeight;
                SyncHeightRecords();
                AttachItemObservation();
                break;
        }

        InvalidateMeasure();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SongMapsetCardViewModel.IsExpanded) ||
            sender is not SongMapsetCardViewModel item)
        {
            return;
        }

        var index = GetIndexForItem(item);
        if (index < 0 || _realizedByIndex.ContainsKey(index))
        {
            // Realized items: the measure pass records the animated height.
            return;
        }

        // Unrealized card collapsed (only-one-expanded policy): its recorded height
        // is the expanded one while the eventual height is the collapsed one.
        if (!item.IsExpanded && _itemHeights[index] > CollapsedItemHeight + LayoutEpsilon)
        {
            ApplyHeightChange(index, CollapsedItemHeight);
            InvalidateMeasure();
        }
    }

    private void SyncHeightRecords()
    {
        var count = ItemsSource?.Count ?? 0;

        while (_itemHeights.Count > count)
        {
            _extentHeight -= _itemHeights[^1];
            _itemHeights.RemoveAt(_itemHeights.Count - 1);
        }

        for (var i = _itemHeights.Count; i < count; i++)
        {
            _itemHeights.Add(EstimatedItemHeight);
            _extentHeight += EstimatedItemHeight;
        }
    }

    // --------------------------------------------------------------- viewport

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var newOffset = Math.Max(0d, e.EffectiveViewport.Top);
        var newViewport = e.EffectiveViewport.Height;

        if (Math.Abs(newOffset - _offsetY) <= LayoutEpsilon &&
            Math.Abs(newViewport - _viewportHeight) <= LayoutEpsilon)
        {
            return;
        }

        _offsetY = newOffset;
        _viewportHeight = newViewport;
        InvalidateMeasure();
    }

    // ---------------------------------------------------------------- measure

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsNaN(availableSize.Width) ? 0d : availableSize.Width;

        _headerHeight = MeasureHeader(width);

        RealizeItemsInWindow(_headerHeight, width);

        _extentHeight = _headerHeight + _itemHeights.Sum();

        return new Size(Math.Max(width, 0d), Math.Max(_extentHeight, 0d));
    }

    private double MeasureHeader(double width)
    {
        if (HeaderContent is not { } header)
        {
            return 0d;
        }

        header.Measure(new Size(Math.Max(0d, width), double.PositiveInfinity));
        return header.IsVisible ? header.DesiredSize.Height : 0d;
    }

    private void RealizeItemsInWindow(double headerHeight, double width)
    {
        var windowStart = Math.Max(0d, _offsetY - ViewportBuffer);
        var windowEnd = _offsetY + _viewportHeight + ViewportBuffer;

        var itemTop = headerHeight;
        var firstVisible = -1;
        var lastVisible = -1;

        var count = _itemHeights.Count;
        for (var i = 0; i < count; i++)
        {
            var height = _itemHeights[i];
            var itemBottom = itemTop + height;

            if (itemBottom >= windowStart && itemTop <= windowEnd)
            {
                var container = Realize(i);
                if (container is not null)
                {
                    MeasureRealizedItem(i, container, width);
                }
            }
            else
            {
                Unrealize(i);
            }

            if (itemBottom > _offsetY && itemTop < _offsetY + _viewportHeight)
            {
                if (firstVisible < 0)
                {
                    firstVisible = i;
                }

                lastVisible = i;
            }

            itemTop = itemBottom;
        }

        PublishVisibleRange(firstVisible, lastVisible);
    }

    private void PublishVisibleRange(int firstVisible, int lastVisible)
    {
        if (firstVisible == _lastFirstVisibleIndex && lastVisible == _lastLastVisibleIndex)
        {
            return;
        }

        _lastFirstVisibleIndex = firstVisible;
        _lastLastVisibleIndex = lastVisible;

        if (firstVisible >= 0)
        {
            ViewportRangeChanged?.Invoke(this, new ViewportRangeChangedEventArgs(firstVisible, lastVisible));
        }
    }

    // ------------------------------------------------- realization bookkeeping

    private Control? Realize(int index)
    {
        if (_realizedByIndex.TryGetValue(index, out var existing))
        {
            return existing;
        }

        if (ItemsSource is not { } items || index >= items.Count || items[index] is not SongMapsetCardViewModel item)
        {
            return null;
        }

        var container = _containerPool.Count > 0 ? _containerPool.Pop() : new SongMapsetCard();
        container.DataContext = item;
        _realizedByIndex[index] = container;
        _indexByContainer[container] = index;
        Children.Add(container);
        return container;
    }

    private void Unrealize(int index)
    {
        if (!_realizedByIndex.Remove(index, out var container))
        {
            return;
        }

        _indexByContainer.Remove(container);
        container.DataContext = null;
        Children.Remove(container);
        _containerPool.Push(container);
    }

    private void UnrealizeAll(bool returnToPool)
    {
        foreach (var container in _realizedByIndex.Values)
        {
            _indexByContainer.Remove(container);
            container.DataContext = null;
            Children.Remove(container);
            if (returnToPool)
            {
                _containerPool.Push(container);
            }
        }

        _realizedByIndex.Clear();
        _indexByContainer.Clear();
    }

    /// <summary>Measures a realized card and records its height.</summary>
    private void MeasureRealizedItem(int index, Control container, double width)
    {
        container.Measure(new Size(Math.Max(0d, width), double.PositiveInfinity));
        var measuredHeight = container.DesiredSize.Height;

        // A freshly created templated control measures ~0 until its template has
        // been applied (right after it enters the attached tree). Keep the record
        // until a real measurement arrives, then schedule one re-measure.
        if (measuredHeight < 2d)
        {
            ScheduleTemplateRemeasure();
            return;
        }

        ApplyHeightChange(index, measuredHeight);
    }

    private void ScheduleTemplateRemeasure()
    {
        if (_remeasureScheduled)
        {
            return;
        }

        _remeasureScheduled = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                _remeasureScheduled = false;
                InvalidateMeasure();
            },
            DispatcherPriority.Loaded);
    }

    /// <summary>Applies a height change to the bookkeeping, compensating the scroll
    /// offset when the change happened to an item fully above the viewport.</summary>
    private void ApplyHeightChange(int index, double newHeight)
    {
        var previous = _itemHeights[index];
        var delta = newHeight - previous;
        if (Math.Abs(delta) <= LayoutEpsilon)
        {
            return;
        }

        var itemExtentBottom = GetItemExtentTop(index) + previous;

        _itemHeights[index] = newHeight;
        _extentHeight += delta;

        if (itemExtentBottom <= _offsetY + LayoutEpsilon)
        {
            // The item the scroller points at moved; keep the viewport stable.
            _offsetY = Math.Max(0d, _offsetY + delta);
            if (ScrollOwner is { } owner)
            {
                owner.Offset = new Vector(owner.Offset.X, Math.Max(0d, owner.Offset.Y + delta));
            }
        }
    }

    // -------------------------------------------------------------- geometry

    public double GetItemExtentTop(int index)
    {
        var top = _headerHeight;
        for (var i = 0; i < index && i < _itemHeights.Count; i++)
        {
            top += _itemHeights[i];
        }

        return top;
    }

    public int GetItemIndexForVisual(Visual visual)
    {
        var current = visual;
        while (current is not null && !ReferenceEquals(current, this))
        {
            if (current is Control control && _indexByContainer.TryGetValue(control, out var index))
            {
                return index;
            }

            current = current.GetVisualParent();
        }

        return -1;
    }

    public int GetIndexForItem(SongMapsetCardViewModel item)
    {
        if (ItemsSource is not { } items)
        {
            return -1;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Range of item indices intersecting the viewport, or (−1, −1) when empty.</summary>
    public (int First, int Last) GetVisibleRange() => (_lastFirstVisibleIndex, _lastLastVisibleIndex);

    /// <summary>
    /// Offset that centers the clicked card after the seek completes: the card at
    /// <paramref name="index"/> expanded to its details height and every expanded
    /// card above it collapsed back to its collapsed height.
    /// </summary>
    public bool TryComputeSeekTargetOffset(int index, out double offset)
    {
        offset = 0d;
        if (index < 0 || index >= _itemHeights.Count || _viewportHeight <= 0d)
        {
            return false;
        }

        var itemTop = _headerHeight;
        for (var i = 0; i < index; i++)
        {
            itemTop += IsItemPendingCollapse(i) ? CollapsedItemHeight : _itemHeights[i];
        }

        var expandedHeight = ComputeExpandedItemHeight(index);
        offset = Math.Max(0d, itemTop + (expandedHeight / 2d) - (_viewportHeight / 2d));
        return true;
    }

    /// <summary>
    /// Height the card will have once expanded: the current recorded height plus
    /// the measured details content (the same formula AnimatedHeightBorder uses).
    /// </summary>
    private double ComputeExpandedItemHeight(int index)
    {
        var current = _itemHeights[index];
        if (_realizedByIndex.TryGetValue(index, out var container) &&
            container.GetVisualDescendants().OfType<AnimatedHeightBorder>().FirstOrDefault() is { } border &&
            GetBorderContent(border) is { } content &&
            border.Bounds.Width > 0d)
        {
            var contentWidth = Math.Max(0d, border.Bounds.Width - border.Padding.Left - border.Padding.Right);
            content.Measure(new Size(Math.Max(0d, contentWidth), double.PositiveInfinity));
            return current + content.DesiredSize.Height + border.Padding.Top + border.Padding.Bottom;
        }

        return current;
    }

    private static Control? GetBorderContent(AnimatedHeightBorder border)
    {
        return border.Child is ContentPresenter { Child: Control content } ? content : border.Child;
    }

    /// <summary>Offset that centers the full (currently recorded) height of the item.</summary>
    public bool TryComputeFullCardCenteredOffset(int index, out double offset)
    {
        offset = 0d;
        if (index < 0 || index >= _itemHeights.Count || _viewportHeight <= 0d)
        {
            return false;
        }

        var itemTop = GetItemExtentTop(index);
        offset = Math.Max(0d, itemTop + (_itemHeights[index] / 2d) - (_viewportHeight / 2d));
        return true;
    }

    private bool IsItemPendingCollapse(int index)
    {
        if (ItemsSource is not { } items ||
            index >= items.Count ||
            items[index] is not SongMapsetCardViewModel { IsExpanded: true })
        {
            return false;
        }

        return _itemHeights[index] > CollapsedItemHeight + 2d;
    }

    // ------------------------------------------------------- scroll animation

    /// <summary>
    /// Glides the scroller to <paramref name="targetOffset"/> over the expansion
    /// duration with the same CubicEaseOut, so the card grows while the list moves
    /// — one synchronized, natural motion.
    /// </summary>
    public void BeginScrollToOffset(double targetOffset, TimeSpan? duration = null)
    {
        if (ScrollOwner is not { } owner)
        {
            return;
        }

        var maxOffset = Math.Max(0d, _extentHeight - _viewportHeight);
        targetOffset = Math.Clamp(targetOffset, 0d, maxOffset);

        if (Math.Abs(owner.Offset.Y - targetOffset) < LayoutEpsilon)
        {
            return;
        }

        _scrollAnimationFrom = owner.Offset.Y;
        _scrollAnimationTarget = targetOffset;
        _scrollAnimationStartTicks = Stopwatch.GetTimestamp();
        _scrollAnimationDurationTicks = (duration ?? SeekScrollDuration).Ticks;

        _scrollAnimationTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _scrollAnimationTimer.Tick -= OnScrollAnimationTick;
        _scrollAnimationTimer.Tick += OnScrollAnimationTick;
        _scrollAnimationTimer.Start();
        OnScrollAnimationTick(this, EventArgs.Empty);
    }

    public void StopScrollAnimation()
    {
        _scrollAnimationTimer?.Stop();
        _scrollAnimationTimer = null;
    }

    private void OnScrollAnimationTick(object? sender, EventArgs e)
    {
        if (ScrollOwner is not { } owner)
        {
            StopScrollAnimation();
            return;
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - _scrollAnimationStartTicks;
        var progress = Math.Clamp(elapsedTicks / (double)_scrollAnimationDurationTicks, 0d, 1d);
        var eased = 1d - Math.Pow(1d - progress, 3d);

        var offset = _scrollAnimationFrom + (_scrollAnimationTarget - _scrollAnimationFrom) * eased;
        owner.Offset = new Vector(owner.Offset.X, offset);

        if (progress >= 1d)
        {
            StopScrollAnimation();
        }
    }

    // --------------------------------------------------------------- arrange

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (HeaderContent is { } header)
        {
            header.Arrange(new Rect(0d, 0d, finalSize.Width, Math.Max(0d, header.DesiredSize.Height)));
        }

        var itemTop = _headerHeight;
        for (var i = 0; i < _itemHeights.Count; i++)
        {
            if (_realizedByIndex.TryGetValue(i, out var container))
            {
                container.Arrange(new Rect(0d, itemTop, finalSize.Width, Math.Max(0d, container.DesiredSize.Height)));
            }

            itemTop += _itemHeights[i];
        }

        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoundsProperty)
        {
            // Width changes re-measure cards and can change recorded heights.
            InvalidateMeasure();
        }
    }
}
