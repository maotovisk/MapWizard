using System;
using Avalonia.Controls;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MapWizard.Desktop.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        // ViewModels
        collection.AddTransient<HitSoundCopierViewModel>();
        collection.AddTransient<HitSoundVisualizerViewModel>();
        collection.AddTransient<MetadataManagerViewModel>();
        collection.AddTransient<ComboColourStudioViewModel>();
        collection.AddTransient<MapCleanerViewModel>();
        collection.AddTransient<WelcomePageViewModel>();
        collection.AddTransient<SettingsViewModel>();
        collection.AddTransient<MainWindowViewModel>();

        // Views
        collection.AddTransient<HitSoundCopierView>();
        collection.AddTransient<HitSoundVisualizerView>();
        collection.AddTransient<MetadataManagerView>();
        collection.AddTransient<ComboColourStudioView>();
        collection.AddTransient<MapCleanerView>();
        collection.AddTransient<WelcomePageView>();
        collection.AddTransient<SettingsView>();
        
        // Other stuff
        collection.AddSingleton<MainWindow>();
        
        // INFO: this is required for Some services that need to open native dialog/toasts/parent windows.
        // Removing it may cause some issues.
        collection.AddSingleton<Lazy<TopLevel>>(provider => new Lazy<TopLevel>(provider.GetRequiredService<MainWindow>));

        collection.AddScoped<IFilesService, FilesService>();
        collection.AddScoped<IMetadataManagerService, MetadataManagerService>();
        collection.AddScoped<IHitSoundService, HitSoundService>();
        collection.AddScoped<IComboColourStudioService, ComboColourStudioService>();
        collection.AddScoped<IMapCleanerService, MapCleanerService>();
        collection.AddSingleton<IAudioPlaybackService, ManagedBassPlaybackService>();
        collection.AddSingleton<IComboColourProjectStore, ComboColourProjectStore>();
        collection.AddScoped<IOsuMemoryReaderService, OsuMemoryReaderService>();
        collection.AddSingleton<ISongLibraryService, SongLibraryService>();
        collection.AddSingleton<ISettingsService, SettingsService>();
        collection.AddSingleton<IThemeService, ThemeService>();
        collection.AddSingleton<IUpdateService, UpdateService>();
        
        collection.AddSingleton<ISukiToastManager, SukiToastManager>();
        collection.AddSingleton<ISukiDialogManager, SukiDialogManager>();
    }

}
