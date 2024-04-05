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
            var source = Beatmap.Decode(sourcePath);
            var target = Beatmap.Decode(targetPath);

            if (source == null || target == null) return;

            SoundTimeline hitsoundTimeline = new();

            foreach (var hitObject in source.HitObjects.Objects)
            {
                if (hitObject is Circle circle)
                {
                    var currentSound = hitsoundTimeline.GetSoundAtTime(circle.Time);

                    if (currentSound == null)
                    {
                        currentSound = new SoundEvent(circle.Time, circle.HitSounds.Sounds, circle.HitSounds.SampleData.NormalSet, circle.HitSounds.SampleData.AdditionSet, source.GetVolumeAt(circle.Time.TotalMilliseconds));
                    }
                    else
                    {
                        currentSound.HitSounds.AddRange(circle.HitSounds.Sounds);
                        currentSound.HitSounds = currentSound.HitSounds.Distinct().ToList();
                    }

                    hitsoundTimeline.SoundEvents.Add(currentSound);
                }
                else if (hitObject is Slider slider)
                {

                }
            }


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