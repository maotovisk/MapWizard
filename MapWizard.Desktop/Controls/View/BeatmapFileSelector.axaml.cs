using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using MapWizard.Desktop.Controls.ViewModel;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Controls.View;

public partial class BeatmapFileSelector : UserControl
{
    private BeatmapFileSelectorData Vm => (BeatmapFileSelectorData)DataContext ?? new BeatmapFileSelectorData();

    // Add properties here
    public bool AllowMany
    {
        get => Vm.AllowMany;
        set => Vm.AllowMany = value;
    }

    public string Title
    {
        get => Vm.Title;
        set => Vm.Title = value;
    }
    
    public string PreferredDirectory {
        get => Vm.PreferredDirectory;
        set => Vm.PreferredDirectory = value;
    }
    
    public AvaloniaList<SelectedMap> BeatmapPaths  {
        get => Vm.BeatmapPaths;
        set
        {
            if (Vm.BeatmapPaths != value)
            {
                Vm.BeatmapPaths = value;
                // Notify that the OriginBeatmapPath should be updated if necessary
                // Optionally notify ViewModel here if needed
            }
        }
        
    }
    
    // Add StyledProperties from the above properties
    public static readonly StyledProperty<bool> AllowManyProperty =
        AvaloniaProperty.Register<BeatmapFileSelector, bool>(nameof(AllowMany));
    
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<BeatmapFileSelector, string>(nameof(Title));
    
    public static readonly StyledProperty<AvaloniaList<SelectedMap>> BeatmapPathsProperty =
        AvaloniaProperty.Register<BeatmapFileSelector, AvaloniaList<SelectedMap>>(nameof(BeatmapPaths), defaultBindingMode: BindingMode.TwoWay);
    
    public static readonly StyledProperty<string> PreferredDirectoryProperty =
        AvaloniaProperty.Register<BeatmapFileSelector, string>(nameof(PreferredDirectory), defaultBindingMode: BindingMode.TwoWay);
    
    public BeatmapFileSelector()
    {
        InitializeComponent();
    }
}