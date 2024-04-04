using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace BeatmapParser.Sections;

/// <summary>
///  This is a osu file format v14 specification of the Metadata section.
/// </summary>
public class Metadata : IMetadata
{
    /// <summary>
    /// Romanised song title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Song title
    /// </summary>
    public string TitleUnicode { get; set; } = string.Empty;

    /// <summary>
    /// Romanised song artist
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Song artist
    /// </summary>
    public string ArtistUnicode { get; set; } = string.Empty;

    /// <summary>
    /// Beatmap creator
    /// </summary>

    public string Creator { get; set; } = string.Empty;
    /// <summary>
    /// Difficulty name
    /// </summary>

    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Original media the song was produced for
    /// </summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>
    /// Search terms for the map
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Difficulty ID
    /// </summary>
    public int BeatmapID { get; set; } = 0;

    /// <summary>
    /// Beatmap ID
    /// </summary>
    public int BeatmapSetID { get; set; } = 0;

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
    public Metadata(string title, string titleUnicode, string artist, string artistUnicode, string creator, string version, string source, List<string> tags, int beatmapId, int beatmapSetId)
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
                string[] splittedLine = line.Split(':', 2);

                if (splittedLine.Length < 1)
                {
                    throw new Exception("Invalid Metadata section field.");
                }

                if (metadata.ContainsKey(splittedLine[0].Trim()))
                {
                    throw new Exception("Adding same propriety multiple times.");
                }

                metadata.Add(splittedLine[0].Trim(), splittedLine.Length != 1 ? splittedLine[1].Trim() : string.Empty);
            });

            if (Helper.IsWithinProperitesQuantitity<IMetadata>(metadata.Count))
            {
                throw new Exception("Invalid Metadata section length.");
            }

            return new Metadata(
                title: metadata["Title"],
                titleUnicode: metadata.TryGetValue("TitleUnicode", out var titleUnicode) ? titleUnicode : metadata["Title"],
                artist: metadata["Artist"],
                artistUnicode: metadata.TryGetValue("ArtistUnicode", out var artistUnicode) ? artistUnicode : metadata["Artist"],
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

        foreach (var prop in typeof(IMetadata).GetProperties())
        {
            if (prop.GetValue(this) is null) continue;

            if (prop.Name == "Tags")
            {
                builder.AppendLine($"{prop.Name}:{string.Join(' ', Tags)}");
                continue;
            }

            builder.AppendLine($"{prop.Name}:{prop.GetValue(this)}");
        }

        return builder.ToString();
    }
}