using System.Text;
using MapWizard.BeatmapParser;

namespace MapWizard.Tools.HitSoundCopier;
/// <summary>
/// Class to copy hit sounds from one beatmap to others.
/// </summary>
public class HitSoundCopier
{
/// <summary>
/// Copy hit sounds from one beatmap to another.
/// </summary>
/// <param name="sourcePath"></param>
/// <param name="targetPath"></param>
private static void CopyFromBeatmap(string sourcePath, string targetPath)
{
    var source = Beatmap.Decode(new FileInfo(sourcePath));
    var target = Beatmap.Decode(new FileInfo(targetPath));

    SoundTimeline hitSoundTimeLine = new();
    SoundTimeline sliderBodyTimeline = new();

    if (source.TimingPoints == null) return;

    foreach (var hitObject in source.HitObjects.Objects)
    {
        if (hitObject is Circle circle)
        {
            var currentSound = new SoundEvent(circle.Time, circle.HitSounds.Sounds, circle.HitSounds.SampleData.NormalSet, circle.HitSounds.SampleData.AdditionSet);
            hitSoundTimeLine.SoundEvents.Add(currentSound);
        }
        else if (hitObject is Slider slider)
        {
            var currentBodySound = new SoundEvent(slider.Time, slider.HitSounds.Sounds, slider.HitSounds.SampleData.NormalSet, slider.HitSounds.SampleData.AdditionSet);
            sliderBodyTimeline.SoundEvents.Add(currentBodySound);

            var currentHeadSound = new SoundEvent(slider.Time, slider.HeadSounds.Sounds, slider.HeadSounds.SampleData.NormalSet, slider.HeadSounds.SampleData.AdditionSet);
            hitSoundTimeLine.SoundEvents.Add(currentHeadSound);

            // Update the repeats sounds
            if (slider is { Repeats: > 1, RepeatSounds: not null } && slider.RepeatSounds.Count == (slider.Repeats - 1))
            {
                for (var i = 0; i < slider.Repeats - 1; i++)
                {
                    var repeatSound = slider.RepeatSounds[i];
                    var repeatSoundTime = TimeSpan.FromMilliseconds(Math.Round(slider.Time.TotalMilliseconds + (slider.EndTime.TotalMilliseconds - slider.Time.TotalMilliseconds) / slider.Repeats * (i + 1)));
                    var currentRepeatSound = new SoundEvent(repeatSoundTime, repeatSound.Sounds, repeatSound.SampleData.NormalSet, repeatSound.SampleData.AdditionSet);
                    hitSoundTimeLine.SoundEvents.Add(currentRepeatSound);

                }
            }

            // Update the tail sound
            var currentEndSound = new SoundEvent(slider.EndTime, slider.TailSounds.Sounds, slider.TailSounds.SampleData.NormalSet, slider.TailSounds.SampleData.AdditionSet);
            hitSoundTimeLine.SoundEvents.Add(currentEndSound);
        }
        else if (hitObject is Spinner spinner)
        {
            var currentSound = new SoundEvent(spinner.End, spinner.HitSounds.Sounds, spinner.HitSounds.SampleData.NormalSet, spinner.HitSounds.SampleData.AdditionSet);
            hitSoundTimeLine.SoundEvents.Add(currentSound);
        }
    }
    
    target.ApplyNonDraggableHitSounds(hitSoundTimeline: hitSoundTimeLine);

    target.ApplyDraggableHitSounds(sliderBodyTimeline);


    // Get sample set events
    var sampleSetTimeline = new SampleSetTimeline();

    foreach (var timingPoint in source.TimingPoints.TimingPointList)
    {
        var currentSampleSet = sampleSetTimeline.GetSampleAtTime(timingPoint.Time.TotalMilliseconds);

        if (currentSampleSet == null)
        {
            currentSampleSet = new SampleSetEvent(timingPoint.Time.TotalMilliseconds, timingPoint.SampleSet, (int)timingPoint.SampleIndex, timingPoint.Volume);
            sampleSetTimeline.HitSamples.Add(currentSampleSet);
        }
        else
        {
            if (timingPoint is InheritedTimingPoint)
            {
                currentSampleSet.Sample = timingPoint.SampleSet;
                currentSampleSet.Index = (int)timingPoint.SampleIndex;
                currentSampleSet.Volume = timingPoint.Volume;
            }
        }
    }

    target.ApplySampleTimeline(sampleSetTimeline);

    var output = target.Encode();

    // make sure it's encoding with Windows Line endings
    output = output.Replace("\r\n", "\n").Replace("\n", "\r\n");

    File.WriteAllText("output-copied.osu", output, Encoding.UTF8);
}

/// <summary>
/// Copy hitsounds from one beatmap to multiple others.
/// </summary>
/// <param name="sourcePath"></param>
/// <param name="targetPath"></param>
public static void CopyFromBeatmap(string sourcePath, string[] targetPath)
{
    foreach (var path in targetPath)
    {
        CopyFromBeatmap(sourcePath, path);
    }
}
}
