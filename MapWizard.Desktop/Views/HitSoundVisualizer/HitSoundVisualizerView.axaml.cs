using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views;

public partial class HitSoundVisualizerView : UserControl
{
    public HitSoundVisualizerView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, Root_OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void Root_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space || DataContext is not HitSoundVisualizerViewModel vm)
        {
            return;
        }

        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focused is TextBox)
        {
            return;
        }

        if (focused is Avalonia.Visual visual)
        {
            if (visual.GetVisualAncestors().OfType<ComboBox>().Any() ||
                visual.GetVisualAncestors().OfType<NumericUpDown>().Any())
            {
                return;
            }
        }

        vm.ToggleTransportPlaybackCommand.Execute(null);
        e.Handled = true;
    }
}
