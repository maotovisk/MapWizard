using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.Notifications;

namespace MapWizard.Desktop.Services;

public readonly record struct NotificationHandle(Guid Id);

public sealed record NotificationAction(
    string Label,
    Action Execute,
    bool IsPrimary = false,
    bool DismissAfterExecution = true);

public interface INotificationService
{
    ReadOnlyObservableCollection<AppNotification> Notifications { get; }

    NotificationHandle Show(
        NotificationType type,
        string title,
        string message,
        TimeSpan? dismissAfter = null,
        bool isBusy = false,
        double? progress = null,
        IReadOnlyList<NotificationAction>? actions = null);

    void Dismiss(NotificationHandle handle);

    void UpdateProgress(NotificationHandle handle, double progress);
}
