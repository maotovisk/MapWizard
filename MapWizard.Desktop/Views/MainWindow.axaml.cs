using Avalonia.Interactivity;
using Avalonia.Input;
using MapWizard.Desktop.Controls;
using MapWizard.Desktop.Services;
using MapWizard.Desktop.ViewModels;
using SukiUI.Enums;

namespace MapWizard.Desktop.Views
{
    public partial class MainWindow : MapWizardWindow
    {
        // Ultra-dark pitch-black backdrop with only a breath of gradient,
        // applied on top of (and taking priority over) the Suki background style.
        private const string AmoledBackgroundShaderCode = """
            vec4 main(vec2 fragCoord) {
                vec2 uv = fragCoord / iResolution.xy;
                vec3 col = mix(vec3(0.0), vec3(0.02), uv.y);
                vec2 centered = (uv - vec2(0.5, 0.0)) * vec2(1.3, 1.0);
                float halo = (1.0 - smoothstep(0.05, 0.8, length(centered))) * 0.018;
                col += iPrimary * halo;
                return vec4(col, iAlpha);
            }
            """;

        private readonly IThemeService _themeService;
        private readonly IModalService _modalService;

        public MainWindow(MainWindowViewModel viewModel, IThemeService themeService, IModalService modalService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _themeService = themeService;
            _modalService = modalService;
            modalService.RegisterHost(ModalHost);
            _themeService.DarkThemeChanged += OnDarkThemeChanged;
            UpdateBackgroundStyle(_themeService.IsDarkTheme);
            AddHandler(KeyDownEvent, OnWindowKeyDownTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
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
            BackgroundShaderCode = isDarkTheme ? AmoledBackgroundShaderCode : null;
        }

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
