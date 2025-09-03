namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the origin point of a graphical object within its container.
/// </summary>
public enum Origin : int
{
    TopLeft = 0,
    Centre = 1,
    CentreLeft = 2,
    TopRight = 3,
    BottomCentre = 4,
    TopCentre = 5,
    Custom = 6,
    CentreRight = 7,
    BottomLeft = 8,
    BottomRight = 9,
}