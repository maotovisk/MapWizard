using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
///  This is a osu file format v14 specification of the Metadata section.
/// </summary>
public class Metadata
{
    /// <summary>
    /// Romanised song title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Song title
    /// </summary>
    public string TitleUnicode { get; set; }

    /// <summary>
    /// Romanised song artist
    /// </summary>
    public string Artist { get; set; }

    /// <summary>
    /// Song artist
    /// </summary>
    public string ArtistUnicode { get; set; }

    /// <summary>
    /// Beatmap creator
    /// </summary>

    public string Creator { get; set; }

    /// <summary>
    /// Difficulty name
    /// </summary>

    public string Version { get; set; }

    /// <summary>
    /// Original media the song was produced for
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Search terms for the map
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Difficulty ID
    /// </summary>
    public int BeatmapID { get; set; }

    /// <summary>
    /// Beatmap ID
    /// </summary>
    public int BeatmapSetID { get; set; } = -1;

    /// <summary>
    /// Constructor for Metadata
    /// </summary>
    /// <param name="title"></param>
    /// <param name="titleUnicode"></param>
    /// <param name="artist"></param>
    /// <param name="artistUnicode"></param>
    /// <param name="creator"></param>
    /// <param name="version"></param>
    /// <param name="source"></param>
    /// <param name="tags"></param>
    /// <param name="beatmapId"></param>
    /// <param name="beatmapSetId"></param>
    private Metadata(string title, string titleUnicode, string artist, string artistUnicode, string creator,
        string version, string source, List<string> tags, int beatmapId, int beatmapSetId)
    {
        Title = title;
        TitleUnicode = titleUnicode;
        Artist = artist;
        ArtistUnicode = artistUnicode;
        Creator = creator;
        Version = version;
        Source = source;
        Tags = tags;
        BeatmapID = beatmapId;
        BeatmapSetID = beatmapSetId;
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public Metadata()
    {
        Title = string.Empty;
        TitleUnicode = string.Empty;
        Artist = string.Empty;
        ArtistUnicode = string.Empty;
        Creator = string.Empty;
        Version = string.Empty;
        Source = string.Empty;
        Tags = [];
    }

    /// <summary>
    /// Parses the Metadata section of the beatmap into a new <see cref="Metadata"/> class
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public static Metadata Decode(List<string> section)
    {
        Dictionary<string, string> metadata = [];
        try
        {
            section.ForEach(line =>
            {
                var splitLine = line.Split(':', 2);

                if (splitLine.Length < 1)
                {
                    throw new Exception("Invalid Metadata section field.");
                }

                metadata[splitLine[0].Trim()] = splitLine.Length != 1 ? splitLine[1].Trim() : string.Empty;
            });

            foreach (var key in new[] { "Title", "Artist", "Creator", "Version" })
            {
                if (!metadata.ContainsKey(key))
                    throw new Exception($"Metadata section missing required field: {key}");
            }

            return new Metadata(
                title: metadata["Title"],
                titleUnicode: metadata.TryGetValue("TitleUnicode", out var titleUnicode)
                    ? titleUnicode
                    : metadata["Title"],
                artist: metadata["Artist"],
                artistUnicode: metadata.TryGetValue("ArtistUnicode", out var artistUnicode)
                    ? artistUnicode
                    : metadata["Artist"],
                creator: metadata["Creator"],
                version: metadata["Version"],
                source: metadata.TryGetValue("Source", out var source) ? source : string.Empty,
                tags: metadata.TryGetValue("Tags", out var tags) ? [.. tags.Split(' ')] : [],
                beatmapId: metadata.TryGetValue("BeatmapID", out var beatmapId) ? int.Parse(beatmapId) : 0,
                beatmapSetId: metadata.TryGetValue("BeatmapSetID", out var beatmapSetId) ? int.Parse(beatmapSetId) : -1
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Metadata: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Encodes the <see cref="General"/> class into a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        foreach (var prop in typeof(Metadata).GetProperties())
        {
            if (prop.GetValue(this) is null) continue;
            if (prop.GetValue(this) is string str && string.IsNullOrEmpty(str) && Helper.FormatVersion == 128) continue;
            
            var separator = Helper.FormatVersion == 128 ? ": " : ":";

            if (prop.Name == "Tags")
            {
                builder.AppendLine($"{prop.Name}{separator}{string.Join(' ', Tags)}");
                continue;
            }
            
            if (prop.Name == "BeatmapSetID" && BeatmapSetID == -1 && Helper.FormatVersion == 128)
                continue;
            
            if (prop.Name == "BeatmapID" && BeatmapID == 0 && Helper.FormatVersion == 128)
                continue;

            if (prop.GetValue(this) is bool boolValue)
            {
                builder.AppendLine($"{prop.Name}{separator}{(boolValue ? 1 : 0)}");
                continue;
            }

            if (prop.GetValue(this) is double doubleValue)
            {
                builder.AppendLine($"{prop.Name}{separator}{doubleValue.ToString(CultureInfo.InvariantCulture)}");
                continue;
            }

            builder.AppendLine($"{prop.Name}{separator}{prop.GetValue(this)}");
        }

        return builder.ToString();
    }
}