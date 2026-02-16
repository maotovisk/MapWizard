using Avalonia.Interactivity;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.ViewModels;
using SukiUI.Controls;
using SukiUI.Enums;

namespace MapWizard.Desktop.Views
{
public partial class MainWindow : SukiWindow
    {
        private readonly IThemeService _themeService;

        public MainWindow(MainWindowViewModel viewModel, IThemeService themeService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _themeService = themeService;
            _themeService.DarkThemeChanged += OnDarkThemeChanged;
            UpdateBackgroundStyle(_themeService.IsDarkTheme);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            _themeService.Initialize();
            UpdateBackgroundStyle(_themeService.IsDarkTheme);
        }

        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

        public void NavigateToStart() => ViewModel.NavigateToWelcome();

        public void NavigateToHitSoundCopier() => ViewModel.NavigateToHitSoundCopier();

        public void NavigateToMetadataManager() => ViewModel.NavigateToMetadataManager();

        public void NavigateToComboColourStudio() => ViewModel.NavigateToComboColourStudio();

        public void NavigateToSettings() => ViewModel.NavigateToSettings();

        private void OnDarkThemeChanged(object? sender, bool isDarkTheme)
        {
            UpdateBackgroundStyle(isDarkTheme);
        }

        private void UpdateBackgroundStyle(bool isDarkTheme)
        {
            BackgroundStyle = isDarkTheme ? SukiBackgroundStyle.GradientDarker : SukiBackgroundStyle.Gradient;
        }
    }
}
