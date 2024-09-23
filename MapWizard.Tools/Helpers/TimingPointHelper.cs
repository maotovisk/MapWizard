using MapWizard.BeatmapParser;

namespace MapWizard.Tools.Helpers;

public static class TimingPointHelper
{
    /// <summary>
    /// Removes redundant timing points from the beatmap.
    /// </summary>
    /// <param name="timingPointsSection"></param>
    /// <returns></returns>
    public static List<TimingPoint> RemoveRedundantGreenLines(TimingPoints timingPointsSection)
    {
        var inheritedTimingPoints = timingPointsSection.TimingPointList.Where(x => x is InheritedTimingPoint).OrderBy(x => x.Time.TotalMilliseconds).ToList();
        
        var clearedTimingPoints = timingPointsSection.TimingPointList.ToList();

        foreach (var inheritedTimingPoint in inheritedTimingPoints)
        {
            var inheritedTimingPointIndex = clearedTimingPoints.IndexOf(inheritedTimingPoint);
            
            if (inheritedTimingPointIndex == 0)
            {
                continue;
            }
            
            var previousTimingPoint = inheritedTimingPoints[inheritedTimingPointIndex - 1];


            if (inheritedTimingPoint.Effects == previousTimingPoint.Effects &&
                inheritedTimingPoint.SampleIndex == previousTimingPoint.SampleIndex &&
                inheritedTimingPoint.SampleSet == previousTimingPoint.SampleSet &&
                inheritedTimingPoint.Volume == previousTimingPoint.Volume)
            {
                clearedTimingPoints = clearedTimingPoints.Where(x=> x != inheritedTimingPoint).ToList();
            }
        }
        
        return clearedTimingPoints;
    }
}