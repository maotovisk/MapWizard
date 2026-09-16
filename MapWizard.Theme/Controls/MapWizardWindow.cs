using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace MapWizard.Theme.Controls;

/// <summary>
/// MapWizard's client-side-decorated application window.
/// Decoration painting is done by the app: a custom titlebar with caption
/// buttons plus Avalonia's drawn frame (border, shadow, resize grips), so the
/// look is identical on Linux (Wayland/X11), Windows and macOS.
/// </summary>
public class MapWizardWindow : Window
{
    /// <summary>The corner radius applied to the window shell in its normal state.</summary>
    public const double ShellCornerRadius = 12;

    public MapWizardWindow()
    {
        WindowDecorations = WindowDecorations.BorderOnly;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 42;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent;
        AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel);
    }

    public void MinimizeWindow() => WindowState = WindowState.Minimized;

    public void ToggleMaximizeWindow()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    public void CloseWindow() => Close();

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border { Tag: string edge } &&
            CanResize &&
            WindowState == WindowState.Normal &&
            TryGetWindowEdge(edge, out var windowEdge))
        {
            BeginResizeDrag(windowEdge, e);
            e.Handled = true;
            return;
        }

        if (e.Source is not Control source)
        {
            return;
        }

        var ancestors = source.GetVisualAncestors().OfType<Control>().ToArray();
        var isTitleBar = source.Classes.Contains("WindowTitleBar") ||
                         ancestors.Any(x => x.Classes.Contains("WindowTitleBar"));
        var isButton = source is Button || ancestors.Any(x => x is Button);
        if (!isTitleBar || isButton)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.ClickCount == 2 && CanResize)
        {
            ToggleMaximizeWindow();
        }
        else
        {
            BeginMoveDrag(e);
        }

        e.Handled = true;
    }

    private static bool TryGetWindowEdge(string edge, out WindowEdge windowEdge)
    {
        windowEdge = edge switch
        {
            "North" => WindowEdge.North,
            "South" => WindowEdge.South,
            "West" => WindowEdge.West,
            "East" => WindowEdge.East,
            "NW" => WindowEdge.NorthWest,
            "NE" => WindowEdge.NorthEast,
            "SW" => WindowEdge.SouthWest,
            "SE" => WindowEdge.SouthEast,
            _ => default
        };

        return edge is "North" or "South" or "West" or "East" or "NW" or "NE" or "SW" or "SE";
    }
}
