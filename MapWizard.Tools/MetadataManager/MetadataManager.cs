using BeatmapParser;
using BeatmapParser.Colours;
using BeatmapParser.Enums.Storyboard;
using BeatmapParser.Events;
using BeatmapParser.Sections;
using MapWizard.Tools.HelperExtensions;

namespace MapWizard.Tools.MetadataManager;

public static class MetadataManager
{
    public static void ApplyMetadata(BeatmapMetadata metadata, string[] targetPaths, MetadataManagerOptions options)
    {
        try
        {
            foreach (var targetPath in targetPaths)
            {
                var beatmapText = System.IO.File.ReadAllText(targetPath);
                var beatmap = Beatmap.Decode(beatmapText);
                
                // Metadata field
                if (options.ApplyMetadataSection)
                {
                    beatmap.MetadataSection.Artist = metadata.RomanizedArtist;
                    beatmap.MetadataSection.ArtistUnicode = metadata.Artist;
                    beatmap.MetadataSection.Title = metadata.RomanizedTitle;
                    beatmap.MetadataSection.TitleUnicode = metadata.Title;
                    beatmap.MetadataSection.Creator = metadata.Creator;
                    beatmap.MetadataSection.Source = metadata.Source;
                    beatmap.MetadataSection.Tags = metadata.Tags.Split(' ').ToList();
                    beatmap.MetadataSection.BeatmapSetID = metadata.BeatmapSetId;
                }

                if (options.ResetBeatmapIds)
                {
                    beatmap.MetadataSection.BeatmapID = 0;
                    beatmap.MetadataSection.BeatmapSetID = -1;
                }
                
                // General field
                if (options.ApplyGeneralSection)
                {
                    if (options.OverwriteAudio)
                        beatmap.GeneralSection.AudioFilename = metadata.AudioFilename;
                    
                    beatmap.GeneralSection.PreviewTime = metadata.PreviewTime;
                    beatmap.GeneralSection.SamplesMatchPlaybackRate = metadata.SamplesMatch;
                    beatmap.GeneralSection.LetterboxInBreaks = metadata.LetterboxInBreaks;
                    beatmap.GeneralSection.EpilepsyWarning = metadata.EpilepsyWarning;
                    beatmap.GeneralSection.WidescreenStoryboard = metadata.WidescreenStoryboard;
                    
                    // Events field
                    if (options.OverwriteBackground)
                        beatmap.Events.SetBackgroundImage(metadata.BackgroundFilename);
                    
                    var videoEvent = beatmap.Events.EventList.Where(x => x.Type == EventType.Video).Select(x => x as Video).FirstOrDefault();
                    if (videoEvent != null && options.OverwriteVideo)
                    {
                        videoEvent.FilePath = metadata.VideoFilename;
                        videoEvent.StartTime = new TimeSpan(0, 0, 0, 0, metadata.VideoOffset);
                        // replace video event
                        beatmap.Events.EventList.RemoveAll(x => x.Type == EventType.Video);
                        if (!string.IsNullOrEmpty(metadata.VideoFilename))
                        {
                            beatmap.Events.EventList.Add(videoEvent);
                        }
                    }
                }
                
                // Colours field
                if (options.ApplyColoursSection || options.ApplyCombosSection)
                {
                    // done this cuz im lazy to implement a clean constructor in the combo colour section class
                    beatmap.Colours ??= ColoursSection.Decode([]);
                    
                    if (options.ApplyCombosSection)
                    {
                        beatmap.Colours.Combos.Clear();
                        beatmap.Colours.Combos = metadata.Colours;
                    }

                    if (options.ApplyColoursSection)
                    {
                        beatmap.Colours.SliderBorder = null;
                        beatmap.Colours.SliderTrackOverride = null;
                        
                        if (options.SliderBorderColour)
                            beatmap.Colours.SliderBorder = metadata.SliderBorderColour;
                        
                        if (options.SliderTrackColour)
                            beatmap.Colours.SliderTrackOverride = metadata.SliderTrackColour;
                    }
                }

                // backup the original file
                if (!File.Exists(targetPath)) continue;

                BeatmapBackupHelper.CreateBackupCopy(targetPath);
                
                File.WriteAllText(targetPath, beatmap.Encode().Replace("\r\n", "\n").Replace("\n", "\r\n"));
            }
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            Console.WriteLine(ex.Message);
        }

    }
}
