using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a break event in a beatmap. A break defines a period during which gameplay is paused.
/// </summary>
public class Break : IEvent
{
    private double _startMilliseconds;
    private double _endMilliseconds;

    /// <summary>
    /// Represents the <see cref="EventType"/> of the event, indicating the category or nature of the beatmap event.
    /// </summary>
    public EventType Type { get; init; } = EventType.Break;

    /// <summary>
    /// The start time of the break period.
    /// </summary>
    public TimeSpan StartTime
    {
        get => TimeSpan.FromMilliseconds(_startMilliseconds);
        set => _startMilliseconds = value.TotalMilliseconds;
    }

    /// <summary>
    /// The end time of the break period.
    /// </summary>
    public TimeSpan EndTime
    {
        get => TimeSpan.FromMilliseconds(_endMilliseconds);
        set => _endMilliseconds = value.TotalMilliseconds;
    }

    /// <summary>
    /// Represents a break event in a beatmap. A break defines a period during which gameplay is paused.
    /// </summary>
    private Break(double startMilliseconds, double endMilliseconds)
    {
        _startMilliseconds = startMilliseconds;
        _endMilliseconds = endMilliseconds;
    }

    /// <summary>
    /// Represents a break event in a beatmap. A break defines a period during which gameplay is paused.
    /// </summary>
    private Break()
    {
        StartTime = TimeSpan.FromMilliseconds(0);
        _endMilliseconds = 0;
    }

    /// <summary>
    /// Encodes the break event into its string representation for use within beatmap files.
    /// </summary>
    /// <returns>
    /// A string representing the break event, including its type, start time, and end time.
    /// </returns>
    public string Encode()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{(int)EventType.Break}");
        sb.Append(',');
        sb.Append(_startMilliseconds.ToString(CultureInfo.InvariantCulture));
        sb.Append(',');
        sb.Append(_endMilliseconds.ToString(CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    /// <summary>
    /// Decodes a string representation of a break event into a <see cref="Break"/> object.
    /// The string must be in a specific format indicating the start time and end time of the event.
    /// </summary>
    /// <param name="line">The raw string representation of the break event. It is expected to contain the event type identifier and time range.</param>
    /// <returns>A <see cref="Break"/> object initialized with the parsed start and end time values from the input string.</returns>
    /// <exception cref="Exception">Thrown when the input string is improperly formatted or parsing fails.</exception>
    public static Break Decode(string line)
    {
        try
        {
            var args = line.Trim().Split(',');
            var start = double.Parse(args[1], CultureInfo.InvariantCulture);
            var end = double.Parse(args[2], CultureInfo.InvariantCulture);
            return new Break(startMilliseconds: start, endMilliseconds: end);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Break line: '{line}' - {ex.Message}\n{ex.StackTrace}");
        }
    }
}
