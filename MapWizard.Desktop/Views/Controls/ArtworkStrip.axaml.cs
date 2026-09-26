using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.Views.Controls;

/// <summary>
/// Two-piece banner: a slanted neutral strap on the left (icon or label) and a body drawn over
/// beatmap artwork. On hover the strap slides away and the artwork grows into its space, leaving
/// more room for the body.
/// </summary>
public partial class ArtworkStrip : UserControl
{
    private static readonly TimeSpan RevealDuration = TimeSpan.FromMilliseconds(260);

    /// <summary>
    /// Extra strap width hidden past the left edge, so the slant never exposes the artwork there.
    /// </summary>
    private const double StrapOverhang = 20;

    private const double StrapSkewDegrees = -20;

    /// <summary>
    /// Width of the highlight drawn on the strap's slanted edge.
    /// </summary>
    private const double StrapEdgeWidth = 2;

    /// <summary>
    /// How far the body layers start under the strap, enough to cover its slanted edge.
    /// </summary>
    private const double BodyUnderlap = 10;

    /// <summary>
    /// Zoom applied to the artwork when fully collapsed, so it visibly comes forward on hover.
    /// </summary>
    private const double HoverArtworkZoom = 0.08;

    public static readonly StyledProperty<IImage?> ArtworkProperty =
        AvaloniaProperty.Register<ArtworkStrip, IImage?>(nameof(Artwork));

    public static readonly StyledProperty<object?> StrapContentProperty =
        AvaloniaProperty.Register<ArtworkStrip, object?>(nameof(StrapContent));

    public static readonly StyledProperty<object?> BodyContentProperty =
        AvaloniaProperty.Register<ArtworkStrip, object?>(nameof(BodyContent));

    public static readonly StyledProperty<double> StrapWidthProperty =
        AvaloniaProperty.Register<ArtworkStrip, double>(nameof(StrapWidth), 36);

    public static readonly StyledProperty<bool> CollapseStrapOnHoverProperty =
        AvaloniaProperty.Register<ArtworkStrip, bool>(nameof(CollapseStrapOnHover), true);

    // 1 = strap fully shown, 0 = collapsed. Animated per frame rather than through Avalonia
    // transitions, which skipped the first change on each control.
    private double _reveal = 1;
    private double _animationFrom = 1;
    private double _animationTarget = 1;
    private TimeSpan? _animationStart;
    private bool _isAnimating;

    public ArtworkStrip()
    {
        InitializeComponent();
        ApplyReveal();
    }

    public IImage? Artwork
    {
        get => GetValue(ArtworkProperty);
        set => SetValue(ArtworkProperty, value);
    }

    public object? StrapContent
    {
        get => GetValue(StrapContentProperty);
        set => SetValue(StrapContentProperty, value);
    }

    public object? BodyContent
    {
        get => GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }

    public double StrapWidth
    {
        get => GetValue(StrapWidthProperty);
        set => SetValue(StrapWidthProperty, value);
    }

    public bool CollapseStrapOnHover
    {
        get => GetValue(CollapseStrapOnHoverProperty);
        set => SetValue(CollapseStrapOnHoverProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPointerOverProperty || change.Property == CollapseStrapOnHoverProperty)
        {
            AnimateRevealTo(CollapseStrapOnHover && IsPointerOver ? 0 : 1);
        }
        else if (change.Property == StrapWidthProperty || change.Property == BoundsProperty)
        {
            ApplyReveal();
        }
    }

    private void AnimateRevealTo(double target)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (AppearanceSettings.ReducedMotion || topLevel is null)
        {
            _isAnimating = false;
            _reveal = target;
            ApplyReveal();
            return;
        }

        // Restart from wherever the strap currently is, so reversing mid-hover stays smooth.
        _animationFrom = _reveal;
        _animationTarget = target;
        _animationStart = null;
        if (!_isAnimating)
        {
            _isAnimating = true;
            topLevel.RequestAnimationFrame(OnAnimationFrame);
        }
    }

    private void OnAnimationFrame(TimeSpan timestamp)
    {
        if (!_isAnimating)
        {
            return;
        }

        _animationStart ??= timestamp;
        var progress = Math.Clamp((timestamp - _animationStart.Value) / RevealDuration, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 3);
        _reveal = _animationFrom + (_animationTarget - _animationFrom) * eased;
        ApplyReveal();

        if (progress < 1 && TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.RequestAnimationFrame(OnAnimationFrame);
            return;
        }

        _isAnimating = false;
    }

    private void ApplyReveal()
    {
        StrapHost.Width = StrapWidth * _reveal;
        Strap.Width = StrapWidth + StrapOverhang;

        // The slant leans the strap's top edge past its host; slide it that much further out as it
        // collapses so no sliver of strap or highlight is left in the corner.
        var height = Bounds.Height > 0 ? Bounds.Height : 42;
        var slantReach = height / 2 * Math.Tan(Math.Abs(StrapSkewDegrees) * Math.PI / 180) + StrapEdgeWidth;
        Strap.RenderTransform = new TransformGroup
        {
            Children =
            {
                new SkewTransform(StrapSkewDegrees, 0),
                new TranslateTransform(-slantReach * (1 - _reveal), 0)
            }
        };
        StrapPresenter.Width = StrapWidth;
        StrapPresenter.Opacity = _reveal;

        // Hand the strap's width to the body so the control keeps its size: the text slides
        // left and the artwork fills the freed space instead of the whole control shrinking.
        BodyPresenter.Margin = new Thickness(12, 0, 14 + StrapWidth * (1 - _reveal), 0);
        Shade.Opacity = 0.6 + 0.4 * _reveal;

        // Keep the artwork out from under the strap's rounded corner; stacked layers there leave
        // an anti-aliasing fringe along the clip.
        var bodyInset = new Thickness(Math.Max(0, StrapWidth * _reveal - BodyUnderlap), 0, 0, 0);
        BodyBase.Margin = bodyInset;
        BodyArtwork.Margin = bodyInset;
        var zoom = 1 + HoverArtworkZoom * (1 - _reveal);
        BodyArtwork.RenderTransform = new ScaleTransform(zoom, zoom);
        Shade.Margin = bodyInset;
    }
}
