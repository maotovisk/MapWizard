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
        // Register ViewModels
        collection.AddTransient<HitSoundCopierViewModel>();
        collection.AddTransient<MetadataManagerViewModel>();
        collection.AddTransient<WelcomePageViewModel>();
        collection.AddTransient<MainWindowViewModel>();

        // Register Views
        collection.AddTransient<HitSoundCopierView>();
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
        
        // Register new UpdateManager from Velopack
        collection.AddSingleton<UpdateManager>(provider => new UpdateManager(_githubSource));
        
        // Register ToastManager and DialogManager from SukiUI
        collection.AddSingleton<ISukiToastManager, SukiToastManager>();
        collection.AddSingleton<ISukiDialogManager, SukiDialogManager>();
    }

}