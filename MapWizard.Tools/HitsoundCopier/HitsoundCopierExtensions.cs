using BeatmapParser;
using BeatmapParser.Enums;
using BeatmapParser.HitObjects;
using BeatmapParser.HitObjects.HitSounds;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.HitSoundCopier.Event;
using MapWizard.Tools.HitSoundCopier.Timeline;

namespace MapWizard.Tools.HitSoundCopier;

public static class HitsoundCopierExtensions
{
    extension(Beatmap origin)
    {
        public (SoundTimeline hitSoundTimeLine, SoundTimeline sliderBodyTimeline) BuildHitSoundTimelines()
        {
            return origin.GeneralSection.Mode switch
            {
                Ruleset.Mania => BuildManiaHitSoundTimelines(origin),
                _ => BuildStandardHitSoundTimelines(origin),
            };
        }

        public SampleSetTimeline BuildSampleSetTimeline()
        {
            return origin.GeneralSection.Mode switch
            {
                Ruleset.Mania => BuildManiaSampleSetTimeline(origin),
                _ => BuildStandardSampleSetTimeline(origin)
            };
        }
        
          /// <summary>
        /// Applies a HitSound Timeline to the HitObjects section.
        /// </summary>
        /// <param name="hitSoundTimeline"></param>
        /// <param name="options"></param>
        public void ApplyNonDraggableHitSounds(SoundTimeline hitSoundTimeline, HitSoundCopierOptions options)
        {
            if (origin.TimingPoints == null) return;
            
            var newHitObjects = new List<IHitObject>(origin.HitObjects.Objects.ToList());
            
            foreach (var hitObject in origin.HitObjects.Objects)
            {
                switch (hitObject)
                {
                    case Circle circle:
                        {
                            if (options.OverwriteEverything) 
                            {
                                circle.HitSounds = (new HitSample(), []);
                            }
                            
                            var currentSound = hitSoundTimeline.GetSoundAtTime(circle.Time, options.Leniency);
                            if (currentSound != null && (Math.Abs(circle.Time.TotalMilliseconds - currentSound.Time.TotalMilliseconds) <= options.Leniency))
                            {
                                circle.HitSounds = (new HitSample(
                                    normalSet: currentSound.NormalSample,
                                    additionSet: currentSound.AdditionSample,
                                    circle.HitSounds.SampleData.FileName
                                ), currentSound.HitSounds);
                            }
                            
                            newHitObjects[origin.HitObjects.Objects.IndexOf(hitObject)] = circle;
                            break;
                        }
                    case Slider slider:
                        {
                            var currentHeadSound = hitSoundTimeline.GetSoundAtTime(slider.Time, options.Leniency);
                            
                            if (options.OverwriteEverything)
                            {
                                slider.HeadSounds = (new HitSample(), []);
                                slider.TailSounds = (new HitSample(), []);
                            }

                            if (currentHeadSound != null)
                            {
                                slider.HeadSounds = (new HitSample(
                                    normalSet: currentHeadSound.NormalSample,
                                    additionSet: currentHeadSound.AdditionSample,
                                    slider.HeadSounds.SampleData.FileName
                                ), currentHeadSound.HitSounds);
                            }

                            // Update the repeats sounds
                            if (slider is { Slides: > 1 })
                            {
                                if (slider.RepeatSounds == null || slider.RepeatSounds.Count != slider.Slides - 1 || options.OverwriteEverything)
                                {
                                    slider.RepeatSounds = new List<(HitSample, List<HitSound>)>(new (HitSample, List<HitSound>)[slider.Slides - 1]);
                                    
                                    for (var i = 0; i < slider.Slides - 1; i++)
                                    {
                                        slider.RepeatSounds[i] = (new HitSample(), []);
                                    }
                                }
                                
                                for (var i = 0; i < slider.Slides - 1; i++)
                                {
                                    var repeatSoundTime = TimeSpan.FromMilliseconds(
                                        Math.Round(slider.Time.TotalMilliseconds + ((slider.EndTime.TotalMilliseconds - slider.Time.TotalMilliseconds) / slider.Slides) * (i + 1))
                                    );
                                    
                                    var repeatSound = hitSoundTimeline.GetSoundAtTime(repeatSoundTime, options.Leniency);

                                    if (repeatSound != null)
                                    {
                                        slider.RepeatSounds[i] = (new HitSample
                                        {
                                            AdditionSet = repeatSound.AdditionSample,
                                            NormalSet = repeatSound.NormalSample,
                                            FileName = slider.RepeatSounds[i].SampleData.FileName
                                        }, repeatSound.HitSounds);
                                    }
                                }
                            }
                            var currentEndSound = hitSoundTimeline.GetSoundAtTime(slider.EndTime, options.Leniency);
                            if (currentEndSound != null)
                            {
                                slider.TailSounds = (new HitSample(
                                    currentEndSound.NormalSample,
                                    currentEndSound.AdditionSample,
                                    slider.TailSounds.SampleData.FileName
                                ), currentEndSound.HitSounds);
                            }
                            
                            
                            newHitObjects[origin.HitObjects.Objects.IndexOf(hitObject)] = slider;
                            break;
                        }
                    case Spinner spinner:
                        {
                            if (options.OverwriteEverything)
                            {
                                spinner.HitSounds = (new HitSample(), []);
                            }
                            
                            var currentSound = hitSoundTimeline.GetSoundAtTime(spinner.End, options.Leniency);
                            if (currentSound != null && (Math.Abs(spinner.End.TotalMilliseconds - currentSound.Time.TotalMilliseconds) <= options.Leniency))
                            {
                                spinner.HitSounds = (new HitSample(
                                   currentSound.NormalSample,
                                   currentSound.AdditionSample,
                                   spinner.HitSounds.SampleData.FileName
                                ), currentSound.HitSounds);
                            }
                            
                            newHitObjects[origin.HitObjects.Objects.IndexOf(hitObject)] = spinner;
                            break;
                        }
                }
            }
            
            origin.HitObjects.Objects = newHitObjects;
        }

        /// <summary>
        /// Applies a hit sound to draggable hit objects (Sliders) at the HitObjects section.
        /// </summary>
        /// <param name="bodyTimeline"></param>
        /// <param name="options"></param>
        public void ApplyDraggableHitSounds(SoundTimeline bodyTimeline, HitSoundCopierOptions options)
        {
            var newHitObjects = new List<IHitObject>(origin.HitObjects.Objects.ToList());
            foreach (var hitObject in origin.HitObjects.Objects)
            {
                if (hitObject is not Slider slider) continue;
                
                if (options.OverwriteEverything)
                {
                    slider.HitSounds = (new HitSample(), []);
                }
                
                var currentBodySound = bodyTimeline.GetSoundAtTime(slider.Time, options.Leniency);
                if (currentBodySound != null && (Math.Abs(slider.Time.TotalMilliseconds - currentBodySound.Time.TotalMilliseconds) <= options.Leniency))
                {
                    slider.HitSounds = (new HitSample(
                        currentBodySound.NormalSample,
                        currentBodySound.AdditionSample,
                        slider.TailSounds.SampleData.FileName
                    ), currentBodySound.HitSounds);
                }
                
                newHitObjects[origin.HitObjects.Objects.IndexOf(hitObject)] = slider;
            }
            
            origin.HitObjects.Objects = newHitObjects;
        }

        /// <summary>
        /// Applies a SampleSetTimeline to the timing points
        /// </summary>\
        /// <param name="origin"></param>
        /// <param name="timeline"></param>
        /// <param name="options"></param>
        public void ApplySampleTimeline(SampleSetTimeline timeline, HitSoundCopierOptions options)
        {
            switch (origin.TimingPoints)
            {
                case null:
                    return;
                case var section:
                    foreach (var timingPoint in section.TimingPointList)
                    {
                        var sampleSet = timeline.GetCurrentSampleAtTime(timingPoint.Time.TotalMilliseconds, options.Leniency);

                        if (sampleSet == null) continue;
                        
                        timingPoint.SampleSet = sampleSet.Sample;
                        timingPoint.SampleIndex = (uint)sampleSet.Index;
                        timingPoint.Volume = (timingPoint.Volume == 5 && !options.OverwriteMuting) ? timingPoint.Volume : (uint)sampleSet.Volume;
                    }

                    // Add the missing inherited points from the timeline
                    foreach (var sampleSet in timeline.HitSamples)
                    {
                        if (section.TimingPointList.Any(x => Math.Abs(x.Time.TotalMilliseconds - sampleSet.Time) < options.Leniency)) continue;
                        
                        var currentTimingPoint = section.TimingPointList.OrderBy(x => x.Time.TotalMilliseconds).LastOrDefault(x => x.Time.TotalMilliseconds <= sampleSet.Time);
                        
                        if (currentTimingPoint == null) continue;
                        
                        // If the timing point is already using the same sample set, sample index and volume, skip it
                        if (currentTimingPoint.SampleSet == sampleSet.Sample && 
                            currentTimingPoint.SampleIndex == sampleSet.Index &&
                            !(Math.Abs(currentTimingPoint.Volume - sampleSet.Volume) > 0.5)) continue;

                        var newTimingPoint = currentTimingPoint switch
                        {
                            UninheritedTimingPoint uninheritedTimingPoint =>
                                new InheritedTimingPoint(
                                    time: TimeSpan.FromMilliseconds(sampleSet.Time),
                                    sampleSet: sampleSet.Sample,
                                    sliderVelocity: 1.0,
                                    sampleIndex: (uint)sampleSet.Index,
                                    volume: (uint)sampleSet.Volume,
                                    effects: uninheritedTimingPoint.Effects
                                ),
                            InheritedTimingPoint inheritedTimingPoint => new InheritedTimingPoint(
                                time: TimeSpan.FromMilliseconds(sampleSet.Time),
                                sampleSet: sampleSet.Sample,
                                sliderVelocity: inheritedTimingPoint.SliderVelocity,
                                sampleIndex: (uint)sampleSet.Index,
                                volume: (uint)sampleSet.Volume,
                                effects: inheritedTimingPoint.Effects
                            ),
                            _ => null
                        };
                        if (newTimingPoint == null) continue;

                        section.TimingPointList.Insert(section.TimingPointList.IndexOf(currentTimingPoint), newTimingPoint);
                    }

                    origin.TimingPoints = section;
                    break;
            }
        }
    }
    
    private static (SoundTimeline hitSoundTimeLine, SoundTimeline sliderBodyTimeline) BuildManiaHitSoundTimelines(Beatmap origin)
    {
        //TODO(maot): Implement Mania Hitsounds
        return (new SoundTimeline(), new SoundTimeline());
    }
    
    private static SampleSetTimeline BuildManiaSampleSetTimeline(Beatmap origin)
    {
        //TODO(maot): Implement Mania SampleSetTimeline
        return new SampleSetTimeline();
    }

    private static SampleSetTimeline BuildStandardSampleSetTimeline(Beatmap origin)
    {
        if (origin.TimingPoints == null) return new SampleSetTimeline();
        
        var sampleSetTimeline = new SampleSetTimeline();

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

        return sampleSetTimeline;
    }

    private static (SoundTimeline clickable, SoundTimeline draggable) BuildStandardHitSoundTimelines(Beatmap origin)
    {
        SoundTimeline hitSoundTimeLine = new();
        SoundTimeline sliderBodyTimeline = new();
        
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
        
        return (hitSoundTimeLine, sliderBodyTimeline);
    } 
    
}