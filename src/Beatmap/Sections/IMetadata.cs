namespace Beatmap.Sections;

/// <summary>
///  This is a osu file format v14 specification of the Metadata section.
/// </summary>
public interface IMetadata
{
    /// <summary>
    /// Romanised song title
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// Song title
    /// </summary>
    string TitleUnicode { get; set; }

    /// <summary>
    /// Romanised song artist
    /// </summary>
    string Artist { get; set; }

    /// <summary>
    /// Song artist
    /// </summary>
    string ArtistUnicode { get; set; }

    /// <summary>
    /// Beatmap creator
    /// </summary>

    string Creator { get; set; }
    /// <summary>
    /// Difficulty name
    /// </summary>

    string Version { get; set; }
    /// <summary>
    /// Original media the song was produced for
    /// </summary>
    string Source { get; set; }
    /// <summary>
    /// Search terms for the map
    /// </summary>
    List<string> Tags { get; set; }

    /// <summary>
    /// Difficulty ID
    /// </summary>
    int BeatmapID { get; set; }

    /// <summary>
    /// Beatmap ID
    /// </summary>
    int BeatmapSetID { get; set; }
}