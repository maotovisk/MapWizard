namespace Beatmap;
/// <summary>
///
/// </summary>
public interface IEvent
{
    /// <summary>
    /// 
    /// </summary>
    TimeSpan Time { get; set; }
}

/// <summary>
///
/// </summary>
public interface IGeneralEvent : IEvent
{
    /// <summary>
    /// 
    /// </summary>
    public EventType Type { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<string> Params { get; set; }
}