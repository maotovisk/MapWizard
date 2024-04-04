using System.Text;
using System.Text.Json.Serialization;

namespace BeatmapParser.Sections;

/// <summary>
/// Represents the events section of a beatmap.
/// </summary>
public class Events : IEvents
{
    /// <summary>
    /// Represents the list of events in the beatmap.
    /// </summary>
    public List<IEvent> EventList { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Events"/> class.
    /// </summary>
    public Events()
    {
        EventList = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Events"/> class with the specified parameters.
    /// </summary>
    /// <param name="eventList"></param>
    public Events(List<IEvent> eventList)
    {
        EventList = eventList;
    }
    /// <summary>
    /// Converts a list of strings to a <see cref="Events"/> object.
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public static Events Decode(List<string> section)
    {
        List<IEvent> result = [];
        try
        {
            section.ForEach(sectionLine =>
            {
                if (sectionLine.StartsWith("//"))
                    return;

                result.Add(GeneralEvent.Decode(sectionLine));
            });

            return new Events(result);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while parsing events\n{ex}");
        }
    }

    /// <summary>
    /// Encodes the <see cref="Events"/> section to a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder sb = new();

        foreach (var ev in EventList)
        {
            if (ev is GeneralEvent @event)
                sb.AppendLine($"{@event.Type},{string.Join(",", @event.Params)}");
        }

        return sb.ToString();
    }
}