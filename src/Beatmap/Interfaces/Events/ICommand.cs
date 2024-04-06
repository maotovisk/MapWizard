namespace BeatmapParser;

/// <summary>
/// 
/// </summary>
public interface ICommand : IEncodable
{
    /// <summary>
    /// 
    /// </summary>
    public CommandTypes Type { get; init; }
}