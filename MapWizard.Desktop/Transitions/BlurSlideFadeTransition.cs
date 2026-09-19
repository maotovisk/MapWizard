using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;

namespace MapWizard.Desktop.Transitions;

/// <summary>
/// Page transition that blurs and slides the old page out while fading,
/// then unblurs and settles the new page in. All animated properties are
/// render-only; the blur is interpolated as an Avalonia effect and rendered
/// by the Skia backend.
/// </summary>
public sealed class BlurSlideFadeTransition : IPageTransition
{
    private readonly SemaphoreSlim _transitionGate = new(1, 1);

    public TimeSpan OutDuration { get; set; } = TimeSpan.FromMilliseconds(160);

    public TimeSpan InDuration { get; set; } = TimeSpan.FromMilliseconds(300);

    public double SlideDistance { get; set; } = 32d;

    public double MaxBlurRadius { get; set; } = 9d;

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        // TransitioningContentControl can start the replacement transition before
        // the canceled transition has unwound. Let the old transition restore its
        // compositor state first so it cannot leave (or later reapply) an offset.
        await _transitionGate.WaitAsync(cancellationToken);

        var direction = forward ? 1d : -1d;

        try
        {
            // TransitioningContentControl adds the incoming presenter before calling Start.
            // Hide it synchronously so it cannot paint a frame over the outgoing page.
            if (to is not null)
            {
                ResetVisualState(to);
                to.IsVisible = false;
            }

            if (from is not null)
            {
                await AnimateOutAsync(from, direction, cancellationToken);
                from.Effect = null;
                from.IsVisible = false;
            }

            if (to is not null)
            {
                await AnimateInAsync(to, direction, cancellationToken);
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
        visual.Effect = null;
        visual.Opacity = 1d;
        visual.RenderTransform = new TranslateTransform(0d, 0d);

        if (ElementComposition.GetElementVisual(visual) is { } compositionVisual)
        {
            compositionVisual.StopAnimation("Opacity");
            compositionVisual.StopAnimation("Translation");
            compositionVisual.Opacity = 1f;
            compositionVisual.Translation = default;
        }
    }

    private async Task AnimateOutAsync(Visual from, double direction, CancellationToken cancellationToken)
    {
        await AnimateAsync(
                from,
                OutDuration,
                new CubicEaseIn(),
                startOpacity: 1d,
                endOpacity: 0d,
                startOffset: 0d,
                endOffset: -direction * SlideDistance,
                startBlurRadius: 0d,
                endBlurRadius: MaxBlurRadius,
                revealBeforeAnimation: false,
                cancellationToken);
    }

    private async Task AnimateInAsync(Visual to, double direction, CancellationToken cancellationToken)
    {
        await AnimateAsync(
                to,
                InDuration,
                new CubicEaseOut(),
                startOpacity: 0d,
                endOpacity: 1d,
                startOffset: direction * SlideDistance,
                endOffset: 0d,
                startBlurRadius: MaxBlurRadius,
                endBlurRadius: 0d,
                revealBeforeAnimation: true,
                cancellationToken);
    }

    private static async Task AnimateAsync(
        Visual target,
        TimeSpan duration,
        Easing easing,
        double startOpacity,
        double endOpacity,
        double startOffset,
        double endOffset,
        double startBlurRadius,
        double endBlurRadius,
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
                startOffset,
                endOffset,
                startBlurRadius,
                endBlurRadius,
                revealBeforeAnimation,
                cancellationToken);
            return;
        }

        var previousOpacity = compositionVisual.Opacity;
        var previousTranslation = compositionVisual.Translation;
        var startTranslation = new Vector3(
            (float)(previousTranslation.X + startOffset),
            (float)previousTranslation.Y,
            (float)previousTranslation.Z);
        var endTranslation = new Vector3(
            (float)(previousTranslation.X + endOffset),
            (float)previousTranslation.Y,
            (float)previousTranslation.Z);

        compositionVisual.StopAnimation("Opacity");
        compositionVisual.StopAnimation("Translation");
        compositionVisual.Opacity = (float)startOpacity;
        compositionVisual.Translation = startTranslation;

        // The effect animation is the Skia-rendered path. Opacity and translation
        // remain compositor animations because they do not redraw or invalidate layout.
        target.Effect = new BlurEffect { Radius = endBlurRadius };
        var blurAnimation = CreateBlurAnimation(startBlurRadius, endBlurRadius, duration, easing)
            .RunAsync(target, cancellationToken);

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
            startTranslation,
            endTranslation);

        try
        {
            await Task.WhenAll(
                blurAnimation,
                Task.Delay(duration, cancellationToken));
            target.Effect = new BlurEffect { Radius = endBlurRadius };
        }
        finally
        {
            compositionVisual.StopAnimation("Opacity");
            compositionVisual.StopAnimation("Translation");
            compositionVisual.Opacity = previousOpacity;
            compositionVisual.Translation = previousTranslation;
        }
    }

    private static void StartCompositionAnimation(
        CompositionVisual visual,
        TimeSpan duration,
        Easing easing,
        float startOpacity,
        float endOpacity,
        Vector3 startTranslation,
        Vector3 endTranslation)
    {
        var opacity = visual.Compositor.CreateScalarKeyFrameAnimation();
        opacity.Target = "Opacity";
        opacity.Duration = duration;
        opacity.InsertKeyFrame(0f, startOpacity);
        opacity.InsertKeyFrame(1f, endOpacity, easing);
        visual.StartAnimation("Opacity", opacity);

        var translation = visual.Compositor.CreateVector3KeyFrameAnimation();
        translation.Target = "Translation";
        translation.Duration = duration;
        translation.InsertKeyFrame(0f, startTranslation);
        translation.InsertKeyFrame(1f, endTranslation, easing);
        visual.StartAnimation("Translation", translation);
    }

    private static async Task AnimateFallbackAsync(
        Visual target,
        TimeSpan duration,
        Easing easing,
        double startOpacity,
        double endOpacity,
        double startOffset,
        double endOffset,
        double startBlurRadius,
        double endBlurRadius,
        bool revealBeforeAnimation,
        CancellationToken cancellationToken)
    {
        var translation = new TranslateTransform(startOffset, 0d);
        target.Opacity = startOpacity;
        target.RenderTransform = translation;
        target.Effect = new BlurEffect { Radius = endBlurRadius };

        var blurAnimation = CreateBlurAnimation(startBlurRadius, endBlurRadius, duration, easing)
            .RunAsync(target, cancellationToken);

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
            translation.X = startOffset + ((endOffset - startOffset) * progress);
            await Task.Delay(stepDuration, cancellationToken);
        }

        await blurAnimation;
    }

    private static Animation CreateBlurAnimation(
        double startRadius,
        double endRadius,
        TimeSpan duration,
        Easing easing)
    {
        return new Animation
        {
            Duration = duration,
            Easing = easing,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(
                            Visual.EffectProperty,
                            new BlurEffect { Radius = startRadius }),
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(
                            Visual.EffectProperty,
                            new BlurEffect { Radius = endRadius }),
                    },
                },
            },
        };
    }
}
