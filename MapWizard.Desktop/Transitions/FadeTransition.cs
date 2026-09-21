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
/// Opacity-only page transition used when Reduced Motion is enabled: a short
/// fade out followed by a fade in, with no scale or movement. Opacity stays on
/// the compositor path to avoid redrawing page content.
/// </summary>
public sealed class FadeTransition : IPageTransition
{
    private readonly SemaphoreSlim _transitionGate = new(1, 1);

    public TimeSpan OutDuration { get; set; } = TimeSpan.FromMilliseconds(120);

    public TimeSpan InDuration { get; set; } = TimeSpan.FromMilliseconds(160);

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        // First page load has no outgoing page: show the content immediately.
        if (from is null)
        {
            if (to is not null)
            {
                ResetVisualState(to);
                to.IsVisible = true;
            }

            return;
        }

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
                await AnimateOpacityAsync(from, OutDuration, 1d, 0d, revealBeforeAnimation: false, cancellationToken);
                from.IsVisible = false;
            }

            if (to is not null)
            {
                await AnimateOpacityAsync(to, InDuration, 0d, 1d, revealBeforeAnimation: true, cancellationToken);
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

    private static async Task AnimateOpacityAsync(
        Visual target,
        TimeSpan duration,
        double startOpacity,
        double endOpacity,
        bool revealBeforeAnimation,
        CancellationToken cancellationToken)
    {
        var easing = new QuadraticEaseOut();
        var compositionVisual = ElementComposition.GetElementVisual(target);
        if (compositionVisual is null)
        {
            await AnimateFallbackAsync(target, duration, easing, startOpacity, endOpacity, revealBeforeAnimation, cancellationToken);
            return;
        }

        var previousOpacity = compositionVisual.Opacity;
        compositionVisual.StopAnimation("Opacity");
        compositionVisual.Opacity = (float)startOpacity;

        if (revealBeforeAnimation)
        {
            target.IsVisible = true;
        }

        var opacity = compositionVisual.Compositor.CreateScalarKeyFrameAnimation();
        opacity.Target = "Opacity";
        opacity.Duration = duration;
        opacity.InsertKeyFrame(0f, (float)startOpacity);
        opacity.InsertKeyFrame(1f, (float)endOpacity, easing);
        compositionVisual.StartAnimation("Opacity", opacity);

        try
        {
            await Task.Delay(duration, cancellationToken);
        }
        finally
        {
            compositionVisual.StopAnimation("Opacity");
            compositionVisual.Opacity = previousOpacity;
        }
    }

    private static async Task AnimateFallbackAsync(
        Visual target,
        TimeSpan duration,
        Easing easing,
        double startOpacity,
        double endOpacity,
        bool revealBeforeAnimation,
        CancellationToken cancellationToken)
    {
        target.Opacity = startOpacity;

        if (revealBeforeAnimation)
        {
            target.IsVisible = true;
        }

        var steps = Math.Max(1, (int)Math.Ceiling(duration.TotalMilliseconds / 16d));
        var stepDuration = TimeSpan.FromTicks(duration.Ticks / steps);

        for (var step = 1; step <= steps; step++)
        {
            var progress = easing.Ease((double)step / steps);
            target.Opacity = startOpacity + ((endOpacity - startOpacity) * progress);
            await Task.Delay(stepDuration, cancellationToken);
        }
    }
}
