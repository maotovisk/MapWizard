using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Material.Dialog;

namespace MapWizard.Views;

public partial class MainWindow : Window
{
    public UserControl HitSoundCopierView { get; } = new UserControl();

    public MainWindow()
    {
        InitializeComponent();
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}