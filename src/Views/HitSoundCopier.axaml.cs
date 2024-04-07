using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Material.Dialog;

namespace MapWizard.Views;

public partial class HitSoundCopier : Window
{
    public HitSoundCopier()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}