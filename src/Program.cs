using System.IO;
using System.Text;
using BeatmapParser;
using ShellProgressBar;

namespace HitsoundCopier;

class Program
{
    static void Main(FileInfo input, FileInfo output)
    {
        // DecodeBOMBA();
        DecodeBOMBA();
        return;

        // debug a specific beatmap if debug

#if DEBUG
        input = new FileInfo("test4.osu");
#endif
        Console.WriteLine($"Parsing Beatmap {input.Name} ...");
        var beatmap = Beatmap.Decode(input);
        Console.WriteLine($"Beatmap Parsed");

        Console.WriteLine(beatmap.Version);

        var circleCount = 0;
        var sliderCount = 0;
        var spinnerCount = 0;

        foreach (var obj in beatmap.HitObjects.Objects)
        {
            if (obj.Type == HitObjectType.Circle) { ++circleCount; continue; }
            if (obj.Type == HitObjectType.Slider) { ++sliderCount; continue; }
            if (obj.Type == HitObjectType.Spinner) { ++spinnerCount; continue; }
        }

        var firstRedLine = (UninheritedTimingPoint?)beatmap.TimingPoints?.TimingPointList.Where(tp => tp is UninheritedTimingPoint).FirstOrDefault();
        var bpm = 60 / (firstRedLine?.BeatLength / 1000) ?? 120;
        Console.WriteLine($"this beatmap bpm is: {bpm}");
        Console.WriteLine($"this beatmap have:");
        Console.WriteLine($"Circles: {circleCount}");
        Console.WriteLine($"Sliders: {sliderCount}");
        Console.WriteLine($"Spinners: {spinnerCount}");

        Console.WriteLine("Press any key to encode the beatmap back ...");

        Console.ReadKey();

        string encodedOsu = beatmap.Encode();
        Console.WriteLine("Beatmap Encoded");
        Console.WriteLine(encodedOsu);

        File.WriteAllText("output.osu", encodedOsu);
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

        Console.WriteLine($"Parsing completed with {parsingErrors.Count - parsingErrors.Count(e =>
            e.Exception.Message.Contains("is not supported yet.") || e.Exception.Message.Contains("Invalid beatmap file") || e.Exception.Message.Contains("Beatmap is empty"))} errors");
        Console.WriteLine($"Parsed {parsedBeatmaps.Count}/{osuFiles.Length} beatmaps successfully");
        Console.WriteLine($"Total maps ignored: {parsingErrors.Count(e =>
            e.Exception.Message.Contains("is not supported yet.") || e.Exception.Message.Contains("Invalid beatmap file") || e.Exception.Message.Contains("Beatmap is empty"))}");

        Console.WriteLine("Press any key to write the errors file...");
        Console.ReadKey();

        if (parsingErrors.Count > 0)
        {
            File.WriteAllLines("parsing_errors.log", parsingErrors.Where(e => !e.Exception.Message.Contains("is not supported yet.") && !e.Exception.Message.Contains("Invalid beatmap file") && !e.Exception.Message.Contains("Beatmap is empty")).Select(e => $"{e.File}: {e.Exception.Message}\n{e.Exception.StackTrace}\n"));
            Console.WriteLine("Errors file written");
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public static void EncodeBOMBA()
    {
        string folderPath = @"D:\osu\Songs";
        string[] osuFiles = Directory.GetFiles(folderPath, "*.osu", SearchOption.AllDirectories);
        Console.WriteLine($"{osuFiles.Length} files detected, press any key to start BOMBA ...");
        Console.ReadKey();

        List<string> differences = [];
        var options = new ProgressBarOptions
        {
            BackgroundCharacter = '\u2593',
            ForegroundColor = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.Gray,
            ProgressBarOnBottom = true
        };

        using var pbar = new ProgressBar(osuFiles.Length, "Initial message", options);

        foreach (string osuFile in osuFiles)
        {
            pbar.Tick($"Checking file: {osuFile}");
            string fileContent = File.ReadAllText(osuFile);

            try
            {
                var beatmap = Beatmap.Decode(fileContent);

                if (beatmap.Version != 14) continue;

                string parsed = beatmap.Encode();

                string[] originaLines = fileContent.Split("\r\n");
                string[] parsedLines = parsed.Split("\r\n");

                int maxLength = Math.Min(originaLines.Length, parsedLines.Length);

                StringBuilder builder = new();

                for (int index1 = 0, index2 = 0; index1 < maxLength && index2 < maxLength; ++index1, ++index2)
                {
                    while (index1 < maxLength && originaLines[index1].StartsWith("//")) ++index1;

                    if (originaLines[index1] == parsedLines[index2]) continue;

                    builder.AppendLine($"{index1 + 1}-: {originaLines[index1]}");
                    builder.AppendLine($"{index1 + 1}+: {parsedLines[index2]}");
                    builder.AppendLine();
                }

                var diff = builder.ToString();

                if (diff != string.Empty) differences.Add($"[{osuFile}]:\n" + diff);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errors when processing {osuFile}: {ex}");
            }
        }

        File.WriteAllLines("encode_diffs.log", differences);
    }
}