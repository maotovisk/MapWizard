using System.IO;
using BeatmapParser;
using ShellProgressBar;

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
            Console.WriteLine($"{osuFiles.Length} files detected, press any key to start parsing ...");

            Console.ReadKey();


            var options = new ProgressBarOptions
            {
                BackgroundCharacter = '\u2593',
                ForegroundColor = ConsoleColor.DarkGreen,
                BackgroundColor = ConsoleColor.Gray,
                ProgressBarOnBottom = true
            };

            List<(string File, Exception Exception)> parsingErrors = [];

            List<Beatmap> parsedBeatmaps = [];

            using (var pbar = new ProgressBar(osuFiles.Length, "Initial message", options))
                foreach (string osuFile in osuFiles)
                {
                    try
                    {
                        pbar.Tick($"Parsing Beatmap {osuFile}");
                        parsedBeatmaps.Add(Beatmap.Decode(new FileInfo(osuFile)));
                    }
                    catch (Exception e)
                    {
                        parsingErrors.Add((osuFile, e));
                    }
                }

            Console.WriteLine($"Parsing completed with {parsingErrors.Count} errors");
            Console.WriteLine($"Parsed {parsedBeatmaps.Count}/{osuFiles.Length} beatmaps successfully");
            Console.WriteLine($"Total maps ignored (format version not supported): {parsingErrors.Count(e => e.Exception.Message.Contains("is not supported yet."))}");

            Console.WriteLine("Press any key to write the errors file...");
            Console.ReadKey();

            if (parsingErrors.Count > 0)
            {
                File.WriteAllLines("parsing_errors.txt", parsingErrors.Where(e => !e.Exception.Message.Contains("is not supported yet.")).Select(e => $"{e.File}: {e.Exception.Message}\n{e.Exception.StackTrace}\n"));
                Console.WriteLine("Errors file written");
            }
        }
    }
}