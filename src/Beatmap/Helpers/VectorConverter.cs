using System.Numerics;

namespace BeatmapParser;

public static class VectorConverter
{


    public static Vector3 ToVector3(string vectorString)
    {
        string[] split = vectorString.Split(',');
        return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
    }
}