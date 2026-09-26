using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Enums;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.Services.MemoryService;
using Microsoft.Extensions.DependencyInjection;

namespace MapWizard.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public INotificationService NotificationService { get; }

        /// <summary>
        /// Beatmap currently open in osu!. Shown in the title bar and inherited by beatmap panels.
        /// </summary>
        public OsuNowPlayingMonitor NowPlaying { get; }

        private readonly IServiceProvider _services;
        private HitSoundCopierViewModel? _hitSoundCopierViewModel;
        private HitSoundVisualizerViewModel? _hitSoundVisualizerViewModel;
        private MetadataManagerViewModel? _metadataManagerViewModel;
        private ComboColourStudioViewModel? _comboColourStudioViewModel;
        private MapCleanerViewModel? _mapCleanerViewModel;
        private readonly WelcomePageViewModel _welcomePageViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private bool _pagesPreloaded;

        [ObservableProperty]
        private string _version = "MapWizard-localdev";

        [ObservableProperty]
        private ViewModelBase _currentPageViewModel;

        [ObservableProperty]
        private bool _isWelcomeSelected;

        [ObservableProperty]
        private bool _isHitSoundCopierSelected;

        [ObservableProperty]
        private bool _isHitSoundVisualizerSelected;

        [ObservableProperty]
        private bool _isMetadataManagerSelected;

        [ObservableProperty]
        private bool _isComboColourStudioSelected;

        [ObservableProperty]
        private bool _isMapCleanerSelected;

        [ObservableProperty]
        private bool _isSettingsSelected;

        public MainWindowViewModel(
            WelcomePageViewModel welcomePageViewModel,
            SettingsViewModel settingsViewModel,
            IUpdateService updateService,
            INotificationService notificationService,
            OsuNowPlayingMonitor nowPlaying,
            IServiceProvider services)
        {
            NowPlaying = nowPlaying;
            _services = services;
            NotificationService = notificationService;
            _welcomePageViewModel = welcomePageViewModel;
            _settingsViewModel = settingsViewModel;
            CurrentPageViewModel = _welcomePageViewModel;

            Version = updateService.VersionLabel;

            SetPage(NavigationPage.Welcome);
            // Also starts the now-playing monitor when it is enabled in the settings.
            settingsViewModel.Initialize();
        }

        /// <summary>
        /// Called from the window after it has opened, so the modal host is ready.
        /// </summary>
        public void RequestStartupUpdateCheck() => _ = _welcomePageViewModel.CheckForUpdatesOnStartupAsync();

        /// <summary>
        /// Builds every page view after startup, so the first navigation to
        /// each page does not pay the XAML/control construction cost. Startup
        /// is deferred past the entrance transition and first frames: building
        /// heavy pages (notably the 900-line HitSound Editor) on the UI thread
        /// during the 300ms entrance animation starves the compositor and makes
        /// the first paint look sluggish. One page is built per idle dispatcher
        /// turn to keep later interaction responsive.
        /// When <paramref name="warmupHost"/> (a panel inside the window) is
        /// given, each built page is also briefly attached at opacity 0 so its
        /// first style/measure/arrange/render work happens off-screen instead
        /// of blocking the transition the first time it is opened.
        /// </summary>
        public void PreloadPages(Avalonia.Controls.Panel? warmupHost = null)
        {
            if (_pagesPreloaded)
            {
                return;
            }

            _pagesPreloaded = true;
            _ = PreloadPagesAfterFirstRenderAsync(warmupHost);
        }

        private async Task PreloadPagesAfterFirstRenderAsync(Avalonia.Controls.Panel? warmupHost)
        {
            try
            {
                // Entrance transition is 300ms; leave headroom for first render,
                // update-check toast, and compositor warm-up before spending UI
                // thread time on speculative XAML inflation.
                await Task.Delay(TimeSpan.FromMilliseconds(800));
            }
            catch
            {
                return;
            }

            // View construction must run on the UI thread; SystemIdle keeps it
            // from stealing frames from input, render, or later transitions.
            // If the user already navigated, ViewLocator hits the cache and skips.
            Dispatcher.UIThread.Post(
                () => PreloadNextPage(new Queue<ViewModelBase>(GetPageViewModels()), warmupHost),
                DispatcherPriority.SystemIdle);
        }

        private void PreloadNextPage(Queue<ViewModelBase> pendingPages, Avalonia.Controls.Panel? warmupHost)
        {
            if (pendingPages.Count == 0)
            {
                return;
            }

            try
            {
                ViewLocator.Preload(pendingPages.Dequeue(), warmupHost);
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            }

            Dispatcher.UIThread.Post(
                () => PreloadNextPage(pendingPages, warmupHost),
                DispatcherPriority.SystemIdle);
        }

        private IEnumerable<ViewModelBase> GetPageViewModels()
        {
            // Welcome is already visible and cached; rebuilding it during the
            // entrance transition would only add UI-thread contention.
            yield return _hitSoundCopierViewModel ??= _services.GetRequiredService<HitSoundCopierViewModel>();
            yield return _hitSoundVisualizerViewModel ??= _services.GetRequiredService<HitSoundVisualizerViewModel>();
            yield return _metadataManagerViewModel ??= _services.GetRequiredService<MetadataManagerViewModel>();
            yield return _comboColourStudioViewModel ??= _services.GetRequiredService<ComboColourStudioViewModel>();
            yield return _mapCleanerViewModel ??= _services.GetRequiredService<MapCleanerViewModel>();
            yield return _settingsViewModel;
        }

        public void NavigateToWelcome() => SetPage(NavigationPage.Welcome);

        public void NavigateToHitSoundCopier() => SetPage(NavigationPage.HitSoundCopier);

        public void NavigateToHitSoundVisualizer() => SetPage(NavigationPage.HitSoundVisualizer);

        public void NavigateToMetadataManager() => SetPage(NavigationPage.MetadataManager);

        public void NavigateToComboColourStudio() => SetPage(NavigationPage.ComboColourStudio);

        public void NavigateToMapCleaner() => SetPage(NavigationPage.MapCleaner);

        public void NavigateToSettings() => SetPage(NavigationPage.Settings);

        private void SetPage(NavigationPage page)
        {
            CurrentPageViewModel = page switch
            {
                NavigationPage.Welcome => _welcomePageViewModel,
                NavigationPage.HitSoundCopier => _hitSoundCopierViewModel ??= _services.GetRequiredService<HitSoundCopierViewModel>(),
                NavigationPage.HitSoundVisualizer => _hitSoundVisualizerViewModel ??= _services.GetRequiredService<HitSoundVisualizerViewModel>(),
                NavigationPage.MetadataManager => _metadataManagerViewModel ??= _services.GetRequiredService<MetadataManagerViewModel>(),
                NavigationPage.ComboColourStudio => _comboColourStudioViewModel ??= _services.GetRequiredService<ComboColourStudioViewModel>(),
                NavigationPage.MapCleaner => _mapCleanerViewModel ??= _services.GetRequiredService<MapCleanerViewModel>(),
                NavigationPage.Settings => _settingsViewModel,
                _ => _welcomePageViewModel
            };

            IsWelcomeSelected = page == NavigationPage.Welcome;
            IsHitSoundCopierSelected = page == NavigationPage.HitSoundCopier;
            IsHitSoundVisualizerSelected = page == NavigationPage.HitSoundVisualizer;
            IsMetadataManagerSelected = page == NavigationPage.MetadataManager;
            IsComboColourStudioSelected = page == NavigationPage.ComboColourStudio;
            IsMapCleanerSelected = page == NavigationPage.MapCleaner;
            IsSettingsSelected = page == NavigationPage.Settings;
        }

        [RelayCommand]
        private void OpenWelcome()
        {
            SetPage(NavigationPage.Welcome);
        }

        [RelayCommand]
        private void OpenHitSoundCopier()
        {
            SetPage(NavigationPage.HitSoundCopier);
        }

        [RelayCommand]
        private void OpenHitSoundVisualizer()
        {
            SetPage(NavigationPage.HitSoundVisualizer);
        }

        [RelayCommand]
        private void OpenMetadataManager()
        {
            SetPage(NavigationPage.MetadataManager);
        }

        [RelayCommand]
        private void OpenComboColourStudio()
        {
            SetPage(NavigationPage.ComboColourStudio);
        }

        [RelayCommand]
        private void OpenMapCleaner()
        {
            SetPage(NavigationPage.MapCleaner);
        }

        [RelayCommand]
        private void OpenSettings()
        {
            SetPage(NavigationPage.Settings);
        }

        [RelayCommand]
        private void OpenGithub()
        {
            OpenExternalLink(AppLinks.Repository, "GitHub");
        }

        [RelayCommand]
        private void OpenDiscord()
        {
            OpenExternalLink(AppLinks.Discord, "Discord");
        }

        private void OpenExternalLink(string url, string title)
        {
            if (AppLinks.TryOpen(url))
            {
                return;
            }

            NotificationService.ShowToast(
                NotificationType.Error,
                title,
                "The link could not be opened.",
                TimeSpan.FromSeconds(8));
        }
    }
}
