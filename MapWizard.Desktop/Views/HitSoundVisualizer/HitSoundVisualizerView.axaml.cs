using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views.HitSoundVisualizer;

public partial class HitSoundVisualizerView : UserControl
{
    private const int SpacePlaybackToggleDebounceMs = 130;
    private long _lastSpacePlaybackToggleTickMs = -SpacePlaybackToggleDebounceMs;
    private HitSoundVisualizerViewModel? _boundViewModel;

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
        DetachedFromVisualTree += (_, _) => BindViewModel(null);
        AddHandler(KeyDownEvent, Root_OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        BindViewModel(DataContext as HitSoundVisualizerViewModel);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        BindViewModel(DataContext as HitSoundVisualizerViewModel);
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

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Z)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                vm.RedoCommand.Execute(null);
            }
            else
            {
                vm.UndoCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Y)
        {
            vm.RedoCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete)
        {
            if (vm.IsSampleRowContextActive)
            {
                vm.RemoveContextSamplePointCommand.Execute(null);
            }
            else
            {
                vm.RemoveSelectedPointCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

        if (TryHandleBankShortcuts(e, vm))
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

        if (e.KeyModifiers == KeyModifiers.None)
        {
            if (e.Key == Key.W)
            {
                vm.AddSelectionHitSoundCommand.Execute("Whistle");
                e.Handled = true;
                return;
            }

            if (e.Key == Key.E)
            {
                vm.AddSelectionHitSoundCommand.Execute("Finish");
                e.Handled = true;
                return;
            }

            if (e.Key == Key.R)
            {
                vm.AddSelectionHitSoundCommand.Execute("Clap");
                e.Handled = true;
                return;
            }
        }

        if (e.Key != Key.Space)
        {
            return;
        }

        var now = Environment.TickCount64;
        if (now - _lastSpacePlaybackToggleTickMs < SpacePlaybackToggleDebounceMs)
        {
            e.Handled = true;
            return;
        }

        _lastSpacePlaybackToggleTickMs = now;
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

    private bool TryHandleBankShortcuts(KeyEventArgs e, HitSoundVisualizerViewModel vm)
    {
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            var additionBank = e.Key switch
            {
                Key.Q => "Auto",
                Key.W => "Normal",
                Key.E => "Soft",
                Key.R => "Drum",
                _ => null
            };

            if (additionBank is not null)
            {
                vm.ApplyAdditionShortcutCommand.Execute(additionBank);
                e.Handled = true;
                return true;
            }
        }

        if (e.KeyModifiers == KeyModifiers.Shift)
        {
            var sampleSetBank = e.Key switch
            {
                Key.Q => "Auto",
                Key.W => "Normal",
                Key.E => "Soft",
                Key.R => "Drum",
                _ => null
            };

            if (sampleSetBank is not null)
            {
                vm.ApplySampleSetShortcutCommand.Execute(sampleSetBank);
                e.Handled = true;
                return true;
            }
        }

        return false;
    }

    private void BindViewModel(HitSoundVisualizerViewModel? vm)
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.FocusPlaybackRequested -= OnFocusPlaybackRequested;
            _boundViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _boundViewModel = vm;
        if (_boundViewModel is null)
        {
            return;
        }

        _boundViewModel.FocusPlaybackRequested += OnFocusPlaybackRequested;
        _boundViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not HitSoundVisualizerViewModel vm)
        {
            return;
        }

        if (e.PropertyName == nameof(HitSoundVisualizerViewModel.IsPreparingPlayback) &&
            !vm.IsPreparingPlayback &&
            vm.HasLoadedMap)
        {
            FocusVisualizer();
        }
    }

    private void OnFocusPlaybackRequested()
    {
        FocusVisualizer();
    }

    private void FocusVisualizer()
    {
        Dispatcher.UIThread.Post(() =>
        {
            Focus();
        }, DispatcherPriority.Input);
    }
}
