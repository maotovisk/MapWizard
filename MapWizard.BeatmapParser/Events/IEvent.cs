namespace MapWizard.BeatmapParser;
/// <summary>
///
/// </summary>
public interface IEvent : IEncodable
{
    /// <summary>
    /// 
    /// </summary>
    public EventType Type { get; init; }
}