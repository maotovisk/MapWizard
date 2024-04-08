using System.Globalization;
using System.Numerics;

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

        foreach (HitSound name in Enum.GetValues(typeof(HitSound)))
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
        foreach (Effect name in Enum.GetValues(typeof(Effect)))
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
        foreach (HitObjectType name in Enum.GetValues(typeof(HitObjectType)))
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
    /// Parses a Event type from a string array.
    /// </summary>
    /// <param name="eventLine"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static EventTypes ParseEventType(string[] eventLine)
    {

        string eventType = eventLine[0].Trim();

        foreach (EventTypes type in Enum.GetValues(typeof(EventType)))
        {
            if (type.ToString().Equals(eventType, StringComparison.CurrentCultureIgnoreCase) ||
                ((int)type).ToString().Equals(eventType, StringComparison.CurrentCultureIgnoreCase))
                return type;
        }

        throw new Exception($"Invalid event type: {eventType}");
    }

    /// <summary>
    /// Returns the <see cref="Type"/> of an event.
    /// </summary>
    /// <param name="eventIdentity"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>

    public static Type GetEventType(EventTypes eventIdentity)
    {
        return eventIdentity switch
        {
            EventTypes.Background => typeof(Background),
            EventTypes.Video => typeof(Video),
            EventTypes.Break => typeof(Break),
            EventTypes.Sample => typeof(Sample),
            EventTypes.Sprite => typeof(Sprite),
            EventTypes.Animation => typeof(Animation),
            _ => throw new Exception($"Unhandled event with identification '{eventIdentity}'."),
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commandLine"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static CommandTypes ParseCommandType(string commandLine)
    {
        var commandType = (int)commandLine.Trim().Last();

        foreach (CommandTypes type in Enum.GetValues(typeof(CommandTypes)))
        {
            if ((int)type == commandType) return type;
        }
        throw new Exception($"Invalid command type: {commandType}");
    }


    public static ICommand ParseCommand(List<ICommand> parsedCommands, List<string> commands, int commandindex)
    {
        CommandTypes identity = ParseCommandType(commands[commandindex]);
        ICommand commandDecoded = identity switch
        {
            CommandTypes.Fade => Fade.Decode(parsedCommands, commands, commandindex),
            CommandTypes.Move => Move.Decode(parsedCommands, commands, commandindex),
            CommandTypes.Scale => Scale.Decode(parsedCommands, commands, commandindex),
            CommandTypes.Rotate => Rotate.Decode(parsedCommands, commands, commandindex),
            CommandTypes.Colour => Colour.Decode(parsedCommands, commands, commandindex),
            CommandTypes.Parameter => Parameter.Decode(parsedCommands, commands, commandindex),
            _ => throw new Exception($"Unhandled command type \'{identity}\'"),
        };
        return commandDecoded;
    }
}
