using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.VisualTree;

namespace MapWizard.Theme.Controls;

/// <summary>
/// A compositor-driven indeterminate progress indicator. The moving segment is
/// animated on the render thread so it follows the platform presentation rate
/// instead of requiring a UI-thread property update for every frame.
/// </summary>
public sealed class BusyIndicator : TemplatedControl
{
    private const float DefaultTrackWidth = 160f;
    private const float DefaultIndicatorWidth = 48f;

    private Control? _track;
    private Control? _indicator;
    private CompositionVisual? _indicatorVisual;

    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<BusyIndicator, bool>(nameof(IsRunning));

    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public BusyIndicator()
    {
        this.GetPropertyChangedObservable(Visual.IsVisibleProperty)
            .Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(_ => UpdateAnimationState()));
        this.GetPropertyChangedObservable(IsRunningProperty)
            .Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(_ => UpdateAnimationState()));
    }

    private sealed class AnonymousObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        StopAnimation();
        base.OnApplyTemplate(e);

        _track = e.NameScope.Find<Control>("PART_Track");
        _indicator = e.NameScope.Find<Control>("PART_Indicator");
        UpdateAnimationState();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TrackAncestors(this);
        UpdateAnimationState();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        StopAnimation();
        _track = null;
        _indicator = null;
        TrackAncestors(null);
        base.OnDetachedFromVisualTree(e);
    }

    private List<IDisposable>? _ancestorSubscriptions;

    private void TrackAncestors(Visual? root)
    {
        _ancestorSubscriptions?.ForEach(d => d.Dispose());
        _ancestorSubscriptions = [];

        var ancestor = root;
        while (ancestor is not null)
        {
            _ancestorSubscriptions.Add(ancestor
                .GetPropertyChangedObservable(Visual.IsVisibleProperty)
                .Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(_ => UpdateAnimationState())));
            ancestor = ancestor.GetValue(Visual.VisualParentProperty) as Visual;
        }
    }

    private void UpdateAnimationState()
    {
        if (!IsEffectivelyVisible || !IsRunning || _track is null || _indicator is null)
        {
            StopAnimation();
            return;
        }

        StopAnimation();
        var visual = ElementComposition.GetElementVisual(_indicator);
        if (visual is null)
        {
            return;
        }

        _indicatorVisual = visual;

        var trackWidth = (float)(_track.Bounds.Width > 0d
            ? _track.Bounds.Width
            : DefaultTrackWidth);
        var indicatorWidth = (float)(_indicator.Bounds.Width > 0d
            ? _indicator.Bounds.Width
            : DefaultIndicatorWidth);
        var animation = _indicatorVisual.Compositor.CreateVector3KeyFrameAnimation();
        var linear = new LinearEasing();

        animation.Target = "Translation";
        animation.Duration = TimeSpan.FromMilliseconds(900);
        animation.IterationBehavior = AnimationIterationBehavior.Forever;
        animation.InsertKeyFrame(0f, new Vector3(-indicatorWidth, 0f, 0f), linear);
        animation.InsertKeyFrame(1f, new Vector3(trackWidth, 0f, 0f), linear);

        _indicatorVisual.StartAnimation("Translation", animation);
    }

    private void StopAnimation()
    {
        if (_indicatorVisual is null)
        {
            return;
        }

        _indicatorVisual.StopAnimation("Translation");
        _indicatorVisual.Translation = default;
        _indicatorVisual = null;
    }
}
