using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MapWizard.Desktop.Controls;
using Avalonia.Markup.Xaml;
using MapWizard.Desktop.DependencyInjection;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MapWizard.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        var collection = new ServiceCollection();
        collection.AddCommonServices();
        _services = collection.BuildServiceProvider();

        var settings = _services.GetRequiredService<ISettingsService>().GetMainSettings();
        RequestedThemeVariant = ThemeService.ToThemeVariant(settings.ThemeMode);
        SmoothScrollViewer.SetGlobalSmoothScrollingEnabled(settings.EnableSmoothWheelScrolling);

        AvaloniaXamlLoader.Load(this);
        _services.GetRequiredService<IThemeService>().Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = _services ?? throw new InvalidOperationException("Application services were not initialized.");
        var mainWindow = services.GetRequiredService<MainWindow>();
        // Defer the startup update check until the window is open so that the
        // modal host is live; otherwise the update modal falls back to a toast.
        mainWindow.Opened += (_, _) => mainWindow.GetViewModel().RequestStartupUpdateCheck();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = mainWindow;
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = mainWindow;
                break;
        }

        base.OnFrameworkInitializationCompleted();
        RunResizeProbe();
        RunChromeProbe();
    }

    /// <summary>
    /// Prints title-bar control geometry for chrome alignment debugging.
    /// Enable with MAPWIZARD_CHROME_PROBE=1.
    /// </summary>
    private static void RunChromeProbe()
    {
        if (Environment.GetEnvironmentVariable("MAPWIZARD_CHROME_PROBE") != "1" ||
            Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not Views.MainWindow window)
        {
            return;
        }

        void Dump()
        {
            Console.WriteLine("== chrome probe ==");
            foreach (var scrollBar in window.GetVisualDescendants().OfType<ScrollBar>())
            {
                var b = scrollBar.TranslatePoint(new Point(0, 0), window) ?? default;
                var owner = scrollBar.GetVisualAncestors().OfType<Control>().Take(3)
                    .Select(c => c.GetType().Name + (c.Classes.Count > 0 ? $"[{string.Join("+", c.Classes)}]" : null));
                Console.WriteLine(
                    $"scrollbar {scrollBar.Orientation} pos={b.X:F2},{b.Y:F2} size={scrollBar.Bounds.Width:F2}x{scrollBar.Bounds.Height:F2} chain={string.Join(" -> ", owner)}");
            }

            Console.WriteLine($"window pos={window.Position.X}x{window.Position.Y} size={window.Bounds.Width:F2}x{window.Bounds.Height:F2}");
            foreach (var border in window.GetVisualDescendants().OfType<Border>()
                         .Where(b => b.Classes.Contains("ContentIsland")))
            {
                var o = border.TranslatePoint(new Point(0, 0), window) ?? default;
                Console.WriteLine($"contentIsland pos={o.X:F2},{o.Y:F2} size={border.Bounds.Width:F2}x{border.Bounds.Height:F2} clip={border.ClipToBounds}");
            }

            foreach (var viewer in window.GetVisualDescendants().OfType<SmoothScrollViewer>())
            {
                var o = viewer.TranslatePoint(new Point(0, 0), window) ?? default;
                Console.WriteLine($"smoothViewer pos={o.X:F2},{o.Y:F2} size={viewer.Bounds.Width:F2}x{viewer.Bounds.Height:F2}");
            }
            Console.WriteLine("  scrollbar dump end.");
            foreach (var button in window.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.TemplatedControl>())
            {
                if (!button.Classes.Contains("WindowControlButton") &&
                    !button.Classes.Contains("TitleBarActionButton"))
                {
                    continue;
                }

                var origin = button.TranslatePoint(new Point(0, 0), window) ?? default;
                Console.WriteLine(
                    $"{string.Join("+", button.Classes)} pos={origin.X:F1},{origin.Y:F1} size={button.Bounds.Width:F1}x{button.Bounds.Height:F1}");

                foreach (var child in button.GetVisualDescendants().OfType<Visual>())
                {
                    if (child is not Avalonia.Controls.Shapes.Path || child.Bounds.Size.Width == 0)
                    {
                        continue;
                    }

                    var childPos = child.TranslatePoint(new Point(0, 0), button) ?? default;
                    Console.WriteLine(
                        $"  path pos={childPos.X:F1},{childPos.Y:F1} size={child.Bounds.Width:F1}x{child.Bounds.Height:F1}");
                }
            }

            foreach (var border in window.GetVisualDescendants().OfType<Border>()
                         .Where(b => b.Classes.Contains("TitleBarIsland")))
            {
                var origin = border.TranslatePoint(new Point(0, 0), window) ?? default;
                Console.WriteLine(
                    $"island pos={origin.X:F1},{origin.Y:F1} size={border.Bounds.Width:F1}x{border.Bounds.Height:F1}");
            }
        }

        window.Opened += (_, _) => DispatcherTimer.RunOnce(() =>
            {
                var page = Environment.GetEnvironmentVariable("MAPWIZARD_RESIZE_PROBE_PAGE");
                if (!string.IsNullOrEmpty(page))
                {
                    switch (page.ToLowerInvariant())
                    {
                        case "hitsoundcopier": window.NavigateToHitSoundCopier(); break;
                        case "metadata": window.NavigateToMetadataManager(); break;
                        case "colourstudio": window.NavigateToComboColourStudio(); break;
                        case "settings": window.NavigateToSettings(); break;
                        default: break;
                    }
                }

                DispatcherTimer.RunOnce(Dump, TimeSpan.FromMilliseconds(400));
            }, TimeSpan.FromMilliseconds(600));
    }

    private static void RunResizeProbe()
    {
        if (Environment.GetEnvironmentVariable("MAPWIZARD_RESIZE_PROBE") != "1" ||
            Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not { } window)
        {
            return;
        }

        DispatcherTimer.RunOnce(
            () =>
            {
                var flags = (Environment.GetEnvironmentVariable("MAPWIZARD_RESIZE_PROBE_FLAGS") ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                if (flags.Contains("nomask"))
                {
                    foreach (var viewer in window.GetVisualDescendants().OfType<SmoothScrollViewer>())
                    {
                        viewer.IsScrollFadeEnabled = false;
                    }
                }

                if (flags.Contains("noshadow"))
                {
                    foreach (var border in window.GetVisualDescendants().OfType<Border>())
                    {
                        border.BoxShadow = default;
                    }
                }

                if (flags.Contains("bake") || flags.Contains("small"))
                {
                    foreach (var border in window.GetVisualDescendants().OfType<Border>()
                                 .Where(b => b.Classes.Contains("WindowBackdrop")))
                    {
                        var brush = new Avalonia.Media.RadialGradientBrush
                        {
                            Center = new Avalonia.RelativePoint(0.5, 0, Avalonia.RelativeUnit.Relative),
                            GradientOrigin = new Avalonia.RelativePoint(0.5, 0, Avalonia.RelativeUnit.Relative),
                            RadiusX = new Avalonia.RelativeScalar(0.75, Avalonia.RelativeUnit.Relative),
                            RadiusY = new Avalonia.RelativeScalar(0.75, Avalonia.RelativeUnit.Relative)
                        };
                        brush.GradientStops.Add(new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#0EB8DB87"), 0));
                        brush.GradientStops.Add(new Avalonia.Media.GradientStop(Avalonia.Media.Colors.Transparent, 1));
                        border.Opacity = 1;
                        border.Background = brush;
                        if (flags.Contains("small"))
                        {
                            border.Height = 320;
                            border.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                        }
                    }
                }

                if (flags.Contains("nobackdrop"))
                {
                    foreach (var border in window.GetVisualDescendants().OfType<Border>()
                                 .Where(b => b.Classes.Contains("WindowBackdrop")))
                    {
                        border.IsVisible = false;
                    }
                }

                var page = Environment.GetEnvironmentVariable("MAPWIZARD_RESIZE_PROBE_PAGE");
                if (window is Views.MainWindow mainWindow && !string.IsNullOrEmpty(page))
                {
                    switch (page.ToLowerInvariant())
                    {
                        case "hitsoundcopier": mainWindow.NavigateToHitSoundCopier(); break;
                        case "metadata": mainWindow.NavigateToMetadataManager(); break;
                        case "colourstudio": mainWindow.NavigateToComboColourStudio(); break;
                        case "settings": mainWindow.NavigateToSettings(); break;
                        case "start": mainWindow.NavigateToStart(); break;
                    }
                }

                Measure(window);
            },
            TimeSpan.FromMilliseconds(1500));
    }

    private static void Measure(Avalonia.Controls.Window window)
    {
        var sample = new System.Collections.Generic.List<double>();
        var frames = new System.Collections.Generic.List<double>();
        var compositor = Avalonia.Rendering.Composition.Compositor.TryGetDefaultCompositor();
        double lastFrame = Stopwatch.GetTimestamp();
        Action? frameRequest = null;
        frameRequest = () =>
        {
            var now2 = Stopwatch.GetTimestamp();
            frames.Add((now2 - lastFrame) * 1000d / Stopwatch.Frequency);
            lastFrame = now2;
        };
        var cpuStart = Process.GetCurrentProcess().TotalProcessorTime;
        var wall = Stopwatch.StartNew();
        var last = Stopwatch.GetTimestamp();
        var tick = 0;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
        timer.Tick += (_, _) =>
        {
            var now = Stopwatch.GetTimestamp();
            sample.Add((now - last) * 1000d / Stopwatch.Frequency);
            last = now;
            tick++;

            var axis = Environment.GetEnvironmentVariable("MAPWIZARD_RESIZE_PROBE_AXIS") ?? "both";
            var phase = tick * 0.06;
            if (axis != "height")
            {
                window.Width = 1180 + (Math.Sin(phase) * 90);
            }
            if (axis != "width")
            {
                window.Height = 700 + (Math.Cos(phase) * 60);
            }
            compositor?.RequestCompositionUpdate(frameRequest!);

            if (tick < 600)
            {
                return;
            }

            timer.Stop();
            wall.Stop();
            var cpu = Process.GetCurrentProcess().TotalProcessorTime - cpuStart;
            var ordered = sample.OrderBy(x => x).ToArray();
            var p95 = ordered[(int)(ordered.Length * 0.95)];
            var frameStats = frames.Count == 0
                ? "frames=0"
                : $"frames={frames.Count} frameMs={frames.Average():F2} p95Frame={frames.OrderBy(x => x).ElementAt((int)(frames.Count * 0.95)):F2}";
            Console.WriteLine(
                $"PROBE resize ticks={sample.Count} wall={wall.ElapsedMilliseconds}ms cpu={cpu.TotalMilliseconds:F0}ms " +
                $"avgTick={sample.Average():F2}ms p95Tick={p95:F2}ms maxTick={ordered[^1]:F2}ms {frameStats}");
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        };
        timer.Start();
    }
}
