using System.Threading.Tasks;

namespace MapWizard.Desktop.Services;

public interface IHitSoundService
{
    public bool CopyHitsoundsAsync(string sourcePath, string[] targetPath);
}