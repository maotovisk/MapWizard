namespace MapWizard.BeatmapParser;

/// <summary>
/// Defines the loop behavior options for animations in the application.
/// </summary>
public enum LoopType : int
{
    /// <summary>
    /// Indicates that an animation should loop indefinitely without stopping.
    /// This looping behavior ensures that the animation will continuously replay
    /// from the beginning as soon as it reaches the end.
    /// </summary>
    LoopForever = 0,

    /// <summary>
    /// Indicates that an animation should play through its sequence of frames exactly once.
    /// Once the animation reaches the end, it stops and does not replay.
    /// </summary>
    LoopOnce = 1,
}