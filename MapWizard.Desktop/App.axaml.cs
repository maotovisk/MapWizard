using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MapWizard.Desktop.Controls;
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
