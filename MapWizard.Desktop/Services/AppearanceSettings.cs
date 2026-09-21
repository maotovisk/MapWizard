using System;
using MapWizard.Desktop.Models.Settings;

namespace MapWizard.Desktop.Services;

/// <summary>
/// Process-wide appearance switches (modal blur, reduced motion), persisted in
/// <see cref="MainSettings"/> and applied live. Mirrors the SmoothScrollViewer
/// global-toggle pattern: loaded from settings at startup and updated by the
/// settings page, consumed by ModalHost and MainWindow.
/// </summary>
public static class AppearanceSettings
{
    private static bool _blurModals = true;
    private static bool _reducedMotion;

    public static event EventHandler? Changed;

    public static bool BlurModals
    {
        get => _blurModals;
        set
        {
            if (_blurModals == value)
            {
                return;
            }

            _blurModals = value;
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }

    public static bool ReducedMotion
    {
        get => _reducedMotion;
        set
        {
            if (_reducedMotion == value)
            {
                return;
            }

            _reducedMotion = value;
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }

    public static void LoadFrom(MainSettings settings)
    {
        BlurModals = settings.BlurModals;
        ReducedMotion = settings.ReducedMotion;
    }
}
