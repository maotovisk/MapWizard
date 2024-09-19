namespace MapWizard.Tools.HitSoundCopier;

public class HitSoundOptions
{
    /*
        Add options for hitsound copying, such as copying all hitsounds, copying only the hitsounds of notes, ovewrite everything, leniency, etc.
    */
    
    public bool CopySliderBodySounds = true;
    public bool OvewriteEverything { get; } = true;
    public int Leniency = 5;

    public HitSoundOptions(bool ovewriteEverything, bool copySliderBodySounds)
    {
        OvewriteEverything = ovewriteEverything;
        CopySliderBodySounds = copySliderBodySounds;
    }
}