using Avalonia;
using System;
using System.Linq;
using Velopack;

namespace MapWizard.Desktop;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var forceSoftwareRendering = args.Any(static arg =>
                string.Equals(arg, "--software-rendering", StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(
                Environment.GetEnvironmentVariable("MAPWIZARD_FORCE_SOFTWARE_RENDERING"),
                "1",
                StringComparison.Ordinal);

        VelopackApp.Build()
            .Run();
        
        BuildAvaloniaApp(forceSoftwareRendering)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp(bool forceSoftwareRendering)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new X11PlatformOptions()
            {
                RenderingMode = forceSoftwareRendering
                    ? [X11RenderingMode.Software]
                    :
                    [
                        X11RenderingMode.Glx,
                        X11RenderingMode.Vulkan,
                        X11RenderingMode.Software
                    ],
                OverlayPopups = true
            })
            .LogToTrace();
    
}
