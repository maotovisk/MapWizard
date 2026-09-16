using Avalonia;
using Avalonia.Controls;

namespace MapWizard.Theme.Controls;

public sealed class BusyArea : ContentControl
{
    public static readonly StyledProperty<bool> IsBusyProperty =
        AvaloniaProperty.Register<BusyArea, bool>(nameof(IsBusy));

    public static readonly StyledProperty<string?> BusyTextProperty =
        AvaloniaProperty.Register<BusyArea, string?>(nameof(BusyText));

    public bool IsBusy
    {
        get => GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public string? BusyText
    {
        get => GetValue(BusyTextProperty);
        set => SetValue(BusyTextProperty, value);
    }

    static BusyArea()
    {
        IsBusyProperty.Changed.AddClassHandler<BusyArea>(
            static (control, e) => control.PseudoClasses.Set(":busy", e.NewValue is true));
    }
}
