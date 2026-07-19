using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SukiUI.Controls;

namespace MapWizard.Desktop.Controls;

/// <summary>
/// Applies the SukiUI Wayland resize fix while retaining SukiWindow's template and behavior.
/// This can be removed once the fix from SukiUI PR #621 is included in the referenced package.
/// </summary>
public class MapWizardWindow : SukiWindow
{
    public MapWizardWindow()
    {
        if (OperatingSystem.IsLinux())
        {
            AddHandler(
                PointerPressedEvent,
                OnResizeGripPointerPressed,
                RoutingStrategies.Tunnel);
        }
    }

    private void OnResizeGripPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!CanResize || WindowState != WindowState.Normal)
            return;

        if (e.Source is not Border { Tag: string edge })
            return;

        if (!TryGetWindowEdge(edge, out var windowEdge))
            return;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) &&
            WindowDecorations == WindowDecorations.None)
        {
            WindowDecorations = WindowDecorations.BorderOnly;
        }

        // SukiUI 7.0.1 uses VisualRoot here, which is null on Wayland. The
        // SukiWindow instance is already the Window, so invoke the drag on it directly.
        BeginResizeDrag(windowEdge, e);
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
