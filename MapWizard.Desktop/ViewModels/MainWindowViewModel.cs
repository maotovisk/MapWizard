using System;
using Avalonia.Controls.Notifications;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Velopack;

namespace MapWizard.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public ISukiToastManager ToastManager { get; }
        public ISukiDialogManager DialogManager { get; }

        private readonly UpdateManager _updateManager;
        
        public ViewModelBase HitSoundCopierViewModel { get; }
        public ViewModelBase MetadataManagerViewModel { get; }
        public ViewModelBase WelcomePageViewModel { get; }

        [ObservableProperty]
        private bool _isDarkTheme;
        
        [ObservableProperty]
        private string _version = "MapWizard-localdev";

        [ObservableProperty] private MaterialIconKind themeToggleIcon;
        
        partial void OnIsDarkThemeChanged(bool value)
        {
            SukiTheme.GetInstance().ChangeBaseTheme(value ? ThemeVariant.Dark : ThemeVariant.Light);
            
            ThemeToggleIcon = value ? MaterialIconKind.WeatherNight : MaterialIconKind.WhiteBalanceSunny;
        }
        
        public MainWindowViewModel(
            WelcomePageViewModel welcomePageViewModel,
            HitSoundCopierViewModel hitSoundCopierViewModel,
            MetadataManagerViewModel metadataManagerViewModel,
            UpdateManager updateManager,
            ISukiToastManager toastManager,
            ISukiDialogManager dialogManager)
        {
            _updateManager = updateManager;
            ToastManager = toastManager;
            DialogManager = dialogManager;
            HitSoundCopierViewModel = hitSoundCopierViewModel;
            MetadataManagerViewModel = metadataManagerViewModel;
            WelcomePageViewModel = welcomePageViewModel;
            
            // Set the version from the UpdateManager
            if (updateManager.IsInstalled)
            {
                Version = updateManager.CurrentVersion?.ToFullString() ?? "MapWizard-localdev";
            }
            else
            {
                Version = "MapWizard-localdev";
            }
            
            if (SukiTheme.GetInstance().ActiveBaseTheme == ThemeVariant.Dark)
            {
                IsDarkTheme = true;
                ThemeToggleIcon = MaterialIconKind.WeatherNight;
            }
            else
            {
                IsDarkTheme = false;
                ThemeToggleIcon = MaterialIconKind.WhiteBalanceSunny;
            }
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
                    .Dismiss().After(TimeSpan.FromSeconds(8))
                    .Queue();
            }
        }

        [RelayCommand]
        private void OpenHitsoundCopier()
        {
        }
    }
}

