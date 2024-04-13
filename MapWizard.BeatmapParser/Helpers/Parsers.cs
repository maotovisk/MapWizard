using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Generic helper methods.
/// </summary>
public partial class Helper
{
    /// <summary>
    /// Converts integer bitwise into a <see cref="HitSound"/> list.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<HitSound> ParseHitSounds(int data)
    {
        if (data == 0) return [HitSound.None];
        List<HitSound> hitSounds = [];

        foreach (HitSound name in Enum.GetValues<HitSound>())
        {
            if ((data & (int)name) != 0) hitSounds.Add(name);
        }

        return hitSounds;
    }

    /// <summary>
    /// Encodes a list of hit sounds into a bitwise.
    /// </summary>
    /// <param name="hitSounds"></param>
    /// <returns></returns>
    public static int EncodeHitSounds(List<HitSound> hitSounds)
    {
        var result = 0;
        foreach (var hitSound in hitSounds.Distinct())
        {
            result |= (int)hitSound;
        }
        return result;
    }

    /// <summary>
    /// Get the a list of effects from a bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static List<Effect> ParseEffects(int data)
    {
        List<Effect> types = [];
        foreach (Effect name in Enum.GetValues<Effect>())
        {
            if ((data & (int)name) != 00000000) types.Add(name);
        }
        return types;
    }

    /// <summary>
    /// Encodes a list of effects into a bitwise.
    /// </summary>
    /// <param name="effects"></param>
    /// <returns></returns>
    public static int EncodeEffects(List<Effect> effects)
    {
        var result = 0;
        foreach (Effect effect in effects)
        {
            result |= (int)effect;
        }
        return result;
    }

    /// <summary>
    /// Gets the hit object type from a bitwise.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static HitObjectType ParseHitObjectType(int data)
    {
        List<HitObjectType> types = [];
        foreach (HitObjectType name in Enum.GetValues<HitObjectType>())
        {
            if ((data & (int)name) != 0x00000000) types.Add(name);
        }
        if (types.Count != 1) throw new Exception("Invalid hit object type.");

        return types[0];
    }

    /// <summary>
    /// Returns a <see cref="CurveType"/> from a character.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static CurveType ParseCurveType(char c) => c switch
    {
        'C' => CurveType.Catmull,
        'B' => CurveType.Bezier,
        'L' => CurveType.Linear,
        _ => CurveType.PerfectCurve
    };
    /// <summary>
    /// Encodes a <see cref="CurveType"/> into a string.
    /// </summary>
    /// <param name="curveType"></param>
    /// <returns></returns>

    public static string EncodeCurveType(CurveType curveType) => curveType switch
    {
        CurveType.Catmull => "C",
        CurveType.Bezier => "B",
        CurveType.Linear => "L",
        _ => "P"
    };

    /// <summary>
    /// Returns a Vector3 from a string.
    /// </summary>
    /// <param name="vectorString"></param>
    /// <returns></returns>
    public static Vector3 ParseVector3(string vectorString)
    {
        string[] split = vectorString.Split(',');
        return new Vector3(float.Parse(split[0], CultureInfo.InvariantCulture),
            float.Parse(split[1], CultureInfo.InvariantCulture), float.Parse(split[2], CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vectorString"></param>
    /// <returns></returns>
    public static Vector3 ParseVector3Hex(string vectorString)
    {
        string[] split = vectorString.Split(',');
        return new Vector3(int.Parse(split[0], NumberStyles.HexNumber),
            int.Parse(split[1], NumberStyles.HexNumber), int.Parse(split[2], NumberStyles.HexNumber));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Vector3 ParseVector3FromUnknownString(string input)
    {
        string[] split = input.Split(',', 3);

        if (split.Length != 3) throw new Exception("Not a valid vector3 string.");

        var X = IsNumeric(split[0]) ? int.Parse(split[0]) : int.Parse(split[0], NumberStyles.HexNumber);
        var Y = IsNumeric(split[1]) ? int.Parse(split[1]) : int.Parse(split[1], NumberStyles.HexNumber);
        var Z = IsNumeric(split[2]) ? int.Parse(split[2]) : int.Parse(split[2], NumberStyles.HexNumber);

        return new Vector3(X, Y, Z);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static bool IsNumeric(string input) => int.TryParse(input, out _);

    public static void TryParseVector3(string vectorString, out Vector3 vector)
    {
        string[] split = vectorString.Split(',');

        //check if it's using hex or decimal values
        if (IsNumeric(split[0]))
        {
            vector = ParseVector3(vectorString);
            return;
        }
        vector = ParseVector3Hex(vectorString);
        return;
    }

    /// <summary>
    /// Parses a Event type from a string array.
    /// </summary>
    /// <param name="eventLine"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static EventType ParseEventType(string[] eventLine)
    {

        string eventType = eventLine[0].Trim();

        foreach (EventType type in Enum.GetValues<EventType>())
        {
            if (type.ToString().Equals(eventType, StringComparison.CurrentCultureIgnoreCase) ||
                ((int)type).ToString().Equals(eventType, StringComparison.CurrentCultureIgnoreCase))
                return type;
        }

        throw new Exception($"Invalid event type: {eventType}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commandType"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static CommandType ParseCommandType(string commandline)
    {
        var commandType = commandline.Split(',')[0];

        while (commandType.StartsWith(' ') || commandType.StartsWith('_')) commandType = commandType[1..];

        commandType = commandType.Trim();

        return commandType switch
        {
            "F" => CommandType.Fade,
            "M" => CommandType.Move,
            "MY" => CommandType.MoveY,
            "MX" => CommandType.MoveX,
            "S" => CommandType.Scale,
            "V" => CommandType.VectorScale,
            "R" => CommandType.Rotate,
            "C" => CommandType.Colour,
            "P" => CommandType.Parameter,
            "L" => CommandType.Loop,
            "T" => CommandType.Trigger,
            _ => throw new Exception($"Invalid command type: {commandType}"),
        };
    }


    public static ICommand ParseCommand(string line)
    {
        try
        {
            CommandType identity = ParseCommandType(line);
            ICommand commandDecoded = identity switch
            {
                CommandType.Fade => Fade.Decode(line),
                CommandType.Move => Move.Decode(line),
                CommandType.MoveY => MoveY.Decode(line),
                CommandType.MoveX => MoveX.Decode(line),
                CommandType.Scale => Scale.Decode(line),
                CommandType.Loop => Loop.Decode(line),
                CommandType.Trigger => Trigger.Decode(line),
                CommandType.VectorScale => VectorScale.Decode(line),
                CommandType.Rotate => Rotate.Decode(line),
                CommandType.Colour => Colour.Decode(line),
                CommandType.Parameter => Parameter.Decode(line),
                _ => throw new Exception($"Unhandled command type \'{identity}\'"),
            };
            return commandDecoded;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while parsing command's ('{line}'): {ex}");
        }
    }
}
