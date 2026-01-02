using BeatmapParser;
using BeatmapParser.Enums;
using BeatmapParser.HitObjects;
using BeatmapParser.HitObjects.HitSounds;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.HitSounds.Copier;
using MapWizard.Tools.HitSounds.Timeline;

namespace MapWizard.Tools.HitSounds.Extensions;

public static class HitSoundCopierExtensions
{
    extension(Beatmap origin)
    {
        public HitSoundTimeline BuildTimeline()
        {
            return origin.GeneralSection.Mode switch
            {
                Ruleset.Osu => HitSoundTimeline.BuildStandardTimeline(origin),
                Ruleset.Mania => HitSoundTimeline.BuildManiaSoundTimelines(origin),
                _ => new HitSoundTimeline()
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
                                    slider.RepeatSounds = [..new (HitSample, List<BeatmapParser.Enums.HitSound>)[slider.Slides - 1]];
                                    
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
        /// </summary>
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
    
    public static string ConvertToSampleName(this (SampleSet sampleSet, HitSound sound, int index) sampleInfo)
    {
        var (sampleset, sound, index) = sampleInfo;
        var baseName = sampleset switch
        {
            SampleSet.Normal => "normal",
            SampleSet.Soft => "soft",
            SampleSet.Drum => "drum",
            _ => "normal"
        };

        var suffix = sound switch
        {
            HitSound.Whistle => "-hitwhistle",
            HitSound.Finish => "-hitfinish",
            HitSound.Clap => "-hitclap",
            _ => "-hitnormal",
        };
        
        var indexSuffix = index > 1 ? $"{index}" : "";

        return $"{baseName}{suffix}{indexSuffix}";
    }
}