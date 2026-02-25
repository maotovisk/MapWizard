namespace MapWizard.Desktop.Models.Settings;

public class MainSettings
{
    [Setting("General", "DarkMode")]
    public bool DarkMode { get; set; } = false;

    [Setting("General", "SongsPath")]
    public string SongsPath { get; set; } = string.Empty;

    [Setting("General", "UpdateStream")]
    public UpdateStream UpdateStream { get; set; } = UpdateStream.Release;

    [Setting("Audio", "PreviewSongVolumePercent")]
    public int AudioPreviewSongVolumePercent { get; set; } = 80;

    [Setting("Audio", "PreviewHitSoundVolumePercent")]
    public int AudioPreviewHitSoundVolumePercent { get; set; } = 100;

    [Setting("Audio", "OutputDeviceId")]
    public string AudioOutputDeviceId { get; set; } = "default";
}
