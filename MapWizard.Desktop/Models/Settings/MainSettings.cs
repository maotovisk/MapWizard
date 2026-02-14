namespace MapWizard.Desktop.Models.Settings;

public class MainSettings
{
    [Setting("General", "DarkMode")]
    public bool DarkMode { get; set; } = false;

    [Setting("General", "UpdateStream")]
    public UpdateStream UpdateStream { get; set; } = UpdateStream.Release;
}
