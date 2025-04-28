using Avalonia;
using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL.Egl;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Vulkan;
using Velopack;
using Velopack.Sources;

namespace MapWizard.Desktop;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build()
            .Run();
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new X11PlatformOptions()
            {
                RenderingMode =
                [
                    X11RenderingMode.Glx,
                    X11RenderingMode.Vulkan,
                    X11RenderingMode.Software
                ],
                OverlayPopups = true
            })
            .LogToTrace();
    
}
