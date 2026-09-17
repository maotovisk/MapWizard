using System;
using System.ComponentModel;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Enums;
using MapWizard.Desktop.Extensions;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MapWizard.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public INotificationService NotificationService { get; }

        private readonly IServiceProvider _services;
        private HitSoundCopierViewModel? _hitSoundCopierViewModel;
        private HitSoundVisualizerViewModel? _hitSoundVisualizerViewModel;
        private MetadataManagerViewModel? _metadataManagerViewModel;
        private ComboColourStudioViewModel? _comboColourStudioViewModel;
        private MapCleanerViewModel? _mapCleanerViewModel;
        private readonly WelcomePageViewModel _welcomePageViewModel;
        private readonly SettingsViewModel _settingsViewModel;

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
            IServiceProvider services)
        {
            _services = services;
            NotificationService = notificationService;
            _welcomePageViewModel = welcomePageViewModel;
            _settingsViewModel = settingsViewModel;
            CurrentPageViewModel = _welcomePageViewModel;

            Version = updateService.VersionLabel;

            SetPage(NavigationPage.Welcome);
            settingsViewModel.Initialize();
        }

        /// <summary>
        /// Called from the window after it has opened, so the modal host is ready.
        /// </summary>
        public void RequestStartupUpdateCheck() => _ = _welcomePageViewModel.CheckForUpdatesOnStartupAsync();

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
