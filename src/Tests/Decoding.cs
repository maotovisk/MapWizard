using MapWizard.BeatmapParser;
namespace MapWizard.Tests;

/// <summary>
/// Very barebones class to decode all beatmaps in a specified path and check for parsing errors.
/// </summary>
public class Decoding
{
    /// <summary>
    /// Decode all beatmaps in the specified a specified path into a <see cref="Beatmap"/>
    /// and check for parsing errors.
    /// </summary>
    /// <param name="folderPath"></param>
    public static void DecodeAllMapsFrom(string folderPath)
    {
        string[] osuFiles = Directory.GetFiles(folderPath, "*.osu", SearchOption.AllDirectories);
        Console.WriteLine($"{osuFiles.Length} files detected, press any key to start parsing ...");

        Console.ReadKey();

        List<(string File, Exception Exception)> parsingErrors = [];

        List<Beatmap> parsedBeatmaps = [];

        foreach (string osuFile in osuFiles)
        {
            try
            {
                ConsoleUtils.ProgressBar.DrawProgressBar(osuFiles.Length, parsedBeatmaps.Count);
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
}