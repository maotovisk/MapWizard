using System.IO.Enumeration;

namespace Beatmap;
/// <summary>
///
/// </summary>
public class HitSample : IHitSample
{
    /// <summary>
    /// Sample set for hitnormals.
    /// </summary>
    public SampleSet NormalSet { get; set; }

    /// <summary>
    /// Sample set for additions.
    /// </summary>
    public SampleSet AdditionSet { get; set; }

    /// <summary>
    /// Index of the hit sample.
    /// </summary>
    public uint Index { get; set; }

    /// <summary>
    /// Volume of the hit sample.
    /// </summary>
    public uint Volume { get; set; }

    /// <summary>
    /// Name of the file of the custom sample.
    /// </summary>
    public string FileName { get; set; }

    ///  <summary>
    /// Initializes a new instance of the <see cref="HitSample"/> class.
    /// </summary>
    /// <param name="normalSet"></param>
    /// <param name="additionSet"></param>
    /// <param name="index"></param>
    /// <param name="volume"></param>
    /// <param name="fileName"></param>
    public HitSample(SampleSet normalSet, SampleSet additionSet, uint index, uint volume, string fileName)
    {
        NormalSet = normalSet;
        AdditionSet = additionSet;
        Index = index;
        Volume = volume;
        FileName = fileName;
    }

    /// <summary>
    /// Initialization of HitSample class
    /// </summary>
    public HitSample()
    {
        NormalSet = SampleSet.Default;
        AdditionSet = SampleSet.Default;
        Index = 0;
        Volume = 0;
        FileName = string.Empty;
    }

    /// <summary>
    /// Converts the hit sample from a string to a HitSample object.
    /// </summary>
    /// <param name="data"></param>
    /// <returns><see cref="HitSample"/></returns>
    public static HitSample FromData(string data)
    {
        try
        {
            string[] split = data.Split(':');
            return new HitSample(
                normalSet: (SampleSet)uint.Parse(split[0]),
                additionSet: (SampleSet)uint.Parse(split[1]),
                index: uint.Parse(split[2]),
                volume: uint.Parse(split[3]),
                fileName: split[4]
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse HitSample {ex}");
            return new HitSample();
        }
    }
}