using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Theme.Palettes;

namespace MapWizard.Desktop.Services;

public sealed partial class AppNotification : ObservableObject
{
    public required NotificationHandle Handle { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required NotificationType Type { get; init; }
    public required IBrush AccentBrush { get; init; }
    public required string Symbol { get; init; }
    public required IReadOnlyList<AppNotificationAction> Actions { get; init; }
    public required IRelayCommand CloseCommand { get; init; }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasProgress;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isDismissing;
}

public sealed record AppNotificationAction(string Label, bool IsPrimary, IRelayCommand Command);

public sealed class NotificationService : INotificationService
{
    private static readonly TimeSpan DismissalAnimationTime = TimeSpan.FromMilliseconds(240);

    private readonly ObservableCollection<AppNotification> _notifications = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _dismissals = [];

    public NotificationService()
    {
        Notifications = new ReadOnlyObservableCollection<AppNotification>(_notifications);
    }

    public ReadOnlyObservableCollection<AppNotification> Notifications { get; }

    public NotificationHandle Show(
        NotificationType type,
        string title,
        string message,
        TimeSpan? dismissAfter = null,
        bool isBusy = false,
        double? progress = null,
        IReadOnlyList<NotificationAction>? actions = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.Invoke(() => Show(type, title, message, dismissAfter, isBusy, progress, actions));
        }

        var handle = new NotificationHandle(Guid.NewGuid());
        var actionModels = new List<AppNotificationAction>();
        if (actions is not null)
        {
            foreach (var action in actions)
            {
                actionModels.Add(new AppNotificationAction(
                    action.Label,
                    action.IsPrimary,
                    new RelayCommand(() =>
                    {
                        action.Execute();
                        if (action.DismissAfterExecution)
                        {
                            Dismiss(handle);
                        }
                    })));
            }
        }

        var notification = new AppNotification
        {
            Handle = handle,
            Type = type,
            Title = title,
            Message = message,
            IsBusy = isBusy,
            HasProgress = progress.HasValue,
            Progress = progress ?? 0,
            Actions = actionModels,
            AccentBrush = StatusTonePalette.GetBrush(ToStatusTone(type)),
            Symbol = GetSymbol(type),
            CloseCommand = new RelayCommand(() => Dismiss(handle))
        };

        _notifications.Add(notification);
        while (_notifications.Count(static n => !n.IsDismissing) > 3)
        {
            BeginDismiss(_notifications.First(static n => !n.IsDismissing));
        }

        if (dismissAfter is { } duration)
        {
            var cancellation = new CancellationTokenSource();
            _dismissals[handle.Id] = cancellation;
            _ = DismissAfterAsync(handle, duration, cancellation.Token);
        }

        return handle;
    }

    public void Dismiss(NotificationHandle handle)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Dismiss(handle));
            return;
        }

        var notification = _notifications.FirstOrDefault(
            n => n.Handle == handle && !n.IsDismissing);
        if (notification is null)
        {
            return;
        }

        BeginDismiss(notification);
    }

    private void BeginDismiss(AppNotification notification)
    {
        if (_dismissals.Remove(notification.Handle.Id, out var cancellation))
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }

        notification.IsDismissing = true;
        DispatcherTimer.RunOnce(
            () => _notifications.Remove(notification),
            DismissalAnimationTime);
    }

    public void UpdateProgress(NotificationHandle handle, double progress)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => UpdateProgress(handle, progress));
            return;
        }

        foreach (var notification in _notifications)
        {
            if (notification.Handle != handle)
            {
                continue;
            }

            notification.HasProgress = true;
            notification.Progress = Math.Clamp(progress, 0, 100);
            return;
        }
    }

    private async System.Threading.Tasks.Task DismissAfterAsync(
        NotificationHandle handle,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        try
        {
            await System.Threading.Tasks.Task.Delay(duration, cancellationToken);
            Dismiss(handle);
        }
        catch (OperationCanceledException)
        {
            // Explicit dismissal owns cleanup.
        }
    }

    private static StatusTone ToStatusTone(NotificationType type) => type switch
    {
        NotificationType.Success => StatusTone.Success,
        NotificationType.Warning => StatusTone.Warning,
        NotificationType.Error => StatusTone.Error,
        _ => StatusTone.Information
    };

    private static string GetSymbol(NotificationType type) => type switch
    {
        NotificationType.Success => "✓",
        NotificationType.Warning => "!",
        NotificationType.Error => "×",
        _ => "i"
    };
}
