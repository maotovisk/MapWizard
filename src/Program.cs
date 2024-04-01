using System.IO;
using BeatmapParser;

namespace HitsoundCopier
{
    class Program
    {
        static void Main(FileInfo input, FileInfo output)
        {
            var beatmap = Beatmap.Decode(input);

            Console.WriteLine("Parsing Beatmap...");
        }
    }
}