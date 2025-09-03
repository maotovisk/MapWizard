namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a base interface for all command types within the system, providing a mechanism for encoding data.
/// Classes implementing this interface define specific command behaviors and are initialized with a specific command type.
/// </summary>
public interface ICommand : IEncodable
{
    /// <summary>
    /// Gets the specific type of the command, which determines its behavior
    /// within the mapping framework. The value is defined by the <see cref="CommandType"/> enumeration.
    /// </summary>
    public CommandType Type { get; init; }
}