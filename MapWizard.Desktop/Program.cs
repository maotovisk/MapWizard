using Avalonia;
using System;
using System.Threading.Tasks;
using Avalonia.Rendering.Composition;
using Velopack;
using Velopack.Sources;

namespace MapWizard.Desktop;

sealed class Program
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
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new SkiaOptions()
            {
                UseOpacitySaveLayer = false
            })
            .With(new X11PlatformOptions()
            {
                RenderingMode = new []
                {
                    X11RenderingMode.Vulkan,
                    X11RenderingMode.Glx,
                    X11RenderingMode.Software
                }
            })
            .LogToTrace();
    
}
