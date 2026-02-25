namespace MapWizard.Desktop.Models.Settings;

public class MainSettings
{
    [Setting("General", "DarkMode")]
    public bool DarkMode { get; set; } = false;

    [Setting("General", "SongsPath")]
    public string SongsPath { get; set; } = string.Empty;

    [Setting("General", "UpdateStream")]
    public UpdateStream UpdateStream { get; set; } = UpdateStream.Release;

    [Setting("HitSoundVisualizer", "SongVolumePercent")]
    public int HitSoundVisualizerSongVolumePercent { get; set; } = 80;

    [Setting("HitSoundVisualizer", "HitSoundVolumePercent")]
    public int HitSoundVisualizerHitSoundVolumePercent { get; set; } = 100;
}
