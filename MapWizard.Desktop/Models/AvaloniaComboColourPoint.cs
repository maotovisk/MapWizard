using CommunityToolkit.Mvvm.ComponentModel;
using MapWizard.Tools.ComboColourStudio;
using System.Collections.ObjectModel;

namespace MapWizard.Desktop.Models;

public partial class AvaloniaComboColourPoint : ObservableObject
{
    [ObservableProperty] private double _time;
    [ObservableProperty] private ColourPointMode _mode;
    [ObservableProperty] private ObservableCollection<AvaloniaComboColourToken> _colourSequence = [];
}

public partial class AvaloniaComboColourToken : ObservableObject
{
    [ObservableProperty] private int _comboNumber = 1;
}
