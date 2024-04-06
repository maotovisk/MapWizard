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
            for (var index = 0; index != section.Count; ++index)
            {
                if (section[index].StartsWith("//")) continue;

                var eventSplit = section[index].Split(',', 2);
                if(eventSplit.Length != 2) throw new Exception("invalid event length");
                var eventIndentification = (EventTypes)Enum.Parse(typeof(EventTypes), eventSplit[1].Trim());

                Type eventType = eventIndentification switch
                {
                    EventTypes.Background => typeof(Background),
                    EventTypes.Video => typeof(Video),
                    EventTypes.Break => typeof(Break),
                    _ => throw new Exception($"Unhandled event with indentification \'{eventIndentification}\'."),
                };

                var decodeFunction = eventType.GetType().GetMethod("Decode") ?? throw new Exception($"{eventType.Name} is missing \'Decode\' method.");

                // Some events dont have commands and need to be parsed diferently
                if (!eventType.GetInterfaces().Contains(typeof(ICommand)))
                {
                    var normalEvent = decodeFunction.Invoke(null, [eventSplit]) ?? throw new Exception($"Failed to \'Decode()\' event with type \'{eventType.Name}\'.");
                    events.Add((IEvent)normalEvent);
                    continue;
                }

                List<string> commands = [];
                while (index != section.Count)
                {
                    var commandSplit = eventSplit[1].Split(',', 2);
                    if(commandSplit.Length != 2) throw new Exception("invalid command length");
                    var commandIndentification = (int)commandSplit[0].Trim().Last();

                    bool isCommand = false;
                    
                    if(!isCommand) break;
                    
                    commands.Add(section[index]);
                    ++index;
                }

                var eventObj = decodeFunction.Invoke(null, [eventSplit, commands]) ?? throw new Exception($"Failed to \'Decode\' event with type \'{eventType.Name}\'");
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

        foreach (var _event in EventList)
        {
            var EncodeInfo = _event.GetType().GetMethod("Encode") ?? throw new Exception($"{_event} do not have method \'Encode\'.");
            var EncodeResult = EncodeInfo.Invoke(null, null) ?? throw new Exception($"Failed to \'Encode\' event at \'{_event}\'");
            builder.AppendLine((string)EncodeResult);
        }
        return builder.ToString();
    }
}