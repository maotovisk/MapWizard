using Avalonia.Interactivity;
using Avalonia.Media;
using MapWizard.Desktop.ViewModels;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Enums;
using SukiUI.Models;

namespace MapWizard.Desktop.Views
{
    public partial class MainWindow : SukiWindow
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            BackgroundStyle = SukiBackgroundStyle.GradientDarker;
            
            var mapWizardTheme = new SukiColorTheme("MapWizard", Colors.DarkSlateBlue, Colors.OrangeRed);
            SukiTheme.GetInstance().AddColorTheme(mapWizardTheme);
            SukiTheme.GetInstance().ChangeColorTheme(mapWizardTheme);
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
        }
    }
}
