using BeatmapParser;
using BeatmapParser.Enums.Storyboard;
using BeatmapParser.Events;

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
                beatmap.MetadataSection.Artist = metadata.RomanizedArtist;
                beatmap.MetadataSection.ArtistUnicode = metadata.Artist;
                beatmap.MetadataSection.Title = metadata.RomanizedTitle;
                beatmap.MetadataSection.TitleUnicode = metadata.Title;
                beatmap.MetadataSection.Creator = metadata.Creator;
                beatmap.MetadataSection.Source = metadata.Source;
                beatmap.MetadataSection.Tags = metadata.Tags.Split(' ').ToList();
                beatmap.MetadataSection.BeatmapID = metadata.BeatmapId;
                beatmap.MetadataSection.BeatmapSetID = metadata.BeatmapSetId;
                
                // General field
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
                
                // Colours field
                beatmap.Colours?.Combos.Clear();
                
                if (beatmap.Colours != null)
                {
                    beatmap.Colours.SliderBorder = null;
                    beatmap.Colours.SliderTrackOverride = null;
                    beatmap.Colours.Combos = metadata.Colours;
                    
                    if (options.SliderBorderColour)
                        beatmap.Colours.SliderBorder = metadata.SliderBorderColour;
                    
                    if (options.SliderTrackColour)
                        beatmap.Colours.SliderTrackOverride = metadata.SliderTrackColour;
                }

                // backup the original file
                if (!File.Exists(targetPath)) continue;
            
                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
                {
                    var backupDirectory = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/MapWizard/Backup");
                    
                    var currentTimestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                    File.Move(targetPath, backupDirectory.FullName + "/" + currentTimestamp + Path.GetFileName(targetPath));
                }
                
                File.WriteAllText(targetPath, beatmap.Encode().Replace("\r\n", "\n").Replace("\n", "\r\n"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }
}