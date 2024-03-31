namespace Beatmap;

interface IBeatmap
{
    FileInfo File { get; }
    IMetadataSection Metadata { get; set; }
    IGeneralSection General { get; set; }
    IEditorSection Editor { get; set; }
    IDifficultySection Difficulty { get; set; }
    IColoursSection Colours { get; set; }



}