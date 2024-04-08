using System.Numerics;
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
    /// Gets the current depth of a event line.
    /// </summary>
    /// <param name="lineRaw"></param>
    /// <returns></returns> 
    private static int GetDepth(string lineRaw)
    {
        int depth = 0;
        foreach (char c in lineRaw)
        {
            if (c != ' ' && c != '_') break;
            ++depth;
        }
        return depth;
    }

    /// <summary>
    /// Converts a list of strings to a <see cref="Events"/> object.
    /// </summary>
    /// <param name="sectionRaw"></param>
    /// <returns></returns>
    public static Events Decode(List<string> sectionRaw)
    {
        try
        {
            var section = sectionRaw.Where(x => !x.Trim().StartsWith("//")).ToList();

            List<IEvent> events = [];

            for (var index = 0; index < section.Count; ++index)
            {
                var lineRaw = section[index];
                int depth = GetDepth(lineRaw);
                var line = lineRaw;

                if (depth == 0)
                {
                    var eventSplit = line.Split(',', 2);
                    if (eventSplit.Length != 2) throw new Exception("Invalid event length");

                    IEvent @event = Helper.ParseEventType(eventSplit) switch
                    {
                        EventTypes.Background => Background.Decode(line),
                        EventTypes.Video => Video.Decode(line),
                        EventTypes.Break => Break.Decode(line),
                        EventTypes.Sample => Sample.Decode(line),
                        EventTypes.Sprite => Sprite.Decode(line),
                        EventTypes.Animation => Animation.Decode(line),
                        _ => throw new Exception($"Unhandled event with identification '{eventSplit[0]}'."),
                    };
                    events.Add(@event);
                    continue;
                }

                if (events.Last() is not ICommands currentEvent) throw new Exception($"this event \'{events.Last().Type}\' do not support commands");

                index = AddCommands(currentEvent, section, index, depth) - 1;
            }

            return new Events(events);
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not parse event lines: {ex}");
        }
    }

    private static int AddCommands(ICommands currentEvent, List<string> sectionRaw, int index, int depth)
    {
        var tempindex = index;

        while (tempindex < sectionRaw.Count)
        {
            var currentdepth = GetDepth(sectionRaw[tempindex]);
            if (currentdepth > depth)
            {
                Console.WriteLine($"{depth} > | index: {tempindex}, ");
                if (currentEvent.Commands.Last() is not ICommands) throw new Exception($"{currentEvent.Commands.Last()} do not support commands");

                tempindex = AddCommands((ICommands)currentEvent.Commands.Last(), sectionRaw, tempindex, currentdepth);
                continue;
            }

            if (currentdepth < depth) break;

            if (currentdepth == depth)
            {
                currentEvent.Commands.Add(Helper.ParseCommand(sectionRaw[tempindex]));
                ++tempindex;
            }
        }
        return tempindex;
    }

    /// <summary>
    /// Encodes the <see cref="Events"/> section to a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        builder.AppendLine("//Background and Video events");

        var backgroundEvents = EventList.OfType<Background>().ToList();
        foreach (var backgroundEvent in backgroundEvents) builder.AppendLine(backgroundEvent.Encode());

        var videoEvents = EventList.OfType<Video>().ToList();
        foreach (var videoEvent in videoEvents) builder.AppendLine(videoEvent.Encode());

        builder.AppendLine("//Break Periods");
        var breakEvents = EventList.OfType<Break>().ToList();
        foreach (var breakEvent in breakEvents) builder.AppendLine(breakEvent.Encode());

        var layeredEvents = EventList.OfType<ILayeredEvent>().ToList();

        builder.AppendLine("//Storyboard Layer 0 (Background)");
        foreach (var eventItem in layeredEvents.Where(x => x.Layer == Layer.Background)) EncodeEvent((IEvent)eventItem, builder);

        builder.AppendLine("//Storyboard Layer 1 (Fail)");
        foreach (var eventItem in layeredEvents.Where(x => x.Layer == Layer.Fail)) EncodeEvent((IEvent)eventItem, builder);

        builder.AppendLine("//Storyboard Layer 2 (Pass)");
        foreach (var eventItem in layeredEvents.Where(x => x.Layer == Layer.Pass)) EncodeEvent((IEvent)eventItem, builder);

        builder.AppendLine("//Storyboard Layer 3 (Foreground)");
        foreach (var eventItem in layeredEvents.Where(x => x.Layer == Layer.Foreground)) EncodeEvent((IEvent)eventItem, builder);

        builder.AppendLine("//Storyboard Layer 4 (Overlay)");
        foreach (var eventItem in layeredEvents.Where(x => x.Layer == Layer.Overlay)) EncodeEvent((IEvent)eventItem, builder);

        var sampleEvents = EventList.OfType<Sample>().ToList();

        builder.Append("//Storyboard Sound Samples");
        foreach (var eventItem in sampleEvents) EncodeEvent((IEvent)eventItem, builder);

        return builder.ToString();
    }

    private void EncodeEvent(IEvent eventItem, StringBuilder builder)
    {
        var encodeInfo = eventItem.GetType().GetMethod("Encode") ?? throw new Exception($"{eventItem} do not have method \'Encode\'.");
        var encodeResult = encodeInfo.Invoke(eventItem, null) ?? throw new Exception($"Failed to \'Encode\' event at \'{eventItem}\'");
        builder.Append((string)encodeResult);
    }
}