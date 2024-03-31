namespace Beatmap.Events;
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
    public string Type { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public List<string> Params { get; set; }
}