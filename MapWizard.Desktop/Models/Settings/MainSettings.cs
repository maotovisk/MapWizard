namespace MapWizard.Desktop.Models.Settings;

public class MainSettings
{
    [Setting("General", "ThemeMode")]
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    [Setting("Appearance", "ColorPalette")]
    public ThemePalette ColorPalette { get; set; } = ThemePalette.MapWizardNoir;

    [Setting("Appearance", "EnableSmoothWheelScrolling")]
    public bool EnableSmoothWheelScrolling { get; set; } = true;

    [Setting("Appearance", "BlurModals")]
    public bool BlurModals { get; set; } = true;

    [Setting("Appearance", "ReducedMotion")]
    public bool ReducedMotion { get; set; } = false;

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

    [Setting("Experimental", "NowPlayingTracking")]
    public bool NowPlayingTracking { get; set; } = false;
}
