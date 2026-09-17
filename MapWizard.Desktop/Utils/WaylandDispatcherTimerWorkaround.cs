using System;
using System.Diagnostics;
using System.Reflection;
using Avalonia.Threading;

namespace MapWizard.Desktop.Utils;

/// <summary>
/// Corrects the clock mismatch in Avalonia 12.1's native Wayland dispatcher.
/// See https://github.com/AvaloniaUI/Avalonia/issues/22064.
/// </summary>
internal static class WaylandDispatcherTimerWorkaround
{
    private const string TimeProviderFieldName = "_timeProvider";
    private const string PlatformImplementationFieldName = "_impl";
    private const long MinimumClockSkewMilliseconds = 5;

    public static void ApplyIfNeeded()
    {
        if (!OperatingSystem.IsLinux() ||
            string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
        {
            return;
        }

        try
        {
            var dispatcher = Dispatcher.UIThread;
            var timeProviderField = typeof(Dispatcher).GetField(
                TimeProviderFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            var platformImplementationField = typeof(Dispatcher).GetField(
                PlatformImplementationFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (timeProviderField?.FieldType != typeof(Func<long>) ||
                timeProviderField.GetValue(dispatcher) is not Func<long> dispatcherClock ||
                platformImplementationField?.GetValue(dispatcher) is not { } platformImplementation)
            {
                Trace.TraceWarning(
                    "The Avalonia Wayland timer workaround was skipped because the dispatcher internals changed.");
                return;
            }

            var platformNowGetter = platformImplementation.GetType()
                .GetProperty("Now", BindingFlags.Instance | BindingFlags.Public)?
                .GetMethod;
            if (platformNowGetter?.ReturnType != typeof(long))
            {
                Trace.TraceWarning(
                    "The Avalonia Wayland timer workaround was skipped because the platform clock was unavailable.");
                return;
            }

            var platformClock = (Func<long>)platformNowGetter.CreateDelegate(
                typeof(Func<long>),
                platformImplementation);
            var clockSkew = Math.Abs(dispatcherClock() - platformClock());
            if (clockSkew < MinimumClockSkewMilliseconds)
            {
                return;
            }

            timeProviderField.SetValue(dispatcher, platformClock);
            Trace.WriteLine(
                $"Applied Avalonia Wayland timer workaround (dispatcher clock skew: {clockSkew} ms).");
        }
        catch (Exception exception) when (exception is MemberAccessException or TargetException)
        {
            // This relies on an Avalonia private field and must not prevent startup if
            // a future framework release changes or removes that implementation detail.
            Trace.TraceWarning($"Could not apply the Avalonia Wayland timer workaround: {exception.Message}");
        }
    }
}
