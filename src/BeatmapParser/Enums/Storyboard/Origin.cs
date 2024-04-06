namespace MapWizard.BeatmapParser;

/// <summary>
/// 
/// </summary>
public enum Origin : int
{
    /// <summary>
    /// x = 0, y = 0
    /// </summary>
    TopLeft = 0,
    
    /// <summary>
    /// x = 320, y = 240
    /// </summary>
    Centre = 1,
    
    /// <summary>
    /// 
    /// </summary>
    CentreLeft = 2,
    
    /// <summary>
    /// 
    /// </summary>
    TopRight = 3,
    
    /// <summary>
    /// 
    /// </summary>
    BottomCentre = 4,
    
    /// <summary>
    /// 
    /// </summary>
    TopCentre = 5,
    
    /// <summary>
    /// 
    /// </summary>
    Custom = 6, // (same effect as TopLeft, but should not be used)
    
    /// <summary>
    /// x = 640, y = 480
    /// </summary>
    CentreRight = 7,
    
    /// <summary>
    /// 
    /// </summary>
    BottomLeft = 8,
    
    /// <summary>
    /// 
    /// </summary>
    BottomRight = 9,
}