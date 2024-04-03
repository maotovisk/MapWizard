using System.Globalization;

namespace BeatmapParser;

/// <summary>
/// This is a class that represents any event in a beatmap.
/// For now, it's a workaround for the lack of implementation of event type parsing.
/// </summary>
public class GeneralEvent : IGeneralEvent
{
    /// <summary>
    /// The type of the event, represented by the <see cref="EventType"/> enum.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The parameters of the event.
    /// </summary>
    public List<string> Params { get; set; }

    /// <summary>
    /// The time at which the event occurs, if applicable.
    /// </summary>
    public TimeSpan? Time { get; set; }


    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralEvent"/> class.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="eventParams"></param>
    public GeneralEvent(string type, List<string> eventParams)
    {
        Params = eventParams;
        Type = type;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralEvent"/> class.
    /// For now it's a workaround for the lack of implementation of event type parsing.
    /// So it's just a string and a list of strings.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static GeneralEvent Decode(string line)
    {
        try
        {
            var split = line.Split(',');

            var type = split[0];
            var parameters = new List<string>();

            parameters.AddRange(split[1..]);
            return new GeneralEvent(type, parameters);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to GeneralEvent line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}