using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace MapWizard.Desktop.Views.Controls;

public class CenteredWrapPanel : Panel
{
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<CenteredWrapPanel, double>(nameof(ItemSpacing), 0d);

    public static readonly StyledProperty<int> MaxItemsPerLineProperty =
        AvaloniaProperty.Register<CenteredWrapPanel, int>(nameof(MaxItemsPerLine), int.MaxValue);

    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<CenteredWrapPanel, double>(nameof(ItemWidth), double.NaN);

    public static readonly StyledProperty<bool> FillAvailableWidthProperty =
        AvaloniaProperty.Register<CenteredWrapPanel, bool>(nameof(FillAvailableWidth));

    public static readonly StyledProperty<double> MinimumItemWidthProperty =
        AvaloniaProperty.Register<CenteredWrapPanel, double>(nameof(MinimumItemWidth), 135d);

    static CenteredWrapPanel()
    {
        AffectsMeasure<CenteredWrapPanel>(ItemSpacingProperty, MaxItemsPerLineProperty,
            ItemWidthProperty, FillAvailableWidthProperty, MinimumItemWidthProperty);
    }

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public int MaxItemsPerLine
    {
        get => GetValue(MaxItemsPerLineProperty);
        set => SetValue(MaxItemsPerLineProperty, value);
    }

    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public bool FillAvailableWidth
    {
        get => GetValue(FillAvailableWidthProperty);
        set => SetValue(FillAvailableWidthProperty, value);
    }

    public double MinimumItemWidth
    {
        get => GetValue(MinimumItemWidthProperty);
        set => SetValue(MinimumItemWidthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (FillAvailableWidth && !double.IsInfinity(availableSize.Width))
        {
            return MeasureFilled(availableSize);
        }

        var hasFiniteWidth = !double.IsInfinity(availableSize.Width);
        var maxLineWidth = 0d;
        var currentLineWidth = 0d;
        var currentLineHeight = 0d;
        var totalHeight = 0d;
        var hasAnyChild = false;
        var currentLineCount = 0;

        foreach (var child in Children)
        {
            var configuredWidth = GetRegularItemWidth(availableSize.Width);
            child.Measure(double.IsNaN(configuredWidth)
                ? availableSize
                : new Size(configuredWidth, availableSize.Height));
            if (!child.IsVisible)
            {
                continue;
            }

            var childSize = child.DesiredSize;
            var childWidth = double.IsNaN(configuredWidth) ? childSize.Width : configuredWidth;
            if (currentLineWidth > 0 &&
                (currentLineCount >= Math.Max(1, MaxItemsPerLine) ||
                 (hasFiniteWidth && currentLineWidth + ItemSpacing + childWidth > availableSize.Width)))
            {
                maxLineWidth = Math.Max(maxLineWidth, currentLineWidth);
                totalHeight += currentLineHeight + ItemSpacing;
                currentLineWidth = childWidth;
                currentLineHeight = childSize.Height;
                currentLineCount = 1;
            }
            else
            {
                currentLineWidth += currentLineWidth > 0 ? ItemSpacing + childWidth : childWidth;
                currentLineHeight = Math.Max(currentLineHeight, childSize.Height);
                currentLineCount++;
            }

            hasAnyChild = true;
        }

        if (hasAnyChild)
        {
            maxLineWidth = Math.Max(maxLineWidth, currentLineWidth);
            totalHeight += currentLineHeight;
        }

        var desiredWidth = hasFiniteWidth ? Math.Min(maxLineWidth, availableSize.Width) : maxLineWidth;
        return new Size(desiredWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (FillAvailableWidth && !double.IsInfinity(finalSize.Width))
        {
            return ArrangeFilled(finalSize);
        }

        var lines = BuildLines(finalSize.Width);
        var y = 0d;

        foreach (var line in lines)
        {
            var x = Math.Max(0d, (finalSize.Width - line.Width) / 2d);
            foreach (var child in line.Children)
            {
                var childSize = child.DesiredSize;
                var childWidth = GetRegularItemWidth(finalSize.Width);
                if (double.IsNaN(childWidth)) childWidth = childSize.Width;
                child.Arrange(new Rect(x, y, childWidth, childSize.Height));
                x += childWidth + ItemSpacing;
            }

            y += line.Height + ItemSpacing;
        }

        return finalSize;
    }

    private Size MeasureFilled(Size availableSize)
    {
        var visibleCount = 0;
        foreach (var child in Children)
        {
            if (child.IsVisible) visibleCount++;
        }

        if (visibleCount == 0) return new Size(0, 0);

        var columns = GetFilledColumnCount(availableSize.Width, visibleCount);
        var itemWidth = Math.Max(0, (availableSize.Width - ItemSpacing * (columns - 1)) / columns);
        var rowHeight = 0d;
        var totalHeight = 0d;
        var column = 0;

        foreach (var child in Children)
        {
            child.Measure(new Size(itemWidth, availableSize.Height));
            if (!child.IsVisible) continue;

            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            column++;
            if (column != columns) continue;

            totalHeight += rowHeight + ItemSpacing;
            rowHeight = 0;
            column = 0;
        }

        totalHeight += rowHeight;
        if (column == 0) totalHeight -= ItemSpacing;
        return new Size(availableSize.Width, Math.Max(0, totalHeight));
    }

    private Size ArrangeFilled(Size finalSize)
    {
        var visibleChildren = new List<Control>();
        foreach (var child in Children)
        {
            if (child.IsVisible) visibleChildren.Add(child);
        }

        if (visibleChildren.Count == 0) return finalSize;

        var columns = GetFilledColumnCount(finalSize.Width, visibleChildren.Count);
        var itemWidth = Math.Max(0, (finalSize.Width - ItemSpacing * (columns - 1)) / columns);
        var y = 0d;

        for (var start = 0; start < visibleChildren.Count; start += columns)
        {
            var rowCount = Math.Min(columns, visibleChildren.Count - start);
            var rowWidth = rowCount * itemWidth + (rowCount - 1) * ItemSpacing;
            var x = Math.Max(0, (finalSize.Width - rowWidth) / 2);
            var rowHeight = 0d;
            for (var i = 0; i < rowCount; i++)
            {
                rowHeight = Math.Max(rowHeight, visibleChildren[start + i].DesiredSize.Height);
            }

            for (var i = 0; i < rowCount; i++)
            {
                visibleChildren[start + i].Arrange(new Rect(x, y, itemWidth, rowHeight));
                x += itemWidth + ItemSpacing;
            }

            y += rowHeight + ItemSpacing;
        }

        return finalSize;
    }

    private int GetFilledColumnCount(double availableWidth, int visibleCount)
    {
        var minimumWidth = Math.Max(1, MinimumItemWidth);
        var fittingColumns = (int)Math.Max(1,
            Math.Floor((Math.Max(0, availableWidth) + ItemSpacing) / (minimumWidth + ItemSpacing)));
        return Math.Min(visibleCount, Math.Min(Math.Max(1, MaxItemsPerLine), fittingColumns));
    }

    private double GetRegularItemWidth(double availableWidth)
    {
        if (double.IsNaN(ItemWidth)) return double.NaN;
        return double.IsInfinity(availableWidth) ? ItemWidth : Math.Min(ItemWidth, availableWidth);
    }

    private List<PanelLine> BuildLines(double availableWidth)
    {
        var hasFiniteWidth = !double.IsInfinity(availableWidth);
        var lines = new List<PanelLine>();
        var currentLineChildren = new List<Control>();
        var currentLineWidth = 0d;
        var currentLineHeight = 0d;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            var childSize = child.DesiredSize;
            var configuredWidth = GetRegularItemWidth(availableWidth);
            var childWidth = double.IsNaN(configuredWidth) ? childSize.Width : configuredWidth;
            var childWidthWithSpacing = currentLineWidth > 0 ? ItemSpacing + childWidth : childWidth;
            var overflowsLine = currentLineChildren.Count >= Math.Max(1, MaxItemsPerLine) ||
                                (hasFiniteWidth && currentLineWidth > 0 && currentLineWidth + childWidthWithSpacing > availableWidth);

            if (overflowsLine)
            {
                lines.Add(new PanelLine(currentLineChildren, currentLineWidth, currentLineHeight));
                currentLineChildren = [child];
                currentLineWidth = childWidth;
                currentLineHeight = childSize.Height;
                continue;
            }

            currentLineChildren.Add(child);
            currentLineWidth += childWidthWithSpacing;
            currentLineHeight = Math.Max(currentLineHeight, childSize.Height);
        }

        if (currentLineChildren.Count > 0)
        {
            lines.Add(new PanelLine(currentLineChildren, currentLineWidth, currentLineHeight));
        }

        return lines;
    }

    private sealed record PanelLine(IReadOnlyList<Control> Children, double Width, double Height);
}
