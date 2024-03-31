
using Beatmap;

/// <summary>
/// This is a class that represents any event in a beatmap.
/// For now, it's a workaround for the lack of implementation of event type parsing.
/// </summary>
public class GeneralEvent : IGeneralEvent
{
    /// <summary>
    /// The type of the event, represented by the <see cref="EventType"/> enum.
    /// </summary>
    public EventType Type { get; set; }

    /// <summary>
    /// The parameters of the event.
    /// </summary>
    public List<string> Params { get; set; }


    /// <summary>
    /// The time at which the event occurs.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralEvent"/> class.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="eventParams"></param>
    /// <param name="time"></param>
    public GeneralEvent(EventType type, List<string> eventParams, TimeSpan time)
    {
        Params = eventParams;
        Type = type;
        Time = time;
    }
}