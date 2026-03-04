using System;
using System.ComponentModel;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MapWizard.Desktop.Enums;
using MapWizard.Desktop.Services;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace MapWizard.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private ISukiToastManager ToastManager { get; }
        public ISukiDialogManager DialogManager { get; }

        private readonly IThemeService _themeService;
        private bool _isUpdatingFromThemeService;

        private ViewModelBase HitSoundCopierViewModel { get; }
        private ViewModelBase HitSoundVisualizerViewModel { get; }
        private ViewModelBase MetadataManagerViewModel { get; }
        private ViewModelBase ComboColourStudioViewModel { get; }
        private ViewModelBase MapCleanerViewModel { get; }
        private ViewModelBase WelcomePageViewModel { get; }
        private ViewModelBase SettingsPageViewModel { get; }

        [ObservableProperty]
        private bool _isDarkTheme;

        [ObservableProperty]
        private string _version = "MapWizard-localdev";

        [ObservableProperty]
        private MaterialIconKind _themeToggleIcon;

        [ObservableProperty]
        private ViewModelBase _currentPageViewModel;

        [ObservableProperty]
        private bool _isWelcomeSelected;

        [ObservableProperty]
        private bool _isHitSoundCopierSelected;

        [ObservableProperty]
        private bool _isHitSoundVisualizerSelected;

        [ObservableProperty]
        private bool _isHitSoundVisualizerEnabled;

        [ObservableProperty]
        private bool _isMetadataManagerSelected;

        [ObservableProperty]
        private bool _isComboColourStudioSelected;

        [ObservableProperty]
        private bool _isMapCleanerSelected;

        [ObservableProperty]
        private bool _isSettingsSelected;

        partial void OnIsDarkThemeChanged(bool value)
        {
            ThemeToggleIcon = value ? MaterialIconKind.WeatherNight : MaterialIconKind.WhiteBalanceSunny;
            if (_isUpdatingFromThemeService)
            {
                return;
            }

            _themeService.SetDarkTheme(value);
        }

        public MainWindowViewModel(
            WelcomePageViewModel welcomePageViewModel,
            HitSoundCopierViewModel hitSoundCopierViewModel,
            HitSoundVisualizerViewModel hitSoundVisualizerViewModel,
            MetadataManagerViewModel metadataManagerViewModel,
            ComboColourStudioViewModel comboColourStudioViewModel,
            MapCleanerViewModel mapCleanerViewModel,
            SettingsViewModel settingsViewModel,
            IThemeService themeService,
            IUpdateService updateService,
            ISukiToastManager toastManager,
            ISukiDialogManager dialogManager)
        {
            _themeService = themeService;
            ToastManager = toastManager;
            DialogManager = dialogManager;
            HitSoundCopierViewModel = hitSoundCopierViewModel;
            HitSoundVisualizerViewModel = hitSoundVisualizerViewModel;
            MetadataManagerViewModel = metadataManagerViewModel;
            ComboColourStudioViewModel = comboColourStudioViewModel;
            MapCleanerViewModel = mapCleanerViewModel;
            WelcomePageViewModel = welcomePageViewModel;
            SettingsPageViewModel = settingsViewModel;
            CurrentPageViewModel = WelcomePageViewModel;

            Version = updateService.VersionLabel;

            _themeService.DarkThemeChanged += OnDarkThemeChanged;
            UpdateThemeState(_themeService.IsDarkTheme);

            SetPage(NavigationPage.Welcome);
            settingsViewModel.Initialize();
            UpdateHitSoundVisualizerAvailability(settingsViewModel.IsHitSoundVisualizerEnabled);
            settingsViewModel.PropertyChanged += OnSettingsViewModelPropertyChanged;
            _ = welcomePageViewModel.CheckForUpdatesOnStartupAsync();
        }

        public void NavigateToWelcome() => SetPage(NavigationPage.Welcome);

        public void NavigateToHitSoundCopier() => SetPage(NavigationPage.HitSoundCopier);

        public void NavigateToHitSoundVisualizer() => SetPage(IsHitSoundVisualizerEnabled
            ? NavigationPage.HitSoundVisualizer
            : NavigationPage.Welcome);

        public void NavigateToMetadataManager() => SetPage(NavigationPage.MetadataManager);

        public void NavigateToComboColourStudio() => SetPage(NavigationPage.ComboColourStudio);

        public void NavigateToMapCleaner() => SetPage(NavigationPage.MapCleaner);

        public void NavigateToSettings() => SetPage(NavigationPage.Settings);

        private void SetPage(NavigationPage page)
        {
            if (page == NavigationPage.HitSoundVisualizer && !IsHitSoundVisualizerEnabled)
            {
                page = NavigationPage.Welcome;
            }

            CurrentPageViewModel = page switch
            {
                NavigationPage.Welcome => WelcomePageViewModel,
                NavigationPage.HitSoundCopier => HitSoundCopierViewModel,
                NavigationPage.HitSoundVisualizer => HitSoundVisualizerViewModel,
                NavigationPage.MetadataManager => MetadataManagerViewModel,
                NavigationPage.ComboColourStudio => ComboColourStudioViewModel,
                NavigationPage.MapCleaner => MapCleanerViewModel,
                NavigationPage.Settings => SettingsPageViewModel,
                _ => WelcomePageViewModel
            };

            IsWelcomeSelected = page == NavigationPage.Welcome;
            IsHitSoundCopierSelected = page == NavigationPage.HitSoundCopier;
            IsHitSoundVisualizerSelected = page == NavigationPage.HitSoundVisualizer;
            IsMetadataManagerSelected = page == NavigationPage.MetadataManager;
            IsComboColourStudioSelected = page == NavigationPage.ComboColourStudio;
            IsMapCleanerSelected = page == NavigationPage.MapCleaner;
            IsSettingsSelected = page == NavigationPage.Settings;
        }

        private void OnDarkThemeChanged(object? sender, bool isDarkTheme)
        {
            UpdateThemeState(isDarkTheme);
        }

        private void UpdateThemeState(bool isDarkTheme)
        {
            _isUpdatingFromThemeService = true;
            ThemeToggleIcon = isDarkTheme ? MaterialIconKind.WeatherNight : MaterialIconKind.WhiteBalanceSunny;
            IsDarkTheme = isDarkTheme;
            _isUpdatingFromThemeService = false;
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
            SetPage(IsHitSoundVisualizerEnabled
                ? NavigationPage.HitSoundVisualizer
                : NavigationPage.Welcome);
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
            var githubUrl = "https://github.com/maotovisk/MapWizard";
            var uri = new Uri(githubUrl);

            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = uri.ToString(),
                    UseShellExecute = true
                });
            }
            else
            {
                ToastManager.CreateToast()
                    .OfType(NotificationType.Error)
                    .WithTitle("Invalid URL")
                    .WithContent("The URL is not valid.")
                    .Dismiss().ByClicking()
                    .Dismiss().After(TimeSpan.FromSeconds(8))
                    .Queue();
            }
        }

        private void OnSettingsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MapWizard.Desktop.ViewModels.SettingsViewModel.IsHitSoundVisualizerEnabled))
            {
                return;
            }

            if (sender is SettingsViewModel settingsViewModel)
            {
                UpdateHitSoundVisualizerAvailability(settingsViewModel.IsHitSoundVisualizerEnabled);
            }
        }

        private void UpdateHitSoundVisualizerAvailability(bool isEnabled)
        {
            IsHitSoundVisualizerEnabled = isEnabled;

            if (!isEnabled && CurrentPageViewModel == HitSoundVisualizerViewModel)
            {
                SetPage(NavigationPage.Welcome);
            }
        }
    }
}
