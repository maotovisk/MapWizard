using System.Linq;
using BeatmapParser;
using BeatmapParser.Events;

namespace MapWizard.Desktop.Utils;

public static class BeatmapBackgroundUtils
{
    public static string? GetBgFilename(this Beatmap beatmap)
    {
        return beatmap.Events.EventList.OfType<Background>().Any() ? beatmap.GetBackgroundFilename() : null;
    }
}