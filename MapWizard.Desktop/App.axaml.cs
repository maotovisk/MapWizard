using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MapWizard.Desktop.DependencyInjection;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MapWizard.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        collection.AddCommonServices();
        var services = collection.BuildServiceProvider();
        services.GetRequiredService<IThemeService>().Initialize();

        var mainWindow = services.GetRequiredService<MainWindow>();

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
    }
    
}
