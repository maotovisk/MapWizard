using System;
using Avalonia.Rendering.Composition;

namespace MapWizard.Theme.Motion;

/// <summary>
/// Applies a short implicit compositor animation to scroll offsets.
/// </summary>
public static class Scrollable
{
    public static void MakeScrollable(CompositionVisual visual, TimeSpan? duration = null)
    {
        var compositor = visual.Compositor;
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Target = "Offset";
        offsetAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue");
        offsetAnimation.Duration = duration ?? TimeSpan.FromMilliseconds(250);

        var animationGroup = compositor.CreateAnimationGroup();
        animationGroup.Add(offsetAnimation);

        var animations = compositor.CreateImplicitAnimationCollection();
        animations["Offset"] = animationGroup;
        visual.ImplicitAnimations = animations;
    }
}
