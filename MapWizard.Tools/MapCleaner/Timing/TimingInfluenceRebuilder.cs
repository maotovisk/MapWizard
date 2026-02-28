using BeatmapParser;
using BeatmapParser.HitObjects;

namespace MapWizard.Tools.MapCleaner.Timing;

internal static class TimingInfluenceRebuilder
{
    public static IReadOnlyList<double> BuildRequiredTimes(
        Beatmap beatmap,
        bool includeSliderBodyTiming,
        bool includeSpinnerBodyTiming)
    {
        var requiredTimes = new HashSet<double>();

        foreach (var hitObject in beatmap.HitObjects.Objects)
        {
            requiredTimes.Add(hitObject.Time.TotalMilliseconds);

            switch (hitObject)
            {
                case Slider slider:
                    requiredTimes.Add(slider.EndTime.TotalMilliseconds);
                    break;
                case Spinner spinner:
                    requiredTimes.Add(spinner.End.TotalMilliseconds);
                    break;
                case ManiaHold maniaHold:
                    requiredTimes.Add(maniaHold.End.TotalMilliseconds);
                    break;
            }
        }

        if (beatmap.Editor?.Bookmarks != null)
        {
            foreach (var bookmark in beatmap.Editor.Bookmarks)
            {
                requiredTimes.Add(bookmark.TotalMilliseconds);
            }
        }

        if (beatmap.TimingPoints != null)
        {
            if (includeSliderBodyTiming)
            {
                foreach (var slider in beatmap.HitObjects.Objects.OfType<Slider>())
                {
                    var start = slider.Time.TotalMilliseconds;
                    var end = slider.EndTime.TotalMilliseconds;

                    foreach (var timingPoint in beatmap.TimingPoints.TimingPointList)
                    {
                        var tpTime = timingPoint.Time.TotalMilliseconds;
                        if (tpTime > start && tpTime < end)
                        {
                            requiredTimes.Add(tpTime);
                        }
                    }
                }
            }

            if (includeSpinnerBodyTiming)
            {
                foreach (var spinner in beatmap.HitObjects.Objects.OfType<Spinner>())
                {
                    var start = spinner.Time.TotalMilliseconds;
                    var end = spinner.End.TotalMilliseconds;

                    foreach (var timingPoint in beatmap.TimingPoints.TimingPointList)
                    {
                        var tpTime = timingPoint.Time.TotalMilliseconds;
                        if (tpTime > start && tpTime < end)
                        {
                            requiredTimes.Add(tpTime);
                        }
                    }
                }

                foreach (var hold in beatmap.HitObjects.Objects.OfType<ManiaHold>())
                {
                    var start = hold.Time.TotalMilliseconds;
                    var end = hold.End.TotalMilliseconds;

                    foreach (var timingPoint in beatmap.TimingPoints.TimingPointList)
                    {
                        var tpTime = timingPoint.Time.TotalMilliseconds;
                        if (tpTime > start && tpTime < end)
                        {
                            requiredTimes.Add(tpTime);
                        }
                    }
                }
            }
        }

        return requiredTimes.OrderBy(x => x).ToList();
    }
}
