namespace Beatmap;

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
        Objects = new List<IHitObject>();
    }

    /// <summary>
    /// Converts a list of strings into a <see cref="HitObjects"/> object.
    /// </summary>
    /// <param name="beatmap"></param>
    /// <param name="lines"></param>
    /// <returns></returns>
    public static HitObjects FromData(ref Beatmap beatmap, List<string> lines)
    {
        List<IHitObject> result = [];
        foreach (var line in lines)
        {
            var split = line.Split(',').ToList();
            try
            {
                var type = Beatmap.GetHitObjectType(int.Parse(split[3])) ?? throw new Exception("objectType is invalid");

                IHitObject hitObject = type switch
                {
                    HitObjectType.Circle => Circle.FromData(split),
                    HitObjectType.Slider => Slider.ParseFromData(split),
                    HitObjectType.Spinner => Spinner.FromData(split),
                    HitObjectType.ManiaHold => ManiaHold.FromData(split),
                    _ => HitObject.FromData(split),
                };

                result.Add(hitObject);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create hit object\n{ex}");
            }
        }

        return new HitObjects()
        {
            Objects = result
        };
    }
}