namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public interface ICommand : IEncodable
{
    /// <summary>
    /// 
    /// </summary>
    public CommandType Type { get; init; }
}