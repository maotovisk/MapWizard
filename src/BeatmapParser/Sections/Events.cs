using System.Text;

namespace MapWizard.BeatmapParser.Sections;

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
        List<IEvent> events = [];
        try
        {
            for (var index = 0; index < section.Count; index++)
            {
                if (section[index].StartsWith("//")) continue;

                var eventSplit = section[index].Split(',', 2);
                if (eventSplit.Length != 2) throw new Exception("Invalid event length");

                var eventIdentity = Helper.ParseEventType(eventSplit);
                var eventType = Helper.GetEventType(eventIdentity);

                var decodeFunction = eventType.GetMethod("Decode") ?? throw new Exception($"{eventType.Name} is missing 'Decode' method.");

                if (!eventType.GetInterfaces().Contains(typeof(ICommands)))
                {
                    var normalEvent = decodeFunction.Invoke(null, [section[index]]) ?? throw new Exception($"Failed to 'Decode()' event with type '{eventType.Name}'.");
                    events.Add((IEvent)normalEvent);
                    continue;
                }

                List<string> commands = [];
                while (index < section.Count)
                {
                    if (!section[index].StartsWith(' ') || !section[index].StartsWith('_')) break;
                    commands.Add(section[index]);
                    index++;
                }

                var eventObj = decodeFunction.Invoke(null, [section[index], commands]) ?? throw new Exception($"Failed to 'Decode' event with type '{eventType.Name}'");
                events.Add((IEvent)eventObj);
            }

            return new Events(events);
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
        StringBuilder builder = new();

        foreach (var eventItem in EventList)
        {
            var encodeInfo = eventItem.GetType().GetMethod("Encode") ?? throw new Exception($"{eventItem} do not have method \'Encode\'.");
            var encodeResult = encodeInfo.Invoke(null, null) ?? throw new Exception($"Failed to \'Encode\' event at \'{eventItem}\'");
            builder.AppendLine((string)encodeResult);
        }
        return builder.ToString();
    }
}