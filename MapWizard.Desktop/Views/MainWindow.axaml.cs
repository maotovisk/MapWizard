using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.ViewModels;
using MapWizard.Theme.Controls;

namespace MapWizard.Desktop.Views
{
    public partial class MainWindow : MapWizardWindow
    {
        private readonly IModalService _modalService;

        public MainWindow(
            MainWindowViewModel viewModel,
            IModalService modalService,
            INotificationService notificationService)
        {
            InitializeComponent();
            _modalService = modalService;
            modalService.RegisterHost(ModalHost);
            NotificationHost.NotificationService = notificationService;
            DataContext = viewModel;
            PropertyChanged += (_, args) =>
            {
                if (args.Property == WindowStateProperty)
                {
                    UpdateShellCorners();
                }
            };
            UpdateShellCorners();
            AddHandler(KeyDownEvent, OnWindowKeyDownTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

        public void NavigateToStart() => ViewModel.NavigateToWelcome();

        public void NavigateToHitSoundCopier() => ViewModel.NavigateToHitSoundCopier();

        public void NavigateToMetadataManager() => ViewModel.NavigateToMetadataManager();

        public void NavigateToComboColourStudio() => ViewModel.NavigateToComboColourStudio();

        public void NavigateToSettings() => ViewModel.NavigateToSettings();

        private void UpdateShellCorners()
        {
            var square = WindowState is WindowState.Maximized or WindowState.FullScreen;
            WindowShell.CornerRadius = square ? default : new CornerRadius(ShellCornerRadius);
        }

        private void MinimizeButton_OnClick(object? sender, RoutedEventArgs e) => MinimizeWindow();

        private void MaximizeButton_OnClick(object? sender, RoutedEventArgs e) => ToggleMaximizeWindow();

        private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => CloseWindow();

        private async void OnWindowKeyDownTunnel(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            if (await _modalService.CloseOnEscapeAsync())
            {
                e.Handled = true;
            }
        }
    }
}
