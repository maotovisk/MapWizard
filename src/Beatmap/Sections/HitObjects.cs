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
    /// <returns></returns>
    public static HitObjects Decode(List<string> lines)
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
                    HitObjectType.Slider => Slider.Decode(split),
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
}