using System.Linq;
using System.Threading.Tasks;
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
        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is HitSoundVisualizerViewModel vm)
            {
                vm.RefreshPersistedPlaybackVolumes();
            }
        };
        AddHandler(KeyDownEvent, Root_OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private async void Root_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not HitSoundVisualizerViewModel vm)
        {
            return;
        }

        if (HasTextEntryLikeFocus())
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.C)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is not null && vm.TryBuildPointClipboardPayload(out var payload))
            {
                await topLevel.Clipboard.SetTextAsync(payload);
                e.Handled = true;
            }

            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.V)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard is not null)
            {
                var text = await topLevel.Clipboard.GetTextAsync();
                vm.PastePointClipboardPayload(text);
                e.Handled = true;
            }

            return;
        }

        if (e.Key != Key.Space)
        {
            return;
        }

        vm.TogglePlaybackCommand.Execute(null);
        e.Handled = true;
    }

    private bool HasTextEntryLikeFocus()
    {
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focused is TextBox)
        {
            return true;
        }

        if (focused is Avalonia.Visual visual)
        {
            return visual.GetVisualAncestors().OfType<ComboBox>().Any() ||
                   visual.GetVisualAncestors().OfType<NumericUpDown>().Any();
        }

        return false;
    }

    private async void CopyHitsoundSelectionToClipboard_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not HitSoundVisualizerViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is null)
        {
            return;
        }

        if (!vm.TryBuildPointClipboardPayload(out var payload))
        {
            return;
        }

        await topLevel.Clipboard.SetTextAsync(payload);
        e.Handled = true;
    }
}
