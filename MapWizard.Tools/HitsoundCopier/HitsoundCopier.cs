using System.Text;
using MapWizard.BeatmapParser.Sections;
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

        ApplyNonDraggableHitSounds(target, hitSoundTimeline: hitSoundTimeLine);

        ApplyDraggableHitSounds(target, sliderBodyTimeline);


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

        ApplySampleTimeline(target, sampleSetTimeline);

        var output = target.Encode();

        // make sure it's encoding with Windows Line endings
        output = output.Replace("\r\n", "\n").Replace("\n", "\r\n");

        File.WriteAllText("output-copied.osu", output, Encoding.UTF8);
    }


    /// <summary>
    /// Applies a HitSound Timeline to the HitObjects section.
    /// </summary>
    /// <param name="hitSoundTimeline"></param>
    /// <param name="leniency"></param>
    private static void ApplyNonDraggableHitSounds(Beatmap origin, SoundTimeline hitSoundTimeline, int leniency = 2)
    {
        if (origin.TimingPoints == null) return;

        if (origin.HitObjects is not HitObjects) return;
        foreach (var hitObject in origin.HitObjects.Objects)
        {
            switch (hitObject)
            {
                case Circle circle:
                    {
                        var currentSound = hitSoundTimeline.GetSoundAtTime(circle.Time);
                        if (currentSound != null && (Math.Abs(circle.Time.TotalMilliseconds - currentSound.Time.TotalMilliseconds) <= leniency))
                        {
                            circle.HitSounds = (new HitSample(
                                normalSet: currentSound.NormalSample,
                                additionSet: currentSound.AdditionSample,
                                circle.HitSounds.SampleData.FileName
                            ), currentSound.HitSounds);
                        }
                        break;
                    }
                case Slider slider:
                    {
                        var currentHeadSound = hitSoundTimeline.GetSoundAtTime(slider.Time);

                        if (currentHeadSound != null && (Math.Abs(slider.Time.TotalMilliseconds - currentHeadSound.Time.TotalMilliseconds) <= leniency))
                        {
                            slider.HeadSounds = (new HitSample(
                                normalSet: currentHeadSound.NormalSample,
                                additionSet: currentHeadSound.AdditionSample,
                                slider.HeadSounds.SampleData.FileName
                            ), currentHeadSound.HitSounds);
                        }

                        // Update the repeats sounds
                        if (slider is { Repeats: > 1, RepeatSounds: not null } && slider.RepeatSounds.Count == (slider.Repeats - 1))
                        {
                            for (var i = 0; i < slider.Repeats - 1; i++)
                            {
                                var repeatSound = hitSoundTimeline.GetSoundAtTime(TimeSpan.FromMilliseconds(Math.Round(slider.Time.TotalMilliseconds + (slider.EndTime.TotalMilliseconds - slider.Time.TotalMilliseconds) / slider.Repeats * (i + 1))));

                                if (repeatSound != null)
                                {
                                    slider.RepeatSounds[i] = (new HitSample(
                                       repeatSound.NormalSample,
                                       repeatSound.AdditionSample,
                                       slider.RepeatSounds[i].SampleData.FileName
                                    ), repeatSound.HitSounds);
                                }
                            }
                        }
                        var currentEndSound = hitSoundTimeline.GetSoundAtTime(slider.EndTime);
                        if (currentEndSound != null && (Math.Abs(slider.EndTime.TotalMilliseconds - currentEndSound.Time.TotalMilliseconds) <= leniency))
                        {
                            slider.TailSounds = (new HitSample(
                                currentEndSound.NormalSample,
                                currentEndSound.AdditionSample,
                                slider.TailSounds.SampleData.FileName
                            ), currentEndSound.HitSounds);
                        }

                        break;
                    }
                case Spinner spinner:
                    {
                        var currentSound = hitSoundTimeline.GetSoundAtTime(spinner.End);
                        if (currentSound != null && (Math.Abs(spinner.End.TotalMilliseconds - currentSound.Time.TotalMilliseconds) <= leniency))
                        {
                            spinner.HitSounds = (new HitSample(
                               currentSound.NormalSample,
                               currentSound.AdditionSample,
                               spinner.HitSounds.SampleData.FileName
                            ), currentSound.HitSounds);
                        }
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Applies a hit sound to draggable hit objects (Sliders) at the HitObjects section.
    /// </summary>
    /// <param name="bodyTimeline"></param>
    /// <param name="leniency"></param>
    private static void ApplyDraggableHitSounds(Beatmap origin, SoundTimeline bodyTimeline, int leniency = 2)
    {
        if (origin.HitObjects is not HitObjects) return;
        foreach (var hitObject in origin.HitObjects.Objects)
        {
            if (hitObject is not Slider slider) continue;
            var currentBodySound = bodyTimeline.GetSoundAtTime(slider.Time);
            if (currentBodySound != null && (Math.Abs(slider.Time.TotalMilliseconds - currentBodySound.Time.TotalMilliseconds) <= leniency))
            {
                slider.HitSounds = (new HitSample(
                    currentBodySound.NormalSample,
                    currentBodySound.AdditionSample,
                    slider.TailSounds.SampleData.FileName
                ), currentBodySound.HitSounds);
            }
        }
    }

    /// <summary>
    /// Applies a SampleSetTimeline to the timing points
    /// </summary>
    /// <param name="timeline"></param>
    /// <param name="leniency"></param>
    private static void ApplySampleTimeline(Beatmap origin, SampleSetTimeline timeline, int leniency = 2)
    {
        switch (origin.TimingPoints)
        {
            case null:
                return;
            case TimingPoints section:
                {
                    foreach (var timingPoint in section.TimingPointList)
                    {
                        var sampleSet = timeline.GetSampleAtTime(timingPoint.Time.TotalMilliseconds);

                        if (sampleSet == null) continue;

                        timingPoint.SampleSet = sampleSet.Sample;
                        timingPoint.SampleIndex = (uint)sampleSet.Index;
                        timingPoint.Volume = (uint)sampleSet.Volume;
                    }

                    // Add the missing timing points 
                    foreach (var sound in timeline.HitSamples)
                    {
                        var currentUninherited = section.GetUninheritedTimingPointAt(sound.Time);
                        var currentInherited = section.GetInheritedTimingPointAt(sound.Time);

                        if (currentUninherited == null) continue;

                        if (currentInherited == null)
                        {
                            section.TimingPointList.Add(new InheritedTimingPoint(
                                time: TimeSpan.FromMilliseconds(sound.Time),
                                sampleSet: sound.Sample,
                                sampleIndex: (uint)sound.Index,
                                volume: (uint)sound.Volume,
                                effects: currentUninherited.Effects,
                                sliderVelocity: section.GetSliderVelocityAt(sound.Time)
                            ));
                        }
                    }
                    break;
                }
        }
    }

    /// <summary>
    /// Copy hitsounds from one beatmap to multiple others.
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    public static void CopyFromBeatmaps(string sourcePath, string[] targetPath)
    {
        foreach (var path in targetPath)
        {
            CopyFromBeatmap(sourcePath, path);
        }
    }
}
