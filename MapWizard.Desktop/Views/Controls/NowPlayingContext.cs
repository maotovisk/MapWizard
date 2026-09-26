using Avalonia;
using Avalonia.Controls;
using MapWizard.Desktop.Services.MemoryService;

namespace MapWizard.Desktop.Views.Controls;

/// <summary>
/// Inherited attached property carrying the <see cref="OsuNowPlayingMonitor"/> down the visual tree,
/// so controls such as <see cref="BeatmapSelectionPanel"/> can show the open beatmap without every
/// page view-model having to forward it. Set once on the main window.
/// </summary>
public sealed class NowPlayingContext
{
    private NowPlayingContext()
    {
    }

    public static readonly AttachedProperty<OsuNowPlayingMonitor?> MonitorProperty =
        AvaloniaProperty.RegisterAttached<NowPlayingContext, Control, OsuNowPlayingMonitor?>(
            "Monitor",
            inherits: true);

    public static OsuNowPlayingMonitor? GetMonitor(Control control) => control.GetValue(MonitorProperty);

    public static void SetMonitor(Control control, OsuNowPlayingMonitor? value) => control.SetValue(MonitorProperty, value);
}
