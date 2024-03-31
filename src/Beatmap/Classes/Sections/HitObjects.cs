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

    public static List<IHitObject> FromData(ref Beatmap beatmap, List<string> lines)
    {
        List<IHitObject> result = [];
        foreach (var line in lines)
        {
            var split = line.Split(',').ToList();
            try
            {
                var type = GetHitObjectType(int.Parse(split[3])) ?? throw new Exception("objectType is invalid");

                IHitObject hitObject = type switch
                {
                    //HitObjectType.Circle => typeof(Circle),
                    HitObjectType.Slider => Slider.FromData(split),
                    //HitObjectType.Spinner => typeof(Spinner),
                    //HitObjectType.ManiaHold => typeof(ManiaHold),
                    _ => HitObject.FromData(split),
                };

                //var obj = (IHitObject?)Activator.CreateInstance(hitObjectType, split) ?? throw new Exception("Object instance is null");

                result.Add(hitObject);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create hit object\n{ex}");
            }
        }

        return new HitObjects(result);
    }
}