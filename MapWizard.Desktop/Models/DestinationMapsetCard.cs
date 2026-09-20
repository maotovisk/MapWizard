using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public bool HasUnselectedDifficulties => Difficulties.Any(difficulty => !difficulty.IsSelected);

    /// <summary>
    /// Re-raises derived state after <see cref="Difficulties"/> entries were
    /// updated in place, so bindings like the select-all button stay current.
    /// </summary>
    public void RefreshDifficultySelectionState()
    {
        OnPropertyChanged(nameof(HasUnselectedDifficulties));
    }
}
