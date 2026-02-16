using System;
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
        public ISukiToastManager ToastManager { get; }
        public ISukiDialogManager DialogManager { get; }

        private readonly IThemeService _themeService;
        private bool _isUpdatingFromThemeService;

        public ViewModelBase HitSoundCopierViewModel { get; }
        public ViewModelBase MetadataManagerViewModel { get; }
        public ViewModelBase ComboColourStudioViewModel { get; }
        public ViewModelBase WelcomePageViewModel { get; }
        public ViewModelBase SettingsViewModel { get; }

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
        private bool _isMetadataManagerSelected;

        [ObservableProperty]
        private bool _isComboColourStudioSelected;

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
            MetadataManagerViewModel metadataManagerViewModel,
            ComboColourStudioViewModel comboColourStudioViewModel,
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
            MetadataManagerViewModel = metadataManagerViewModel;
            ComboColourStudioViewModel = comboColourStudioViewModel;
            WelcomePageViewModel = welcomePageViewModel;
            SettingsViewModel = settingsViewModel;
            CurrentPageViewModel = WelcomePageViewModel;

            Version = updateService.VersionLabel;

            _themeService.DarkThemeChanged += OnDarkThemeChanged;
            UpdateThemeState(_themeService.IsDarkTheme);

            SetPage(NavigationPage.Welcome);
            settingsViewModel.Initialize();
            _ = welcomePageViewModel.CheckForUpdatesOnStartupAsync();
        }

        public void NavigateToWelcome() => SetPage(NavigationPage.Welcome);

        public void NavigateToHitSoundCopier() => SetPage(NavigationPage.HitSoundCopier);

        public void NavigateToMetadataManager() => SetPage(NavigationPage.MetadataManager);

        public void NavigateToComboColourStudio() => SetPage(NavigationPage.ComboColourStudio);

        public void NavigateToSettings() => SetPage(NavigationPage.Settings);

        private void SetPage(NavigationPage page)
        {
            CurrentPageViewModel = page switch
            {
                NavigationPage.Welcome => WelcomePageViewModel,
                NavigationPage.HitSoundCopier => HitSoundCopierViewModel,
                NavigationPage.MetadataManager => MetadataManagerViewModel,
                NavigationPage.ComboColourStudio => ComboColourStudioViewModel,
                NavigationPage.Settings => SettingsViewModel,
                _ => WelcomePageViewModel
            };

            IsWelcomeSelected = page == NavigationPage.Welcome;
            IsHitSoundCopierSelected = page == NavigationPage.HitSoundCopier;
            IsMetadataManagerSelected = page == NavigationPage.MetadataManager;
            IsComboColourStudioSelected = page == NavigationPage.ComboColourStudio;
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
    }
}
