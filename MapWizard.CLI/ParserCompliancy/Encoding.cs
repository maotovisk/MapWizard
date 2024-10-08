using System.Text;
using MapWizard.BeatmapParser;

namespace MapWizard.CLI.ParserCompliancy;

/// <summary>
/// Very barebones class to encode all beatmaps in a specified path and check for differences.
/// </summary>
public class Encoding
{

    /// <summary>
    /// Decode all beatmaps in the specified a specified path into a <see cref="Beatmap"/>
    /// , encodes it back and log the differences.
    /// </summary>
    public static void EncodeAllMapsFrom(string folderPath)
    {
        string[] osuFiles = Directory.GetFiles(folderPath, "*.osu", SearchOption.AllDirectories);
        Console.WriteLine($"{osuFiles.Length} files detected, press any key to start BOMBA ...");
        Console.ReadKey();

        List<string> differences = [];

        int count = 0;

        foreach (string osuFile in osuFiles)
        {
            count++;
            ConsoleUtils.ProgressBar.DrawProgressBar(osuFiles.Length, count);
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
                    while (index2 < maxLength && parsedLines[index2].StartsWith("//")) ++index2;

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
