using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Material.Dialog;

namespace MapWizard.Views;

public partial class HitSoundCopierView : Window
{
    public HitSoundCopierView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}