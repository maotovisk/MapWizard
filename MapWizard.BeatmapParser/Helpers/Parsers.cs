using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Generic helper methods used across the parser.
/// </summary>
public partial class Helper
{
    /// <summary>
    /// Parses a bitmask of hitsounds into a list of <see cref="HitSound"/> flags.
    /// </summary>
    /// <param name="data">The integer bitmask where each bit represents a <see cref="HitSound"/> value.</param>
    /// <returns>A list of <see cref="HitSound"/> flags. When <paramref name="data"/> is 0 returns a list containing <see cref="HitSound.None"/>.</returns>
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
    /// Encodes a list of <see cref="HitSound"/> flags into an integer bitmask.
    /// </summary>
    /// <param name="hitSounds">The list of hit sounds to encode. Duplicate flags are ignored.</param>
    /// <returns>An integer bitmask representing the combined hit sounds.</returns>
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
    /// Parses an integer bitmask into a list of <see cref="Effect"/> flags.
    /// </summary>
    /// <param name="data">The bitmask where each bit represents an <see cref="Effect"/>.</param>
    /// <returns>A list of <see cref="Effect"/> flags present in the bitmask. Returns an empty list if none are set.</returns>
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
    /// Encodes a list of <see cref="Effect"/> flags into an integer bitmask.
    /// </summary>
    /// <param name="effects">The list of effects to encode.</param>
    /// <returns>An integer bitmask representing the combined effects.</returns>
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
    /// Parses an integer bitmask to determine the single <see cref="HitObjectType"/> value.
    /// </summary>
    /// <param name="data">The bitmask that encodes hit object type flags.</param>
    /// <returns>The single <see cref="HitObjectType"/> parsed from the bitmask.</returns>
    /// <exception cref="Exception">Thrown when the bitmask does not represent exactly one HitObjectType.</exception>
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
    /// <param name="c">The character representing the curve type (e.g. 'C', 'B', 'L', 'P').</param>
    /// <returns>The corresponding <see cref="CurveType"/> value.</returns>
    public static CurveType ParseCurveType(char c) => c switch
    {
        'C' => CurveType.Catmull,
        'B' => CurveType.Bezier, 
        'L' => CurveType.Linear,
        _ => CurveType.PerfectCurve
    };

    /// <summary>
    /// Encodes a <see cref="CurveType"/> into a single-character string used in osu! files.
    /// </summary>
    /// <param name="curveType">The curve type to encode.</param>
    /// <returns>A single-character string representing the curve type.</returns>
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
    /// <param name="vectorString">The X,Y,Z components of the vector in the format X,Y,Z.</param>
    /// <returns>The parsed <see cref="Vector3"/> object.</returns>
    public static Vector3 ParseVector3(string vectorString)
    {
        string[] split = vectorString.Split(',');
        return new Vector3(float.Parse(split[0], CultureInfo.InvariantCulture),
            float.Parse(split[1], CultureInfo.InvariantCulture), float.Parse(split[2], CultureInfo.InvariantCulture));
    }
    
    /// <summary>
    /// Returns a Color from a string.
    /// </summary>
    /// <param name="colorString">The RGB colour from in the format R,G,B.</param>
    /// <returns>A <see cref="Color"/> instance representing the parsed color.</returns>
    public static Color ParseColor(string colorString)
    {
        string[] split = colorString.Split(',');
        return Color.FromArgb(255, int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
    }
    
    /// <summary>
    /// Parses a vector where components are hexadecimal values.
    /// </summary>
    /// <param name="vectorString">A comma-separated string of three hexadecimal values (e.g. "FF,00,10").</param>
    /// <returns>A <see cref="Vector3"/> with the parsed numeric components.</returns>
    public static Vector3 ParseVector3Hex(string vectorString)
    {
        string[] split = vectorString.Split(',');
        return new Vector3(int.Parse(split[0], NumberStyles.HexNumber),
            int.Parse(split[1], NumberStyles.HexNumber), int.Parse(split[2], NumberStyles.HexNumber));
    }

    /// <summary>
    /// Parses a string representing a vector in either decimal or hexadecimal format into a <see cref="Vector3"/> object.
    /// </summary>
    /// <param name="input">The input string containing three numeric components separated by commas, which can be in decimal or hexadecimal format.</param>
    /// <returns>A <see cref="Vector3"/> object representing the parsed vector.</returns>
    /// <exception cref="Exception">Thrown when the input string does not contain exactly three components or fails to parse.</exception>
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
    /// Parses a string representation of a color with uncertain format into a <see cref="Color"/> object.
    /// </summary>
    /// <param name="input">The input string containing color components, potentially in decimal or hexadecimal format.</param>
    /// <returns>A <see cref="Color"/> object representing the parsed color.</returns>
    /// <exception cref="Exception">Thrown when the input string format is invalid or cannot be parsed.</exception>
    public static Color ParseColorFromUnknownString(string input)
    {
        string[] split = input.Split(',', 3);

        if (split.Length != 3) throw new Exception("Not a valid vector3 string.");
        
        var R = IsNumeric(split[0]) ? int.Parse(split[0]) : int.Parse(split[0], NumberStyles.HexNumber);
        var G = IsNumeric(split[1]) ? int.Parse(split[1]) : int.Parse(split[1], NumberStyles.HexNumber);
        var B = IsNumeric(split[2]) ? int.Parse(split[2]) : int.Parse(split[2], NumberStyles.HexNumber);
        
        return Color.FromArgb(255, R, G, B);
    }

    /// <summary>
    /// Determines whether the given input can be interpreted as a numeric value.
    /// </summary>
    /// <param name="input">The string to evaluate.</param>
    /// <returns>True if the input is numeric; otherwise, false.</returns>
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
    /// <param name="eventLine">An array where the first element is the event type string or numeric code.</param>
    /// <returns>The parsed <see cref="EventType"/>.</returns>
    /// <exception cref="Exception">Thrown when the event type is invalid or unrecognized.</exception>
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
    /// Parses a command line to determine the corresponding <see cref="CommandType"/>.
    /// </summary>
    /// <param name="commandline">The string representing the command line to parse.</param>
    /// <returns>The parsed <see cref="CommandType"/> corresponding to the command line.</returns>
    /// <exception cref="Exception">Thrown when the command type in the input string is invalid or unrecognized.</exception>
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

    /// <summary>
    /// Parses a string line into an implementation of <see cref="ICommand"/> based on the command type.
    /// </summary>
    /// <param name="line">The string representation of the command to parse.</param>
    /// <returns>An instance of <see cref="ICommand"/> corresponding to the parsed command type.</returns>
    /// <exception cref="Exception">Thrown if the command type is unhandled or if an error occurs during parsing.</exception>
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
                _ => throw new Exception($"Unhandled command type '{identity}'"),
            };
            return commandDecoded;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while parsing command's ('{line}'): {ex}");
        }
    }
}
