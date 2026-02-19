namespace MapWizard.Tools.HelperExtensions;

public static class MapWizardPathResolver
{
    private const string AppDirectoryName = "MapWizard";
    private const string BackupDirectoryName = "Backup";

    public static string ResolveBackupDirectoryPath()
    {
        return Path.Combine(ResolveDataDirectoryPath(), BackupDirectoryName);
    }

    private static string ResolveDataDirectoryPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppDirectoryName);
        }

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppDirectoryName);
        }

        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var basePath = string.IsNullOrWhiteSpace(xdgDataHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share")
            : xdgDataHome;

        return Path.Combine(basePath, AppDirectoryName);
    }
}
