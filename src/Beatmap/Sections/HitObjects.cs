using System.Text;

namespace BeatmapParser.Sections;

/// <summary>
/// Represents the hit objects section of a beatmap.
/// </summary>
public class HitObjects : IHitObjects
{
    /// <summary>
    /// Represents the list of hit  objects in the beatmap.
    /// </summary>
    public List<IHitObject> Objects { get; set; }

    ///  <summary>
    /// Initializes a new instance of <see cref="HitObjects"/> section.
    /// </summary>
    /// <param name="objects"></param>
    public HitObjects(List<IHitObject> objects)
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
    /// Converts a list of strings into a <see cref="HitObjects"/> object.
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="timingPoints"></param>
    /// <param name="difficulty"></param>
    /// <returns></returns>
    public static HitObjects Decode(List<string> lines, TimingPoints timingPoints, Difficulty difficulty)
    {
        List<IHitObject> result = [];
        try
        {
            foreach (var line in lines)
            {
                var split = line.Split(',').ToList();

                var type = Helper.ParseHitObjectType(int.Parse(split[3]));

                IHitObject hitObject = type switch
                {
                    HitObjectType.Circle => Circle.Decode(split),
                    HitObjectType.Slider => Slider.Decode(split, timingPoints, difficulty),
                    HitObjectType.Spinner => Spinner.Decode(split),
                    HitObjectType.ManiaHold => ManiaHold.Decode(split),
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

            if (obj is Circle circle)
                builder.AppendLine(circle.Encode());
            else if (obj is Slider slider)
                builder.AppendLine(slider.Encode());
            else if (obj is Spinner spinner)
                builder.AppendLine(spinner.Encode());
            else if (obj is ManiaHold hold)
                builder.AppendLine(hold.Encode());
            else
                builder.AppendLine(((HitObject)obj).Encode());
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
        if (Objects == null) return null;

        if (Objects.Count == 0) return null;

        // We need to account if the object is a slider or Spinner, as they have a End time.
        foreach (var obj in Objects)
        {
            if (obj is Circle circle && Math.Abs(circle.Time.TotalMilliseconds - time) <= leniency)
                return circle;

            // we need to check if the time is in the repeat sounds too (need to calculate the time of the repeats based on the number of repeats and the difference between the end time and the start time of the slider)
            if (obj is Slider slider)
            {
                if (Math.Abs(slider.Time.TotalMilliseconds - time) <= leniency || Math.Abs(slider.EndTime.TotalMilliseconds - time) <= leniency)
                    return slider;

                if (slider.Repeats > 1 && slider.RepeatSounds != null && slider.RepeatSounds.Count == (slider.Repeats - 1))
                    for (int i = 1; i < slider.Repeats - 1; i++)
                    {
                        var repeatTime = (slider.EndTime - slider.Time) / (slider.Repeats - 1) * i + slider.Time;
                        if (Math.Abs(repeatTime.TotalMilliseconds - time) <= leniency)
                            return slider;
                    }
            }

            if (obj is Spinner spinner && Math.Abs(spinner.End.TotalMilliseconds - time) <= leniency)
                return spinner;
        }

        return null;
    }
}