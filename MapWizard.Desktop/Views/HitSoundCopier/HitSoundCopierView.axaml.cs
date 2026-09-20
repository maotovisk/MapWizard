using System.Diagnostics;
using Avalonia.Controls;

namespace MapWizard.Desktop.Views;

public partial class HitSoundCopierView : UserControl
{
    public HitSoundCopierView()
    {
        
        var sw = Stopwatch.StartNew();

        InitializeComponent();
        Debug.WriteLine(
            $"HsCopier InitializeComponent: {sw.Elapsed.TotalMilliseconds:F2}ms");
    }
}