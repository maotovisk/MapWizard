using System;
using Beatmap.Sections;

namespace Beatmap.Classes
{
    /// <summary>
    /// Represents a beatmap.
    /// </summary>
    public class Beatmap : IBeatmap
    {
        public FileInfo File { get; }
        public int Version { get; set; }
        public IMetadata Metadata { get; set; }
        public IGeneral General { get; set; }
        public IEditor Editor { get; set; }
        public IDifficulty Difficulty { get; set; }
        public IColours Colours { get; set; }
        public IEvents Events { get; set; }
        public ITimingPoints TimingPoints { get; set; }
        public IHitObjects HitObjects { get; set; }

        public Beatmap(FileInfo file)
        {
            File = file;
        }



    }
}