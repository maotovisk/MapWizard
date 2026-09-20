using MapWizard.Tools.HelperExtensions;

namespace MapWizard.Tests.HelperExtensions;

[Collection("DataDirectoryTests")]
public class BeatmapBackupHelperTests
{
    [Fact]
    public void CreateBackupCopy_RepeatedCallsForSameFile_ProducesUniqueBackups()
    {
        var sandboxRoot = CreateSandbox();
        var previousDataHome = Environment.GetEnvironmentVariable(MapWizardPathResolver.DataDirectoryOverrideVariable);
        Environment.SetEnvironmentVariable(MapWizardPathResolver.DataDirectoryOverrideVariable, sandboxRoot);

        try
        {
            var beatmapDir = Directory.CreateDirectory(Path.Combine(sandboxRoot, "beatmaps"));
            var sourcePath = Path.Combine(beatmapDir.FullName, "target.osu");
            File.WriteAllText(sourcePath, "v1");

            var backupPath1 = BeatmapBackupHelper.CreateBackupCopy(sourcePath);
            var backupPath2 = BeatmapBackupHelper.CreateBackupCopy(sourcePath);

            Assert.True(File.Exists(sourcePath));
            Assert.True(File.Exists(backupPath1));
            Assert.True(File.Exists(backupPath2));
            Assert.NotEqual(backupPath1, backupPath2);
            Assert.Equal("v1", File.ReadAllText(backupPath1));
            Assert.Equal("v1", File.ReadAllText(backupPath2));
        }
        finally
        {
            Environment.SetEnvironmentVariable(MapWizardPathResolver.DataDirectoryOverrideVariable, previousDataHome);
            Directory.Delete(sandboxRoot, recursive: true);
        }
    }

    private static string CreateSandbox()
    {
        var path = Path.Combine(Path.GetTempPath(), "mapwizard-backup-helper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
