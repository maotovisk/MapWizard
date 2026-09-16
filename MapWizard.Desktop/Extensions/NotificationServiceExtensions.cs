using System;
using Avalonia.Controls.Notifications;
using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.Extensions;

public static class NotificationServiceExtensions
{
    public static NotificationHandle ShowToast(
        this INotificationService notificationService,
        NotificationType type,
        string title,
        string message,
        TimeSpan? dismissAfter = null)
    {
        return notificationService.Show(
            type,
            title,
            message,
            dismissAfter ?? TimeSpan.FromSeconds(8));
    }
}
