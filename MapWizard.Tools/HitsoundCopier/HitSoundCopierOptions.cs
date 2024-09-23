namespace MapWizard.Tools.HitSoundCopier;

public class HitSoundCopierOptions
{
    /*
        Add options for hitsound copying, such as copying all hitsounds, copying only the hitsounds of notes, ovewrite everything, leniency, etc.
    */
    
    public bool CopySliderBodySounds = true;
    public bool OvewriteEverything = true;
    public bool CopySampleAndVolumeChanges = true;
    public bool OverwriteMuting = true;
    public int Leniency = 5;

    public HitSoundCopierOptions()
    {
    }
}