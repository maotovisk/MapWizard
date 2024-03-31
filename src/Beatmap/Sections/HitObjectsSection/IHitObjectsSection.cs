

/// <summary>
/// 
/// </summary>
public interface IHitObjectsSection
{
    /// <summary>
    /// 
    /// </summary>
    public List<IHitObject> Objects { get; set; }
}

/// <summary>
///
/// </summary>
public interface ICircle : IHitObject
{

}

/// <summary>
///
/// </summary>
public interface ISpinner : IHitObject
{

}

/// <summary>
///
/// </summary>
public interface IManiaHold : IHitObject
{

}

/// <summary>
///
/// </summary>
public interface ISlider : IHitObject
{
    // curveType|curvePoints,slides,length,edgeSounds,edgeSets
}