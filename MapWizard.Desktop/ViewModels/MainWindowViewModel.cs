using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using SukiUI;
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
            ISukiToastManager toastManager)
        {
            _updateManager = updateManager;
            ToastManager = toastManager;
            DialogManager = new SukiDialogManager();
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

            Task.Run(CheckForUpdates);
        }

        private async Task CheckForUpdates()
        {
            if (!_updateManager.IsInstalled)
                return;

            ToastManager.CreateToast().OfType(NotificationType.Information)
                .WithLoadingState(true)
                .WithTitle("Updates")
                .WithContent("Checking for updates...")
                .Dismiss().After(TimeSpan.FromSeconds(8))
                .Queue();

            var newVersion = await _updateManager.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                return;
            }
            
            ToastManager.CreateToast().OfType(NotificationType.Information)
                .WithLoadingState(false)
                .WithTitle("Updates")
                .WithContent($"New version {newVersion.BaseRelease?.Version} is available.")
                .WithActionButtonNormal("Later", _ => { }, true)
                .WithActionButton("Update", _ => ShowUpdateToastWithProgress(newVersion).Wait(), true)
                .Queue();
        }

        private async Task ShowUpdateToastWithProgress(UpdateInfo info)
        {
            var progress = new ProgressBar() { Value = 0, ShowProgressText = true };
            var toast = ToastManager.CreateToast()
                .WithTitle("Downloading Update...")
                .WithContent(progress)
                .Queue();

            await _updateManager.DownloadUpdatesAsync(info, x =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    progress.Value = x;
                    if (progress.Value < 100) return;
                    ToastManager.Dismiss(toast);
                });
            });
            
            ToastManager.CreateToast()
                .OfType(NotificationType.Success)
                .WithTitle("Update Downloaded")
                .WithContent("The update has been downloaded. Please restart the app to apply the update.")
                .WithActionButton("Next Restart", _ => { _updateManager.WaitExitThenApplyUpdates(info); }, true)
                .WithActionButtonNormal("Restart Now", _ =>
                {
                    _updateManager.ApplyUpdatesAndRestart(info);
                }, true)
                .Queue();
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
    }
}

