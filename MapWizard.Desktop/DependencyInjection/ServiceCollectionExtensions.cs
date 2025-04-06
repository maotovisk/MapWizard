using System;
using Avalonia.Controls;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MapWizard.Desktop.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        // Register ViewModels
        collection.AddTransient<HitsoundCopierViewModel>();
        collection.AddTransient<MetadataManagerViewModel>();
        collection.AddTransient<WelcomePageViewModel>();
        collection.AddTransient<MainWindowViewModel>();

        // Register Views
        collection.AddTransient<HitsoundCopierView>();
        collection.AddTransient<MetadataManagerView>();
        collection.AddTransient<WelcomePageView>();

        // Register MainWindow
        collection.AddSingleton<MainWindow>();
        
        // Register Lazy<TopLevel> so it can be injected lazily
        collection.AddSingleton<Lazy<TopLevel>>(provider => new Lazy<TopLevel>(provider.GetRequiredService<MainWindow>));

        // Register FilesService with TopLevel
        collection.AddScoped<IFilesService, FilesService>();
        collection.AddScoped<IMetadataManagerService, MetadataManagerService>();
        collection.AddScoped<IHitSoundService, HitSoundService>();
        collection.AddScoped<IOsuMemoryReaderService, OsuMemoryReaderService>();
    }

}