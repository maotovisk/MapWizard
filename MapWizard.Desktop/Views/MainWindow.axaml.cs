using System;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using MapWizard.Desktop.ViewModels;
using MsBox.Avalonia;
using Velopack;
using Velopack.Sources;
using SukiUI.Controls;
using SukiUI.Toasts;

namespace MapWizard.Desktop.Views
{
    public partial class MainWindow : SukiWindow
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
        }
    }
}
