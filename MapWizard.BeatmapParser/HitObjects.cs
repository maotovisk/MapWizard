using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the hit objects section of a beatmap.
/// </summary>
public class HitObjects
{
    /// <summary>
    /// Represents the list of hit  objects in the beatmap.
    /// </summary>
    public List<IHitObject> Objects { get; set; }

    ///  <summary>
    /// Initializes a new instance of <see cref="HitObjects"/> section.
    /// </summary>
    /// <param name="objects"></param>
    private HitObjects(List<IHitObject> objects)
    {
        Objects = objects;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HitObjects"/> section.
    /// </summary>
    public HitObjects()
    {
        Objects = [];
    }

    /// <summary>
    /// Decodes the provided list of hit object data strings into a <see cref="HitObjects"/> instance.
    /// </summary>
    /// <param name="lines">A list of strings representing the raw hit object data.</param>
    /// <param name="timingPoints">The timing points used to calculate timing-related properties for the hit objects.</param>
    /// <param name="difficulty">The difficulty settings used to aid in decoding the hit objects.</param>
    /// <returns>A <see cref="HitObjects"/> instance containing the parsed hit objects.</returns>
    public static HitObjects Decode(List<string> lines, TimingPoints timingPoints, Difficulty difficulty)
    {
        List<IHitObject> result = [];
        try
        {
            foreach (var line in lines)
            {
                var split = line.Split(',').ToList();

                var type = Helper.ParseHitObjectType(int.Parse(split[3]));

                // todo: ManiaHold case
                IHitObject hitObject = type switch
                {
                    HitObjectType.Circle => Circle.Decode(split),
                    HitObjectType.Slider => Slider.Decode(split, timingPoints, difficulty),
                    HitObjectType.Spinner => Spinner.Decode(split),
                    _ => HitObject.Decode(split),
                };

                result.Add(hitObject);
            }

            return new HitObjects(result);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse HitObjects section\n{ex}");
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        foreach (var obj in Objects)
        {
            switch (obj)
            {
                case Circle circle:
                    builder.AppendLine(circle.Encode());
                    break;
                case Slider slider:
                    builder.AppendLine(slider.Encode());
                    break;
                case Spinner spinner:
                    builder.AppendLine(spinner.Encode());
                    break;
                case ManiaHold hold:
                    builder.AppendLine(hold.Encode());
                    break;
                default:
                    builder.AppendLine(((HitObject)obj).Encode());
                    break;
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// Gets the hit object at a specific time.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="leniency"></param>
    /// <returns></returns>
    public IHitObject? GetHitObjectAt(double time, int leniency = 2)
    {
        if (Objects.Count == 0) return null;

        foreach (var obj in Objects)
        {
            switch (obj)
            {
                case Circle circle when Math.Abs(circle.Time.TotalMilliseconds - time) <= leniency:
                    return circle;
                case Slider slider when Math.Abs(slider.Time.TotalMilliseconds - time) <= leniency || Math.Abs(slider.EndTime.TotalMilliseconds - time) <= leniency:
                    return slider;
                case Slider slider:
                    {
                        if (slider is { Slides: > 1, RepeatSounds: not null } && slider.RepeatSounds.Count == (slider.Slides - 1))
                            for (var i = 1; i < slider.Slides - 1; i++)
                            {
                                var repeatTime = (slider.EndTime - slider.Time) / (slider.Slides - 1) * i + slider.Time;
                                if (Math.Abs(repeatTime.TotalMilliseconds - time) <= leniency)
                                    return slider;
                            }

                        break;
                    }
                case Spinner spinner when Math.Abs(spinner.End.TotalMilliseconds - time) <= leniency:
                    return spinner;
            }
        }

        return null;
    }
}