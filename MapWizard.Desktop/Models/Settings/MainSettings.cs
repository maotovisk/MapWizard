namespace MapWizard.Desktop.Models.Settings;

public class MainSettings
{
    [Setting("General", "ThemeMode")]
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    [Setting("General", "SongsPath")]
    public string SongsPath { get; set; } = string.Empty;

    [Setting("General", "UpdateStream")]
    public UpdateStream UpdateStream { get; set; } = UpdateStream.Release;

    [Setting("General", "EnableHitSoundVisualizer")]
    public bool EnableHitSoundVisualizer { get; set; } = false;

    [Setting("Audio", "PreviewSongVolumePercent")]
    public int AudioPreviewSongVolumePercent { get; set; } = 80;

    [Setting("Audio", "PreviewHitSoundVolumePercent")]
    public int AudioPreviewHitSoundVolumePercent { get; set; } = 100;

    [Setting("Audio", "OutputDeviceId")]
    public string AudioOutputDeviceId { get; set; } = "default";
}
