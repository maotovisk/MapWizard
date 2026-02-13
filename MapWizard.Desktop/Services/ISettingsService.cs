using MapWizard.Desktop.Models.Settings;

namespace MapWizard.Desktop.Services;

public interface ISettingsService
{
    string ConfigDirectoryPath { get; }
    MainSettings GetMainSettings();
    void SaveMainSettings(MainSettings settings);
}
