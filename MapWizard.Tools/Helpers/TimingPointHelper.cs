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
        
        var timingPointsToRemove = new List<TimingPoint>();

        foreach (var inheritedTimingPoint in inheritedTimingPoints)
        {
            
            var inheritedTimingPointIndex = timingPointsSection.TimingPointList.IndexOf(inheritedTimingPoint);
            
            if (inheritedTimingPointIndex == 0)
            {
                continue;
            }
            
            var previousTimingPoint = timingPointsSection.TimingPointList[inheritedTimingPointIndex - 1];
            
            if (previousTimingPoint is UninheritedTimingPoint or null)
            {
                continue;
            }

            previousTimingPoint = previousTimingPoint as InheritedTimingPoint;

            if (previousTimingPoint != null &&
                previousTimingPoint.SampleSet == inheritedTimingPoint.SampleSet &&
                previousTimingPoint.SampleIndex == inheritedTimingPoint.SampleIndex &&
                previousTimingPoint.Volume == inheritedTimingPoint.Volume &&
                Math.Abs(((InheritedTimingPoint)previousTimingPoint).SliderVelocity - ((InheritedTimingPoint)inheritedTimingPoint).SliderVelocity) < 0.0005 &&
                ((InheritedTimingPoint)previousTimingPoint).Effects == ((InheritedTimingPoint)inheritedTimingPoint).Effects)
            {   
                timingPointsToRemove.Add(inheritedTimingPoint);
            }
        }
        return timingPointsSection.TimingPointList.Except(timingPointsToRemove).ToList();
    }
}