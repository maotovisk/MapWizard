using System;
using Avalonia.Controls.Notifications;
using SukiUI.Toasts;

namespace MapWizard.Desktop.Extensions;

public static class SukiToastManagerExtensions
{
    public static void ShowToast(
        this ISukiToastManager toastManager,
        NotificationType type,
        string title,
        string message,
        TimeSpan? dismissAfter = null)
    {
        toastManager.CreateToast()
            .OfType(type)
            .WithTitle(title)
            .WithContent(message)
            .Dismiss().ByClicking()
            .Dismiss().After(dismissAfter ?? TimeSpan.FromSeconds(8))
            .Queue();
    }
}
