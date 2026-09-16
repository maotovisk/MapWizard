using Avalonia;
using Avalonia.Controls;

namespace MapWizard.Desktop.Views.Controls;

public partial class ImportantNotice : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ImportantNotice, string>(nameof(Title), "Important");

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ImportantNotice, string>(nameof(Message), string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ImportantNotice()
    {
        InitializeComponent();
    }
}
