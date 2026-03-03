using CommunityToolkit.Mvvm.ComponentModel;

namespace MapWizard.Desktop.Models;

public partial class MapsetDifficultyCard(string path) : ObservableObject
{
    [ObservableProperty] private bool _isSelected;

    public SelectedMap Beatmap { get; } = new()
    {
        Path = path
    };

    public string Path => Beatmap.Path;
}
