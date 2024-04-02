using System.IO;
using BeatmapParser;

namespace HitsoundCopier
{
    class Program
    {
        static void Main(FileInfo input, FileInfo output)
        {
            DecodeBOMBA();

            //             // debug a specific beatmap if debug

            // #if DEBUG
            //             input = new FileInfo("test4.osu");
            // #endif
            //             Console.WriteLine($"Parsing Beatmap {input.Name} ...");
            //             var beatmap = Beatmap.Decode(input);
            //             Console.WriteLine($"Beatmap Parsed");

            //             Console.WriteLine(beatmap.Version);

            //             var circleCount = 0;
            //             var sliderCount = 0;
            //             var spinnerCount = 0;

            //             foreach (var obj in beatmap.HitObjects.Objects)
            //             {
            //                 if (obj.Type == HitObjectType.Circle) { ++circleCount; continue; }
            //                 if (obj.Type == HitObjectType.Slider) { ++sliderCount; continue; }
            //                 if (obj.Type == HitObjectType.Spinner) { ++spinnerCount; continue; }
            //             }

            //             var firstRedLine = (UninheritedTimingPoint?)beatmap.TimingPoints?.TimingPointList.First() ?? new UninheritedTimingPoint();
            //             var bpm = 60 / firstRedLine.BeatLength.TotalSeconds;
            //             Console.WriteLine($"this beatmap bpm is: {bpm}");
            //             Console.WriteLine($"this beatmap have:");
            //             Console.WriteLine($"Circles: {circleCount}");
            //             Console.WriteLine($"Sliders: {sliderCount}");
            //             Console.WriteLine($"Spinners: {spinnerCount}");


        }

        public static void DecodeBOMBA()
        {
            string folderPath = @"D:\osu\Songs";

            string[] osuFiles = Directory.GetFiles(folderPath, "*.osu", SearchOption.AllDirectories);
            Console.WriteLine($"we have {osuFiles.Length} files!");

            foreach (string osuFile in osuFiles)
            {
                Console.WriteLine($"Parsing Beatmap {osuFile} ...");
                Beatmap.Decode(new FileInfo(osuFile));
            }
            Console.WriteLine($"done Parsing Beatmaps");
        }
    }
}