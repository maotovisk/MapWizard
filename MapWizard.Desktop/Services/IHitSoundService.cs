using System.Threading.Tasks;
using MapWizard.Tools.HitSoundCopier;

namespace MapWizard.Desktop.Services;

public interface IHitSoundService
{
    public bool CopyHitsoundsAsync(string sourcePath, string[] targetPath, HitSoundCopierOptions options);
}