using System.Drawing;
using System.Linq;
using BeatmapParser.Colours;
using MapWizard.Desktop.Models;
using MapWizard.Tools.MetadataManager;

namespace MapWizard.Desktop.Services;

public class MetadataManagerService : IMetadataManagerService
{
    public void ApplyMetadata(AvaloniaBeatmapMetadata metadata, string[] targetPaths, MetadataManagerOptions options)
    {
        var targetMetadata = new BeatmapMetadata
        {
            Artist = metadata.Artist,
            Title = metadata.Title,
            Creator = metadata.Creator,
            Source = metadata.Source,
            Tags = metadata.Tags,
            BeatmapId = metadata.BeatmapId,
            BeatmapSetId = metadata.BeatmapSetId,
            AudioFilename = metadata.AudioFilename,
            BackgroundFilename = metadata.BackgroundFilename,
            VideoFilename = metadata.VideoFilename,
            VideoOffset = metadata.VideoOffset,
            PreviewTime = metadata.PreviewTime,
            RomanizedArtist = metadata.RomanizedArtist,
            RomanizedTitle = metadata.RomanizedTitle,
            Colours = metadata.Colours.Select(x => new ComboColour((uint)x.Number, x.Colour ?? Color.White)).ToList(),
            SliderTrackColour = metadata.SliderTrackColour,
            SliderBorderColour = metadata.SliderBorderColour,
            WidescreenStoryboard = metadata.WidescreenStoryboard,
            LetterboxInBreaks = metadata.LetterboxInBreaks,
            EpilepsyWarning = metadata.EpilepsyWarning,
            SamplesMatch = metadata.SamplesMatch
        };
        
        
        MetadataManager.ApplyMetadata(targetMetadata, targetPaths, options);
    }
}