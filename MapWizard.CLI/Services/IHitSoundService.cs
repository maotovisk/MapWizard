namespace MapWizard.Services;

public interface IHitSoundService
{
    public void CopyHitsounds(string sourcePath, string targetPath);
    public void CopyHitsounds(string sourcePath, string targetPath, double offset);
    public void CopyHitsounds(string sourcePath, string targetPath, double offset, double leniency);
    public void CopyHitsounds(string sourcePath, string targetPath, double offset, double leniency, bool copySampleChanges);
}