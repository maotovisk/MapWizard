using System.IO;
using BeatmapParser;

namespace HitsoundCopier
{
    class Program
    {
        static void Main(FileInfo input, FileInfo output)
        {
            var beatmap = Beatmap.Decode(input);

            Console.WriteLine(beatmap.Version);

            Console.WriteLine("Parsing Beatmap...");
        }
    }
}