using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace MapWizard.Desktop.Views.Controls;

public class CenteredWrapPanel : Panel
{
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<CenteredWrapPanel, double>(nameof(ItemSpacing), 0d);

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var hasFiniteWidth = !double.IsInfinity(availableSize.Width);
        var maxLineWidth = 0d;
        var currentLineWidth = 0d;
        var currentLineHeight = 0d;
        var totalHeight = 0d;
        var hasAnyChild = false;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            if (!child.IsVisible)
            {
                continue;
            }

            var childSize = child.DesiredSize;
            if (hasFiniteWidth && currentLineWidth > 0 && currentLineWidth + ItemSpacing + childSize.Width > availableSize.Width)
            {
                maxLineWidth = Math.Max(maxLineWidth, currentLineWidth);
                totalHeight += currentLineHeight + ItemSpacing;
                currentLineWidth = childSize.Width;
                currentLineHeight = childSize.Height;
            }
            else
            {
                currentLineWidth += currentLineWidth > 0 ? ItemSpacing + childSize.Width : childSize.Width;
                currentLineHeight = Math.Max(currentLineHeight, childSize.Height);
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
        var lines = BuildLines(finalSize.Width);
        var y = 0d;

        foreach (var line in lines)
        {
            var x = Math.Max(0d, (finalSize.Width - line.Width) / 2d);
            foreach (var child in line.Children)
            {
                var childSize = child.DesiredSize;
                child.Arrange(new Rect(x, y, childSize.Width, childSize.Height));
                x += childSize.Width + ItemSpacing;
            }

            y += line.Height + ItemSpacing;
        }

        return finalSize;
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
            var childWidthWithSpacing = currentLineWidth > 0 ? ItemSpacing + childSize.Width : childSize.Width;
            var overflowsLine = hasFiniteWidth && currentLineWidth > 0 && currentLineWidth + childWidthWithSpacing > availableWidth;

            if (overflowsLine)
            {
                lines.Add(new PanelLine(currentLineChildren, currentLineWidth, currentLineHeight));
                currentLineChildren = [child];
                currentLineWidth = childSize.Width;
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
