namespace Beatmap;
/// <summary>
/// Event class.
/// </summary>
public class Event : IEvent
{
    /// <summary>
    ///  Time of the event.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    ///  Initializes a new instance of the <see cref="Event"/> class.
    /// </summary>
    /// <param name="time"></param>
    public Event(TimeSpan time)
    {
        Time = time;
    }

    public Events FromData(ref Beatmap beatmap, List<string> section)
    {
        beatmap.Events = section.Select(x =>
        {
            var split = x.Split(',');
            return new Event
            {
                StartTime = int.Parse(split[0]),
                Layer = int.Parse(split[1]),
                EventType = (EventType)Enum.Parse(typeof(EventType), split[2]),
                Parameters = split.Skip(3).ToList()
            };
        }).ToList();
    }
}