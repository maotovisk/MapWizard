using System.Numerics;

namespace Beatmap;

/// <summary>
/// Decodes a osu beatmap file into a <see cref="Beatmap"/>.
/// </summary>
public class BeatmapDecoder
{
    /// <summary>
    /// Splits the sections of a beatmap file into a dictionary.
    /// </summary>
    /// <param name="path">Path of tthe beatmap</param>
    /// <returns>Dictionary of sections</returns>
    /// <exception cref="Exception"></exception>
    public static Dictionary<string, List<string>> SplitSections(FileInfo path)
    {
        if (!path.Exists) throw new Exception("");

        Dictionary<string, List<string>> result = [];
        var lines = File.ReadAllLines(path.FullName).ToList();
        int firstIndex = -1;

        for (var index = 0; index != lines.Count; ++index)
        {
            foreach (string name in Enum.GetValues(typeof(SectionTypes)))
            {
                if (lines[index].Contains($"[{name}]"))
                {
                    if (firstIndex == -1)
                    {
                        firstIndex = index;
                        if (index != 0) result.Add("Begin", lines[0..index]);
                        continue;
                    }
                    if (firstIndex + 1 == index) result.Add(name, [lines[index]]);
                    else result.Add(name, lines[(firstIndex + 1)..index]);

                    firstIndex = index;
                }
            }
        }
        if (lines.Count - firstIndex != 0)
        {
            result.Add("End", lines[firstIndex..lines.Count]);
        }
        return result;
    }

    /// <summary>
    /// Decodes a dictionary of sections into a <see cref="Beatmap"/>.
    /// </summary>
    /// <param name="sections"></param>
    /// <returns></returns>
    public Beatmap Decode(Dictionary<string, List<string>> sections)
    {
        Beatmap beatmap = new();

        try
        {
            if (sections.TryGetValue($"Begin", out List<string>? version)) Version(ref beatmap, version);
            if (sections.TryGetValue($"{SectionTypes.General}", out List<string>? general)) General(ref beatmap, general);
            if (sections.TryGetValue($"{SectionTypes.Editor}", out List<string>? editor)) Editor(ref beatmap, editor);
            if (sections.TryGetValue($"{SectionTypes.Metadata}", out List<string>? metadata)) Metadata(ref beatmap, metadata);
            if (sections.TryGetValue($"{SectionTypes.Difficulty}", out List<string>? difficulty)) Difficulty(ref beatmap, difficulty);
            if (sections.TryGetValue($"{SectionTypes.Colours}", out List<string>? colours)) Colours(ref beatmap, colours);
            if (sections.TryGetValue($"{SectionTypes.Events}", out List<string>? events)) Events(ref beatmap, events);
            if (sections.TryGetValue($"{SectionTypes.TimingPoints}", out List<string>? timingPoints)) TimingPoints(ref beatmap, timingPoints);
            if (sections.TryGetValue($"{SectionTypes.HitObjects}", out List<string>? hitObjects)) HitObjects(ref beatmap, hitObjects);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            beatmap = new();
        }
        return beatmap;
    }

    private static void Version(ref Beatmap beatmap, List<string> section)
    {
        beatmap.Version = int.Parse(section[0]);
    }

    private void General(ref Beatmap beatmap, List<string> section)
    {
        beatmap.General = new General
        {
            AudioFilename = section[0],
            AudioLeadIn = int.Parse(section[1]),
            PreviewTime = int.Parse(section[2]),
            Countdown = bool.Parse(section[3]),
            SampleSet = section[4],
            StackLeniency = double.Parse(section[5]),
            Mode = int.Parse(section[6]),
            LetterboxInBreaks = bool.Parse(section[7]),
            WidescreenStoryboard = bool.Parse(section[8])
        };
    }

    private void Editor(ref Beatmap beatmap, List<string> section)
    {
        beatmap.Editor = new Editor
        {
            Bookmarks = section[0].Split(',').Select(double.Parse).Select(TimeSpan.FromMilliseconds).ToList(),
            DistanceSpacing = double.Parse(section[1]),
            BeatDivisor = int.Parse(section[2]),
            GridSize = int.Parse(section[3]),
            TimelineZoom = double.Parse(section[4])
        };
    }

    private void Metadata(ref Beatmap beatmap, List<string> section)
    {
        beatmap.Metadata = new Metadata
        {
            Title = section[0],
            TitleUnicode = section[1],
            Artist = section[2],
            ArtistUnicode = section[3],
            Creator = section[4],
            Version = section[5],
            Source = section[6],
            Tags = section[7].Split(' ').ToList(),
            BeatmapID = int.Parse(section[8]),
            BeatmapSetID = int.Parse(section[9])
        };
    }

    private void Difficulty(ref Beatmap beatmap, List<string> section)
    {
        beatmap.Difficulty = new Difficulty
        {
            HPDrainRate = double.Parse(section[0]),
            CircleSize = double.Parse(section[1]),
            OverallDifficulty = double.Parse(section[2]),
            ApproachRate = double.Parse(section[3]),
            SliderMultiplier = double.Parse(section[4]),
            SliderTickRate = double.Parse(section[5])
        };
    }

    private void Colours(ref Beatmap beatmap, List<string> section)
    {
        beatmap.Colours = section.Select(x =>
        {
            var split = x.Split(':');
            return new Colour
            {
                Name = split[0],
                Value = split[1]
            };
        }).ToList();
    }

    private void Events(ref Beatmap beatmap, List<string> section)
    {
        beatmap.Events = section.Select(x =>
        {
            var split = x.Split(',');
            return new Event
            {
                StartTime = int.Parse(split[0]),
                Layer = int.Parse(split[1]),
                EventType = (EventType)Enum.Parse(typeof(EventType), split[2]),
                Parameters = split.Skip(3).ToList()
            };
        }).ToList();
    }

    private void TimingPoints(ref Beatmap beatmap, List<string> section)
    {
        beatmap.TimingPoints = section.Select(x =>
        {
            var split = x.Split(',');
            return new TimingPoint
            {
                Offset = double.Parse(split[0]),
                MillisecondsPerBeat = double.Parse(split[1]),
                Meter = int.Parse(split[2]),
                SampleSet = int.Parse(split[3]),
                SampleIndex = int.Parse(split[4]),
                Volume = int.Parse(split[5]),
                Inherited = bool.Parse(split[6]),
                KiaiMode = bool.Parse(split[7])
            };
        }).ToList();
    }

    /// <summary>
    /// Gets the hit object type from a bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public HitObjectType? GetHitObjectType(int data)
    {
        List<HitObjectType> types = [];
        foreach (HitObjectType name in Enum.GetValues(typeof(HitObjectType)))
        {
            if ((data & (int)name) != 0x000000000) types.Add(name);
        }
        if (types.Count == 1) return types.First();

        return null;
    }

    /// <summary>
    /// Gets the hit sounds from a hit object, based on the bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public List<HitSound> GetHitSounds(int data)
    {
        List<HitSound> hitSounds = [];
        foreach (HitSound name in Enum.GetValues(typeof(HitSound)))
        {
            if ((data & (int)name) != 0x000000000) hitSounds.Add(name);
        }
        return hitSounds;
    }
    /// <summary>
    /// Parses a base hit object from a splitted hitObject line.
    /// </summary>
    /// <param name="split"></param>
    /// <returns></returns>
    public HitObject ParseHitObject(List<string> split)
    {
        // default      : x,    y,  time,   type,   hitSound,   objectParams,   hitSample
        // index          0,    1,  2,      3,      4,          5,              6

        var bits = int.Parse(split[3]);

        return new HitObject()
        {
            Coordinates = new Vector2(int.Parse(split[0]), int.Parse(split[1])),
            Time = TimeSpan.FromSeconds(double.Parse(split[2])),

            HitSounds = GetHitSounds(int.Parse(split[4])),
            HitSample = ParseHitSample(split.Last()),

            NewCombo = (bits & 0x000000F00) != 0x000000000,
            ComboColour = (uint)((bits & 0x00FFF0000) >> 4 * 4)
        };
    }


    /// <summary>
    /// Parses a hit sample from a string.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public HitSample ParseHitSample(string data)
    {
        var split = data.Split(':');

        // default  normalSet   :additionSet    :index  :volume :filename
        // index    0           :1              :2      :3      :4
        return new HitSample
        {
            NormalSet = (SampleSet)Enum.Parse(typeof(SampleSet), split[0]),
            AdditionSet = (SampleSet)Enum.Parse(typeof(SampleSet), split[1]),
            Index = uint.Parse(split[2]),
            Volume = uint.Parse(split[3]),
            FileName = split[4]
        };
    }
}