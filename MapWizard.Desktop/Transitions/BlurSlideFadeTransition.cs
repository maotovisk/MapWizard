using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;

namespace MapWizard.Desktop.Transitions;

/// <summary>
/// Page transition that blurs and slides the old page out while fading,
/// then unblurs and settles the new page in. Driven by stepped interpolation
/// so it cannot stall.
/// </summary>
public sealed class BlurSlideFadeTransition : IPageTransition
{
    public TimeSpan OutDuration { get; set; } = TimeSpan.FromMilliseconds(160);

    public TimeSpan InDuration { get; set; } = TimeSpan.FromMilliseconds(300);

    public double SlideDistance { get; set; } = 32d;

    public double MaxBlurRadius { get; set; } = 9d;

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        var direction = forward ? 1d : -1d;

        // TransitioningContentControl adds the incoming presenter before calling Start.
        // Hide it synchronously so it cannot paint a frame over the outgoing page.
        if (to is not null)
        {
            to.IsVisible = false;
            to.Opacity = 0d;
            to.RenderTransform = new TranslateTransform(direction * SlideDistance, 0d);
        }

        try
        {
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
                from.Effect = null;
                from.Opacity = 1d;
                from.RenderTransform = new TranslateTransform(0d, 0d);
            }

            if (to is not null)
            {
                to.Effect = null;
                to.Opacity = 1d;
                to.IsVisible = true;
                to.RenderTransform = new TranslateTransform(0d, 0d);
            }
        }
    }

    private async Task AnimateOutAsync(Visual from, double direction, CancellationToken cancellationToken)
    {
        var blur = new BlurEffect { Radius = 0d };
        from.Effect = blur;

        const int steps = 10;
        for (var i = 1; i <= steps; i++)
        {
            var t = SmoothStep((double)i / steps);
            from.Opacity = 1d - t;
            from.RenderTransform = new TranslateTransform(-direction * SlideDistance * t, 0d);
            blur.Radius = MaxBlurRadius * t;
            await Task.Delay(OutDuration / steps, cancellationToken);
        }
    }

    private async Task AnimateInAsync(Visual to, double direction, CancellationToken cancellationToken)
    {
        var blur = new BlurEffect { Radius = MaxBlurRadius };
        to.Effect = blur;
        to.Opacity = 0d;
        to.RenderTransform = new TranslateTransform(direction * SlideDistance, 0d);
        to.IsVisible = true;

        const int steps = 14;
        for (var i = 1; i <= steps; i++)
        {
            var t = SmoothStep((double)i / steps);
            to.Opacity = t;
            to.RenderTransform = new TranslateTransform(direction * SlideDistance * (1d - t), 0d);
            blur.Radius = MaxBlurRadius * (1d - t);
            await Task.Delay(InDuration / steps, cancellationToken);
        }
    }

    private static double SmoothStep(double t)
    {
        t = Math.Clamp(t, 0d, 1d);
        return t * t * (3d - 2d * t);
    }
}
