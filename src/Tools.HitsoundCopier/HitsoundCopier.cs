using System.Text;
using BeatmapParser;

namespace Tools.HitsoundCopier
{
    /// <summary>
    /// Class to copy hitsounds from one beatmap to others.
    /// </summary>
    public class HitsoundCopier
    {
        /// <summary>
        /// Copy hitsounds from one beatmap to another.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        public static void CopyFromBeatmap(string sourcePath, string targetPath)
        {
            var source = Beatmap.Decode(new FileInfo(sourcePath));
            var target = Beatmap.Decode(new FileInfo(targetPath));

            if (source == null || target == null) return;

            SoundTimeline hitsoundTimeline = new();
            SoundTimeline sliderBodyTimeline = new();

            if (source.TimingPoints == null) return;

            foreach (var hitObject in source.HitObjects.Objects)
            {
                if (hitObject is Circle circle)
                {
                    var currentSound = hitsoundTimeline.GetSoundAtTime(circle.Time);

                    if (currentSound == null)
                    {
                        currentSound = new SoundEvent(circle.Time, circle.HitSounds.Sounds, circle.HitSounds.SampleData.NormalSet, circle.HitSounds.SampleData.AdditionSet);
                        hitsoundTimeline.SoundEvents.Add(currentSound);
                    }
                    else
                    {
                        currentSound.HitSounds.AddRange(circle.HitSounds.Sounds);
                        currentSound.HitSounds = currentSound.HitSounds.Distinct().ToList();

                        hitsoundTimeline.SoundEvents[hitsoundTimeline.SoundEvents.IndexOf(currentSound)] = currentSound;
                    }

                }
                else if (hitObject is Slider slider)
                {
                    // This will go to the slider body timeline
                    var currentBodySound = sliderBodyTimeline.GetSoundAtTime(slider.Time);

                    if (currentBodySound == null)
                    {
                        currentBodySound = new SoundEvent(slider.Time, slider.HitSounds.Sounds, slider.HitSounds.SampleData.NormalSet, slider.HitSounds.SampleData.AdditionSet);

                        sliderBodyTimeline.SoundEvents.Add(currentBodySound);
                    }
                    else
                    {
                        currentBodySound.HitSounds.AddRange(slider.HitSounds.Sounds);
                        currentBodySound.HitSounds = currentBodySound.HitSounds.Distinct().ToList();
                        sliderBodyTimeline.SoundEvents[sliderBodyTimeline.SoundEvents.IndexOf(currentBodySound)] = currentBodySound;
                    }

                    // Now we go to the slider edges timeline, starting by the head.
                    var currentHeadSound = hitsoundTimeline.GetSoundAtTime(slider.Time);

                    if (currentHeadSound == null)
                    {
                        currentHeadSound = new SoundEvent(slider.Time, slider.HeadSounds.Sounds, slider.HeadSounds.SampleData.NormalSet, slider.HeadSounds.SampleData.AdditionSet);
                        hitsoundTimeline.SoundEvents.Add(currentHeadSound);
                    }
                    else
                    {
                        currentHeadSound.HitSounds.AddRange(slider.HeadSounds.Sounds);
                        currentHeadSound.HitSounds = currentHeadSound.HitSounds.Distinct().ToList();
                    }

                    // Now we go to the slider edges timeline, starting by the repeats, if any.
                    if (slider.Repeats > 1 && slider.RepeatSounds != null && slider.RepeatSounds.Count == (slider.Repeats - 1))
                    {
                        for (var i = 0; i < slider.Repeats - 1; i++)
                        {
                            var repeatSound = slider.RepeatSounds[i];
                            var repeatSoundTime = slider.Time + (((slider.EndTime - slider.Time) / slider.Repeats) * (i + 1));

                            var currentRepeatSound = hitsoundTimeline.GetSoundAtTime(repeatSoundTime);

                            if (currentRepeatSound == null)
                            {
                                currentRepeatSound = new SoundEvent(repeatSoundTime, repeatSound.Sounds, repeatSound.SampleData.NormalSet, repeatSound.SampleData.AdditionSet);
                                hitsoundTimeline.SoundEvents.Add(currentRepeatSound);
                            }
                            else
                            {
                                currentRepeatSound.HitSounds.AddRange(repeatSound.Sounds);
                                currentRepeatSound.HitSounds = currentRepeatSound.HitSounds.Distinct().ToList();
                            }
                        }
                    }

                    // Finally we go to the slider end
                    var currentEndSound = hitsoundTimeline.GetSoundAtTime(slider.EndTime);

                    if (currentEndSound == null)
                    {
                        currentEndSound = new SoundEvent(slider.EndTime, slider.TailSounds.Sounds, slider.TailSounds.SampleData.NormalSet, slider.TailSounds.SampleData.AdditionSet);
                        hitsoundTimeline.SoundEvents.Add(currentEndSound);
                    }
                    else
                    {
                        currentEndSound.HitSounds.AddRange(slider.TailSounds.Sounds);
                        currentEndSound.HitSounds = currentEndSound.HitSounds.Distinct().ToList();
                    }
                }
                if (hitObject is Spinner spinner)
                {
                    var currentSound = hitsoundTimeline.GetSoundAtTime(spinner.End);

                    if (currentSound == null)
                    {
                        currentSound = new SoundEvent(spinner.Time, spinner.HitSounds.Sounds, spinner.HitSounds.SampleData.NormalSet, spinner.HitSounds.SampleData.AdditionSet);
                        hitsoundTimeline.SoundEvents.Add(currentSound);
                    }
                    else
                    {
                        currentSound.HitSounds.AddRange(spinner.HitSounds.Sounds);
                        currentSound.HitSounds = currentSound.HitSounds.Distinct().ToList();
                    }
                }
            }

            foreach (var sound in hitsoundTimeline.SoundEvents)
            {
                target.ApplyNonDraggableHitsoundAt(sound.Time.TotalMilliseconds, sound);
            }

            foreach (var sound in sliderBodyTimeline.SoundEvents)
            {
                target.ApplyDraggableHitsoundAt(sound.Time.TotalMilliseconds, sound);
            }

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
}