using Avalonia.Controls;
using Avalonia;

namespace MapWizard.Desktop.Controls.View;

public partial class BeatmapFileSelector : UserControl
{

    public static readonly StyledProperty<bool> AllowManyProperty =
        AvaloniaProperty.Register<BeatmapFileSelector, bool>(nameof(AllowMany), defaultValue: false);

    public bool AllowMany
    {
        get => GetValue(AllowManyProperty);
        set => SetValue(AllowManyProperty, value);
    }
    public BeatmapFileSelector()
    {
        InitializeComponent();
    }
}