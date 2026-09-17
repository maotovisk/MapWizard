using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace MapWizard.Theme.Controls;

/// <summary>
/// MapWizard's client-side-decorated application window.
/// The app provides the titlebar controls. Windows owns its outer shape and
/// corners; Linux keeps the app-drawn shell because compositor behavior varies.
/// On macOS the native traffic lights are kept (full decorations) so the
/// window controls sit at the top-left, as per platform convention.
/// </summary>
public class MapWizardWindow : Window
{
    public MapWizardWindow()
    {
        WindowDecorations = OperatingSystem.IsMacOS()
            ? Avalonia.Controls.WindowDecorations.Full
            : WindowDecorations.BorderOnly;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 60;
        if (OperatingSystem.IsWindows())
        {
            Classes.Add("Windows");
            TransparencyLevelHint = [WindowTransparencyLevel.None];
        }
        else
        {
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
            Background = Brushes.Transparent;
        }
        AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel);
    }

    /// <summary>Whether the platform provides native caption buttons (macOS).</summary>
    public static bool HasNativeWindowControls => OperatingSystem.IsMacOS();

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

        var isTitleBar = false;
        Visual? current = source;
        while (current is not null)
        {
            if (current is Button)
            {
                return;
            }

            if (current is Control control && control.Classes.Contains("WindowTitleBar"))
            {
                isTitleBar = true;
            }

            current = current.GetVisualParent();
        }

        if (!isTitleBar)
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
