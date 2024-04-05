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