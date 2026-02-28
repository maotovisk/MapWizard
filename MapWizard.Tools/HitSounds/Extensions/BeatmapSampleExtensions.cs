using BeatmapParser;
using BeatmapParser.Enums;
using BeatmapParser.HitObjects;
using BeatmapParser.HitObjects.HitSounds;

namespace MapWizard.Tools.HitSounds.Extensions;

public static class BeatmapSampleExtensions
{
    public static HashSet<string> GetReferencedCustomSampleFileNames(this Beatmap beatmap)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var hitObject in beatmap.HitObjects.Objects)
        {
            switch (hitObject)
            {
                case Circle circle:
                    AddReferencedFileName(circle.HitSounds.SampleData.FileName, names);
                    break;
                case Slider slider:
                    AddReferencedFileName(slider.HitSounds.SampleData.FileName, names);
                    AddReferencedFileName(slider.HeadSounds.SampleData.FileName, names);
                    AddReferencedFileName(slider.TailSounds.SampleData.FileName, names);

                    if (slider.RepeatSounds != null)
                    {
                        foreach (var repeatSound in slider.RepeatSounds)
                        {
                            AddReferencedFileName(repeatSound.SampleData.FileName, names);
                        }
                    }

                    break;
                case Spinner spinner:
                    AddReferencedFileName(spinner.HitSounds.SampleData.FileName, names);
                    break;
            }
        }

        return names;
    }

    public static HashSet<int> GetUsedSampleIndices(this Beatmap beatmap)
    {
        var indices = new HashSet<int>();
        if (beatmap.TimingPoints == null)
        {
            return indices;
        }

        foreach (var timingPoint in beatmap.TimingPoints.TimingPointList)
        {
            indices.Add(NormalizeSampleIndex((int)timingPoint.SampleIndex));
        }

        return indices;
    }

    public static void RemapSampleIndices(this Beatmap beatmap, IReadOnlyDictionary<int, int> remap)
    {
        if (remap.Count == 0 || beatmap.TimingPoints == null)
        {
            return;
        }

        foreach (var timingPoint in beatmap.TimingPoints.TimingPointList)
        {
            var currentIndex = NormalizeSampleIndex((int)timingPoint.SampleIndex);
            if (remap.TryGetValue(currentIndex, out var remappedIndex))
            {
                timingPoint.SampleIndex = (uint)remappedIndex;
            }
        }
    }

    public static void RemapReferencedCustomSampleFileNames(
        this Beatmap beatmap,
        IReadOnlyDictionary<string, string> renameMap)
    {
        if (renameMap.Count == 0)
        {
            return;
        }

        foreach (var hitObject in beatmap.HitObjects.Objects)
        {
            switch (hitObject)
            {
                case Circle circle:
                    circle.HitSounds = RenameHitSoundSet(circle.HitSounds, renameMap);
                    break;
                case Slider slider:
                    slider.HitSounds = RenameHitSoundSet(slider.HitSounds, renameMap);
                    slider.HeadSounds = RenameHitSoundSet(slider.HeadSounds, renameMap);
                    slider.TailSounds = RenameHitSoundSet(slider.TailSounds, renameMap);

                    if (slider.RepeatSounds != null)
                    {
                        for (var i = 0; i < slider.RepeatSounds.Count; i++)
                        {
                            slider.RepeatSounds[i] = RenameHitSoundSet(slider.RepeatSounds[i], renameMap);
                        }
                    }

                    break;
                case Spinner spinner:
                    spinner.HitSounds = RenameHitSoundSet(spinner.HitSounds, renameMap);
                    break;
            }
        }
    }

    private static int NormalizeSampleIndex(int sampleIndex)
    {
        return sampleIndex <= 1 ? 1 : sampleIndex;
    }

    private static void AddReferencedFileName(string? rawName, HashSet<string> names)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return;
        }

        var fileName = Path.GetFileName(rawName.Trim().Trim('"'));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        names.Add(fileName);
    }

    private static (HitSample SampleData, List<HitSound> Sounds) RenameHitSoundSet(
        (HitSample SampleData, List<HitSound> Sounds) hitSoundSet,
        IReadOnlyDictionary<string, string> renameMap)
    {
        var currentFileName = hitSoundSet.SampleData.FileName;
        if (string.IsNullOrWhiteSpace(currentFileName))
        {
            return hitSoundSet;
        }

        var normalizedCurrentName = Path.GetFileName(currentFileName.Trim().Trim('"'));
        if (string.IsNullOrWhiteSpace(normalizedCurrentName) || !renameMap.TryGetValue(normalizedCurrentName, out var renamedFileName))
        {
            return hitSoundSet;
        }

        var renamedSampleData = new HitSample(
            hitSoundSet.SampleData.NormalSet,
            hitSoundSet.SampleData.AdditionSet,
            renamedFileName);

        return (renamedSampleData, hitSoundSet.Sounds);
    }
}
