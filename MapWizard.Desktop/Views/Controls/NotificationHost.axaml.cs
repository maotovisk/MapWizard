using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.Views.Controls;

public partial class NotificationHost : UserControl
{
    public static readonly StyledProperty<INotificationService?> NotificationServiceProperty =
        AvaloniaProperty.Register<NotificationHost, INotificationService?>(nameof(NotificationService));

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

        if (notification.IsDismissing)
        {
            border.Opacity = 0;
            return;
        }

        border.Opacity = 0;
        border.RenderTransform = TransformOperations.Parse("translate(14px, 0px)");
        DispatcherTimer.RunOnce(
            () =>
            {
                border.Opacity = 1;
                border.RenderTransform = TransformOperations.Parse("translate(0px, 0px)");
            },
            TimeSpan.FromMilliseconds(20));

        PropertyChangedEventHandler? dismissHandler = null;
        dismissHandler = (_, args) =>
        {
            if (args.PropertyName != nameof(AppNotification.IsDismissing) || !notification.IsDismissing)
            {
                return;
            }

            notification.PropertyChanged -= dismissHandler;
            border.Opacity = 0;
            border.RenderTransform = TransformOperations.Parse("translate(14px, 0px)");
        };
        notification.PropertyChanged += dismissHandler;
        border.DetachedFromVisualTree += (_, _) => notification.PropertyChanged -= dismissHandler;
    }
}
