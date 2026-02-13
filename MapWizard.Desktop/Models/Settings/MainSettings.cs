namespace MapWizard.Desktop.Models.Settings;

public class MainSettings
{
    [Setting("General", "DarkMode")]
    public bool DarkMode { get; set; } = false;
}
