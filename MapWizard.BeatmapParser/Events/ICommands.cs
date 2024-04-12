namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public interface IHasCommands
{
    /// <summary>
    /// 
    /// </summary>
    public List<ICommand> Commands { get; set; }
}