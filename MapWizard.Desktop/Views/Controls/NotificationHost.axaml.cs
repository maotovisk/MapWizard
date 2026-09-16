using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.Views.Controls;

public partial class NotificationHost : UserControl
{
    public static readonly StyledProperty<INotificationService?> NotificationServiceProperty =
        AvaloniaProperty.Register<NotificationHost, INotificationService?>(nameof(NotificationService));

    private const float SlideDistance = 56f;
    private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(240);

    public NotificationHost()
    {
        InitializeComponent();
    }

    public INotificationService? NotificationService
    {
        get => GetValue(NotificationServiceProperty);
        set => SetValue(NotificationServiceProperty, value);
    }

    private void OnToastLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not AppNotification notification)
        {
            return;
        }

        var visual = ElementComposition.GetElementVisual(border);
        if (visual is null)
        {
            return;
        }

        visual.StopAnimation("Opacity");
        visual.StopAnimation("Translation");
        visual.Opacity = 0f;
        visual.Translation = new System.Numerics.Vector3(SlideDistance, 0f, 0f);

        StartSlideAnimation(visual,
            fromOpacity: 0f, toOpacity: 1f,
            fromX: SlideDistance, toX: 0f);

        PropertyChangedEventHandler? dismissHandler = null;
        dismissHandler = (_, args) =>
        {
            if (args.PropertyName != nameof(AppNotification.IsDismissing) || !notification.IsDismissing)
            {
                return;
            }

            notification.PropertyChanged -= dismissHandler;
            StartSlideAnimation(visual,
                fromOpacity: 1f, toOpacity: 0f,
                fromX: 0f, toX: SlideDistance);
        };
        notification.PropertyChanged += dismissHandler;
        border.DetachedFromVisualTree += (_, _) => notification.PropertyChanged -= dismissHandler;
    }

    private static void StartSlideAnimation(
        CompositionVisual visual,
        float fromOpacity, float toOpacity,
        float fromX, float toX)
    {
        var opacity = visual.Compositor.CreateScalarKeyFrameAnimation();
        opacity.Target = "Opacity";
        opacity.Duration = AnimationDuration;
        opacity.InsertKeyFrame(0f, fromOpacity);
        opacity.InsertKeyFrame(1f, toOpacity);
        visual.StartAnimation("Opacity", opacity);

        var translation = visual.Compositor.CreateVector3KeyFrameAnimation();
        translation.Target = "Translation";
        translation.Duration = AnimationDuration;
        translation.InsertKeyFrame(0f, new System.Numerics.Vector3(fromX, 0f, 0f));
        translation.InsertKeyFrame(1f, new System.Numerics.Vector3(toX, 0f, 0f));
        visual.StartAnimation("Translation", translation);
    }

    private void OnToastTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Button)
        {
            return;
        }

        if (sender is Border border
            && border.DataContext is AppNotification notification
            && notification.CloseCommand.CanExecute(null))
        {
            notification.CloseCommand.Execute(null);
        }
    }
}
