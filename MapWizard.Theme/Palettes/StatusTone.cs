using Avalonia.Media;

namespace MapWizard.Theme.Palettes;

public enum StatusTone
{
    Information,
    Success,
    Warning,
    Error
}

public static class StatusTonePalette
{
    public static IBrush GetBrush(StatusTone tone) => new SolidColorBrush(tone switch
    {
        StatusTone.Success => Color.Parse("#7FD88F"),
        StatusTone.Warning => Color.Parse("#F5A742"),
        StatusTone.Error => Color.Parse("#E06C75"),
        _ => Color.Parse("#56B6C2")
    });
}
