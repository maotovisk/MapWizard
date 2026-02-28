namespace MapWizard.Tools.HelperExtensions;

public static class BeatmapBackupHelper
{
    private const string BackupTimestampFormat = "yyyy-MM-dd-HH-mm-ss";

    public static string CreateBackupCopy(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Backup source file does not exist.", sourcePath);
        }

        var fileName = Path.GetFileName(sourcePath);
        var currentTimestamp = DateTime.Now.ToString(BackupTimestampFormat);
        var exceptions = new List<Exception>();

        foreach (var backupDirectoryPath in GetBackupDirectoryCandidates(sourcePath))
        {
            try
            {
                var backupDirectory = Directory.CreateDirectory(backupDirectoryPath);
                var backupPath = BuildUniqueBackupPath(backupDirectory.FullName, currentTimestamp, fileName);
                File.Copy(sourcePath, backupPath, overwrite: false);
                return backupPath;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException("Failed to create backup for beatmap file.", exceptions);
    }

    private static IEnumerable<string> GetBackupDirectoryCandidates(string sourcePath)
    {
        yield return MapWizardPathResolver.ResolveBackupDirectoryPath();
        yield return Path.Combine(Path.GetDirectoryName(sourcePath) ?? ".", ".mapwizard-backup");
    }

    private static string BuildUniqueBackupPath(string backupDirectoryPath, string currentTimestamp, string fileName)
    {
        var backupPath = Path.Combine(backupDirectoryPath, $"{currentTimestamp}-{fileName}");
        var suffix = 1;

        while (File.Exists(backupPath))
        {
            backupPath = Path.Combine(backupDirectoryPath, $"{currentTimestamp}-{suffix}-{fileName}");
            suffix++;
        }

        return backupPath;
    }
}
