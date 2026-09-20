using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Rendering.Composition;

namespace MapWizard.Desktop.Transitions;

/// <summary>
/// Scales and fades the old page away before bringing the new page forward.
/// Scale and opacity stay on the compositor path to avoid redrawing page content.
/// </summary>
public sealed class ScaleFadeTransition : IPageTransition
{
    private readonly SemaphoreSlim _transitionGate = new(1, 1);

    public TimeSpan OutDuration { get; set; } = TimeSpan.FromMilliseconds(160);

    public TimeSpan InDuration { get; set; } = TimeSpan.FromMilliseconds(300);

    public double InStartScale { get; set; } = 0.98d;

    public double OutEndScale { get; set; } = 1.02d;

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        // TransitioningContentControl can start a replacement transition before
        // the canceled transition has restored its compositor state.
        await _transitionGate.WaitAsync(cancellationToken);

        try
        {
            // The incoming presenter is already in the visual tree when Start is
            // called, so hide it before the outgoing page begins to animate.
            if (to is not null)
            {
                ResetVisualState(to);
                to.IsVisible = false;
            }

            if (from is not null)
            {
                await AnimateAsync(
                    from,
                    OutDuration,
                    new CubicEaseIn(),
                    startOpacity: 1d,
                    endOpacity: 0d,
                    startScale: 1d,
                    endScale: OutEndScale,
                    revealBeforeAnimation: false,
                    cancellationToken);
                from.IsVisible = false;
            }

            if (to is not null)
            {
                await AnimateAsync(
                    to,
                    InDuration,
                    new CubicEaseOut(),
                    startOpacity: 0d,
                    endOpacity: 1d,
                    startScale: InStartScale,
                    endScale: 1d,
                    revealBeforeAnimation: true,
                    cancellationToken);
            }
        }
        finally
        {
            if (from is not null)
            {
                ResetVisualState(from);
            }

            if (to is not null)
            {
                ResetVisualState(to);
                to.IsVisible = true;
            }

            _transitionGate.Release();
        }
    }

    private static void ResetVisualState(Visual visual)
    {
        visual.Opacity = 1d;
        visual.RenderTransformOrigin = RelativePoint.Center;
        visual.RenderTransform = new ScaleTransform(1d, 1d);

        if (ElementComposition.GetElementVisual(visual) is { } compositionVisual)
        {
            compositionVisual.StopAnimation("Opacity");
            compositionVisual.StopAnimation("Scale");
            compositionVisual.Opacity = 1f;
            compositionVisual.Scale = Vector3.One;
        }
    }

    private static async Task AnimateAsync(
        Visual target,
        TimeSpan duration,
        Easing easing,
        double startOpacity,
        double endOpacity,
        double startScale,
        double endScale,
        bool revealBeforeAnimation,
        CancellationToken cancellationToken)
    {
        var compositionVisual = ElementComposition.GetElementVisual(target);
        if (compositionVisual is null)
        {
            await AnimateFallbackAsync(
                target,
                duration,
                easing,
                startOpacity,
                endOpacity,
                startScale,
                endScale,
                revealBeforeAnimation,
                cancellationToken);
            return;
        }

        var previousOpacity = compositionVisual.Opacity;
        var previousScale = compositionVisual.Scale;
        var previousCenterPoint = compositionVisual.CenterPoint;
        var startScaleVector = new Vector3((float)startScale, (float)startScale, 1f);
        var endScaleVector = new Vector3((float)endScale, (float)endScale, 1f);

        compositionVisual.StopAnimation("Opacity");
        compositionVisual.StopAnimation("Scale");
        compositionVisual.Opacity = (float)startOpacity;
        compositionVisual.Scale = startScaleVector;
        compositionVisual.CenterPoint = new Vector3(
            (float)(target.Bounds.Width / 2d),
            (float)(target.Bounds.Height / 2d),
            0f);

        if (revealBeforeAnimation)
        {
            target.IsVisible = true;
        }

        StartCompositionAnimation(
            compositionVisual,
            duration,
            easing,
            (float)startOpacity,
            (float)endOpacity,
            startScaleVector,
            endScaleVector);

        try
        {
            await Task.Delay(duration, cancellationToken);
        }
        finally
        {
            compositionVisual.StopAnimation("Opacity");
            compositionVisual.StopAnimation("Scale");
            compositionVisual.Opacity = previousOpacity;
            compositionVisual.Scale = previousScale;
            compositionVisual.CenterPoint = previousCenterPoint;
        }
    }

    private static void StartCompositionAnimation(
        CompositionVisual visual,
        TimeSpan duration,
        Easing easing,
        float startOpacity,
        float endOpacity,
        Vector3 startScale,
        Vector3 endScale)
    {
        var opacity = visual.Compositor.CreateScalarKeyFrameAnimation();
        opacity.Target = "Opacity";
        opacity.Duration = duration;
        opacity.InsertKeyFrame(0f, startOpacity);
        opacity.InsertKeyFrame(1f, endOpacity, easing);
        visual.StartAnimation("Opacity", opacity);

        var scale = visual.Compositor.CreateVector3KeyFrameAnimation();
        scale.Target = "Scale";
        scale.Duration = duration;
        scale.InsertKeyFrame(0f, startScale);
        scale.InsertKeyFrame(1f, endScale, easing);
        visual.StartAnimation("Scale", scale);
    }

    private static async Task AnimateFallbackAsync(
        Visual target,
        TimeSpan duration,
        Easing easing,
        double startOpacity,
        double endOpacity,
        double startScale,
        double endScale,
        bool revealBeforeAnimation,
        CancellationToken cancellationToken)
    {
        var scale = new ScaleTransform(startScale, startScale);
        target.Opacity = startOpacity;
        target.RenderTransformOrigin = RelativePoint.Center;
        target.RenderTransform = scale;

        if (revealBeforeAnimation)
        {
            target.IsVisible = true;
        }

        var steps = Math.Max(1, (int)Math.Ceiling(duration.TotalMilliseconds / 16d));
        var stepDuration = TimeSpan.FromTicks(duration.Ticks / steps);

        for (var step = 1; step <= steps; step++)
        {
            var progress = easing.Ease((double)step / steps);
            var currentScale = startScale + ((endScale - startScale) * progress);
            target.Opacity = startOpacity + ((endOpacity - startOpacity) * progress);
            scale.ScaleX = currentScale;
            scale.ScaleY = currentScale;
            await Task.Delay(stepDuration, cancellationToken);
        }
    }
}
