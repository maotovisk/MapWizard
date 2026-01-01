using BeatmapParser;
using BeatmapParser.HitObjects;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.HitSoundCopier.Event;

namespace MapWizard.Tools.HitSoundCopier.Timeline;

public class HitsoundTimeline
{
    public SoundTimeline DraggableSoundTimeline { get; set; } = new();
    public SoundTimeline NonDraggableSoundTimeline { get; set; } = new();
    public SampleSetTimeline SampleSetTimeline { get; set; } = new();
    
    public static HitsoundTimeline BuildManiaSoundTimelines(Beatmap origin)
    {
        //TODO(maot): Implement Mania Hitsounds
        return new HitsoundTimeline();
    }

    public static HitsoundTimeline BuildStandardTimeline(Beatmap origin)
    {
        if (origin.TimingPoints == null) return new HitsoundTimeline();
        
        var sampleSetTimeline = new SampleSetTimeline();
        var hitSoundTimeLine = new SoundTimeline(); 
        var sliderBodyTimeline = new SoundTimeline();

        foreach (var timingPoint in origin.TimingPoints.TimingPointList)
        {
            var currentSampleSet = sampleSetTimeline.GetSampleAtExactTime(timingPoint.Time.TotalMilliseconds);

            if (currentSampleSet == null)
            {
                currentSampleSet = new SampleSetEvent(timingPoint.Time.TotalMilliseconds, timingPoint.SampleSet, (int)timingPoint.SampleIndex, timingPoint.Volume);
                sampleSetTimeline.HitSamples.Add(currentSampleSet);
            }
            else
            {
                if (timingPoint is not InheritedTimingPoint) continue;
                
                currentSampleSet.Sample = timingPoint.SampleSet;
                currentSampleSet.Index = (int)timingPoint.SampleIndex;
                currentSampleSet.Volume = timingPoint.Volume;
            }
        }
        
        foreach (var hitObject in origin.HitObjects.Objects)
        {
            switch (hitObject)
            {
                case Circle circle:
                {
                    var currentSound = new SoundEvent(circle.Time, circle.HitSounds.Sounds, circle.HitSounds.SampleData.NormalSet, circle.HitSounds.SampleData.AdditionSet);
                    hitSoundTimeLine.SoundEvents.Add(currentSound);
                    break;
                }
                case Slider slider:
                {
                    var currentBodySound = new SoundEvent(slider.Time, slider.HitSounds.Sounds, slider.HitSounds.SampleData.NormalSet, slider.HitSounds.SampleData.AdditionSet);
                    sliderBodyTimeline.SoundEvents.Add(currentBodySound);

                    var currentHeadSound = new SoundEvent(slider.Time, slider.HeadSounds.Sounds, slider.HeadSounds.SampleData.NormalSet, slider.HeadSounds.SampleData.AdditionSet);
                    hitSoundTimeLine.SoundEvents.Add(currentHeadSound);

                    // Update the repeats sounds
                    if (slider is { Slides: > 1, RepeatSounds: not null } && slider.RepeatSounds.Count == slider.Slides - 1)
                    {
                        // the parser interprets the tail as an extra repeat, so we need to loop to one less repeat
                        for (var i = 0; i < slider.Slides - 1; i++)
                        {
                            var repeatSound = slider.RepeatSounds[i];
                            var repeatSoundTime = TimeSpan.FromMilliseconds(
                                Math.Round(slider.Time.TotalMilliseconds + ((slider.EndTime.TotalMilliseconds - slider.Time.TotalMilliseconds) / slider.Slides) * (i + 1)
                                )
                            );
                            
                            var currentRepeatSound = new SoundEvent(repeatSoundTime, repeatSound.Sounds, repeatSound.SampleData.NormalSet, repeatSound.SampleData.AdditionSet);
                            hitSoundTimeLine.SoundEvents.Add(currentRepeatSound);
                        }
                    }

                    // Update the tail sound
                    var currentEndSound = new SoundEvent(slider.EndTime, slider.TailSounds.Sounds, slider.TailSounds.SampleData.NormalSet, slider.TailSounds.SampleData.AdditionSet);
                    hitSoundTimeLine.SoundEvents.Add(currentEndSound);
                    break;
                }
                case Spinner spinner:
                {
                    var currentSound = new SoundEvent(spinner.End, spinner.HitSounds.Sounds, spinner.HitSounds.SampleData.NormalSet, spinner.HitSounds.SampleData.AdditionSet);
                    hitSoundTimeLine.SoundEvents.Add(currentSound);
                    break;
                }
            }
        }

        return new HitsoundTimeline
        {
            DraggableSoundTimeline = sliderBodyTimeline,
            NonDraggableSoundTimeline = hitSoundTimeLine,
            SampleSetTimeline = sampleSetTimeline
        };
    } 
}