using System;
using Avalonia.Controls;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Velopack;
using Velopack.Sources;

namespace MapWizard.Desktop.DependencyInjection;

public static class ServiceCollectionExtensions
{
    
    private static readonly GithubSource _githubSource =
        new("https://github.com/maotovisk/MapWizard", null, false, null);
    
    
    public static void AddCommonServices(this IServiceCollection collection)
    {
        // ViewModels
        collection.AddTransient<HitSoundCopierViewModel>();
        collection.AddTransient<MetadataManagerViewModel>();
        collection.AddTransient<WelcomePageViewModel>();
        collection.AddTransient<MainWindowViewModel>();

        // Views
        collection.AddTransient<HitSoundCopierView>();
        collection.AddTransient<MetadataManagerView>();
        collection.AddTransient<WelcomePageView>();
        
        // Other stuff
        collection.AddSingleton<MainWindow>();
        
        // INFO: this is required for Some services that need to open native dialog/toasts/parent windows.
        // Removing it may cause some issues.
        collection.AddSingleton<Lazy<TopLevel>>(provider => new Lazy<TopLevel>(provider.GetRequiredService<MainWindow>));

        collection.AddScoped<IFilesService, FilesService>();
        collection.AddScoped<IMetadataManagerService, MetadataManagerService>();
        collection.AddScoped<IHitSoundService, HitSoundService>();
        collection.AddScoped<IOsuMemoryReaderService, OsuMemoryReaderService>();
        
        collection.AddSingleton<UpdateManager>(provider => new UpdateManager(_githubSource));
        
        collection.AddSingleton<ISukiToastManager, SukiToastManager>();
        collection.AddSingleton<ISukiDialogManager, SukiDialogManager>();
    }

}