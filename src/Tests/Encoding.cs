using System.Text;
using BeatmapParser;
using ShellProgressBar;

namespace MapWizard.Tests;

/// <summary>
/// Very barebones class to encode all beatmaps in a specified path and check for differences.
/// </summary>
public class Encoding
{

    /// <summary>
    /// Decode all beatmaps in the specified a specified path into a <see cref="Beatmap"/>
    /// , encodes it back and log the differences.
    /// </summary>
    public static void EncodeAllBeatmaps(string folderPath)
    {
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
