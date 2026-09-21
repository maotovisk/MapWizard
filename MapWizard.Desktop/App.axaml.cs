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
using MapWizard.Desktop.Utils;
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
        AppearanceSettings.LoadFrom(settings);

        AvaloniaXamlLoader.Load(this);
        _services.GetRequiredService<IThemeService>().Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        WaylandDispatcherTimerWorkaround.ApplyIfNeeded();

        var services = _services ?? throw new InvalidOperationException("Application services were not initialized.");
        var mainWindow = services.GetRequiredService<MainWindow>();
        mainWindow.Opened += (_, _) =>
        {
            mainWindow.GetViewModel().RequestStartupUpdateCheck();
            mainWindow.GetViewModel().PreloadPages(mainWindow.GetPreloadWarmupHost());
        };

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
