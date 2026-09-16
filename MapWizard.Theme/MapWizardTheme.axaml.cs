using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace MapWizard.Theme;

public sealed class MapWizardTheme : Styles
{
    public MapWizardTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
