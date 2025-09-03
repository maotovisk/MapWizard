namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents an entity that contains a collection of commands.
/// </summary>
public interface IHasCommands
{
    /// <summary>
    /// Gets or sets the collection of commands associated with the object.
    /// </summary>
    public List<ICommand> Commands { get; set; }
}