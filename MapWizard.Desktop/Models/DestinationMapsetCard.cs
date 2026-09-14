using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MapWizard.Desktop.Models;

public partial class DestinationMapsetCard(
    string mapsetDirectoryPath,
    SelectedMap referenceBeatmap,
    IEnumerable<MapsetDifficultyCard> difficulties,
    bool isSuggested = false) : ObservableObject
{
    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private bool _isSuggested = isSuggested;

    public string MapsetDirectoryPath { get; } = mapsetDirectoryPath;
    public SelectedMap ReferenceBeatmap { get; } = referenceBeatmap;
    public ObservableCollection<MapsetDifficultyCard> Difficulties { get; } = new(difficulties);
}
