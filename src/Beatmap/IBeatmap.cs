namespace Beatmap;
using Sections;

interface IBeatmap
{
    FileInfo File { get; }
    IMetadata Metadata { get; set; }
    IGeneral General { get; set; }
    IEditor Editor { get; set; }
    IDifficulty Difficulty { get; set; }
    IColours Colours { get; set; }



}