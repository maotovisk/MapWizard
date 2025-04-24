using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Velopack;

namespace MapWizard.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public ISukiToastManager ToastManager { get; }
        public ISukiDialogManager DialogManager { get; }

        private UpdateManager _updateManager;
    
        private readonly ViewModelBase[] _views;
        
        public ViewModelBase HitSoundCopierViewModel { get; }
        public ViewModelBase MetadataManagerViewModel { get; }
        public ViewModelBase WelcomePageViewModel { get; }

        public MainWindowViewModel(
            WelcomePageViewModel wpVm,
            HitSoundCopierViewModel hsVm,
            MetadataManagerViewModel mmVm,
            UpdateManager updateManager,
            ISukiToastManager toastManager)
        {
            _updateManager = updateManager;
            ToastManager = toastManager;
            DialogManager = new SukiDialogManager();
            HitSoundCopierViewModel = hsVm;
            MetadataManagerViewModel = mmVm;
            WelcomePageViewModel = wpVm;

            Task.Run(CheckForUpdates);
        }

      
        private async Task CheckForUpdates()
        {

            if (!_updateManager.IsInstalled)
                return; // not an installed app

            ToastManager.CreateToast().OfType(NotificationType.Information)
                .WithLoadingState(true)
                .WithTitle("Updates")
                .WithContent("Checking for updates...")
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
                .WithActionButton("Update", _ => ShowUpdateToastWithProgress(newVersion), true)
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
    }
}

