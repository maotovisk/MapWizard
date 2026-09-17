using System;
using Avalonia.Animation.Easings;
using Avalonia.Rendering.Composition;

namespace MapWizard.Theme.Motion;

/// <summary>
/// Applies a short implicit compositor animation to scroll offsets.
/// </summary>
public static class Scrollable
{
    /// <summary>
    /// Short enough to react immediately while still softening discrete wheel detents.
    /// The exponential curve is the closed-form equivalent of repeatedly lerping
    /// toward the target and does not require a UI-thread timer per frame.
    /// </summary>
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(180);

    public static void MakeScrollable(CompositionVisual visual, TimeSpan? duration = null)
    {
        var compositor = visual.Compositor;
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Target = "Offset";
        offsetAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", new ExponentialEaseOut());
        offsetAnimation.Duration = duration ?? DefaultDuration;

        var animationGroup = compositor.CreateAnimationGroup();
        animationGroup.Add(offsetAnimation);

        var animations = compositor.CreateImplicitAnimationCollection();
        animations["Offset"] = animationGroup;
        visual.ImplicitAnimations = animations;
    }
}
