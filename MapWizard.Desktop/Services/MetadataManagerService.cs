using System;
using System.Drawing;
using System.IO;
using System.Linq;
using MapWizard.BeatmapParser;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Services;

public class MetadataManagerService : IMetadataManagerService
{
    public void ApplyMetadata(BeatmapMetadata metadata, string[] targetPaths)
    {
        try
        {
            foreach (var targetPath in targetPaths)
            {
                var beatmapText = System.IO.File.ReadAllText(targetPath);
                var beatmap = Beatmap.Decode(beatmapText);
                
                // Metadata field
                beatmap.Metadata.Artist = metadata.RomanizedArtist;
                beatmap.Metadata.ArtistUnicode = metadata.Artist;
                beatmap.Metadata.Title = metadata.RomanizedTitle;
                beatmap.Metadata.TitleUnicode = metadata.Title;
                beatmap.Metadata.Creator = metadata.Creator;
                beatmap.Metadata.Source = metadata.Source;
                beatmap.Metadata.Tags = metadata.Tags.Split(' ').ToList();
                beatmap.Metadata.BeatmapID = metadata.BeatmapId;
                beatmap.Metadata.BeatmapSetID = metadata.BeatmapSetId;
                
                // General field
                beatmap.General.AudioFilename = metadata.AudioFilename;
                beatmap.General.PreviewTime = metadata.PreviewTime;
                beatmap.General.SamplesMatchPlaybackRate = metadata.SamplesMatch;
                beatmap.General.LetterboxInBreaks = metadata.LetterboxInBreaks;
                beatmap.General.EpilepsyWarning = metadata.EpilepsyWarning;
                beatmap.General.WidescreenStoryboard = metadata.WidescreenStoryboard;
                
                // Events field
                beatmap.Events.SetBackgroundImage(metadata.BackgroundFilename);
                
                var videoEvent = beatmap.Events.EventList.Where(x => x.Type == EventType.Video).Select(x => x as Video).FirstOrDefault();
                if (videoEvent != null)
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
                
                foreach (var combo in metadata.Colours)
                {
                    beatmap.Colours.Combos.Add(new ComboColour((uint)combo.Number, combo.Colour ?? Color.White));
                }
                
                if (metadata.SliderBorderColour != null)
                {
                    beatmap.Colours.SliderBorder = metadata.SliderBorderColour;
                }
                
                if (metadata.SliderTrackColour != null)
                {
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