namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public enum Layer : int
{
    /// <summary>
    /// 
    /// </summary>
    Background = 0,

    /// <summary>
    /// 
    /// </summary>
    Fail = 1,

    /// <summary>
    /// 
    /// </summary>
    Pass = 2,

    /// <summary>
    /// 
    /// </summary>
    Foreground = 3,

    /// <summary>
    /// 
    /// </summary>
    Video = 4,

    Overlay = int.MinValue
}