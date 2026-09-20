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
            ApplyPlatformWindowChrome();
            PropertyChanged += (_, args) =>
            {
                if (args.Property == WindowStateProperty)
                {
                    UpdateShellCorners();
                    UpdateWindowControlGlyphs();
                }
            };
            UpdateShellCorners();
            UpdateWindowControlGlyphs();
            AddHandler(KeyDownEvent, OnWindowKeyDownTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

        /// <summary>Exposes the view-model to startup plumbing in App.axaml.cs.</summary>
        internal MainWindowViewModel GetViewModel() => ViewModel;

        /// <summary>
        /// Panel hosting the page content: used as the opacity-0 warm-up host
        /// during page preloading, so warm pages measure with the exact live
        /// constraints they will get on the first navigation.
        /// </summary>
        internal Panel GetPreloadWarmupHost() => ContentIslandHost;

        public void NavigateToStart() => ViewModel.NavigateToWelcome();

        public void NavigateToHitSoundCopier() => ViewModel.NavigateToHitSoundCopier();

        public void NavigateToHitSoundVisualizer() => ViewModel.NavigateToHitSoundVisualizer();

        public void NavigateToMetadataManager() => ViewModel.NavigateToMetadataManager();

        public void NavigateToComboColourStudio() => ViewModel.NavigateToComboColourStudio();

        public void NavigateToMapCleaner() => ViewModel.NavigateToMapCleaner();

        public void NavigateToSettings() => ViewModel.NavigateToSettings();

        /// <summary>
        /// On macOS the native traffic lights (left-aligned) are shown over the
        /// extended client area while the custom caption buttons are hidden.
        /// </summary>
        private void ApplyPlatformWindowChrome()
        {
            if (!MapWizardWindow.HasNativeWindowControls)
            {
                return;
            }

            WindowControlsPanel.IsVisible = false;
            // Shift the title island right so it clears the traffic lights.
            TitleIsland.Padding = new Thickness(64, 0);
        }

        private void UpdateShellCorners()
        {
            if (OperatingSystem.IsWindows())
            {
                WindowShell.CornerRadius = default;
                WindowShell.ClipToBounds = false;
                return;
            }

            var square = WindowState is WindowState.Maximized or WindowState.FullScreen;
            WindowShell.CornerRadius = square ? default : new CornerRadius(12d);
            WindowShell.ClipToBounds = true;
        }

        private void UpdateWindowControlGlyphs()
        {
            var maximized = WindowState is WindowState.Maximized or WindowState.FullScreen;
            MaximizeGlyph.IsVisible = !maximized;
            RestoreIcon.IsVisible = maximized;
            RestoreFront.IsVisible = maximized;
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
