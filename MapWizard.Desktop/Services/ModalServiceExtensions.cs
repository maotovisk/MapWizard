using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;

namespace MapWizard.Desktop.Services;

public static class ModalServiceExtensions
{
    public static async Task ShowMessageAsync(
        this IModalService modalService,
        string title,
        object content,
        string buttonText = "OK",
        CancellationToken cancellationToken = default)
    {
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var button = CreateButton(buttonText, primary: true);
        button.Click += async (_, _) => await modalService.CloseAsync(true);
        footer.Children.Add(button);

        await modalService.ShowAsync(new ModalRequest(content, title, footer), cancellationToken);
    }

    public static async Task<bool> ShowConfirmationAsync(
        this IModalService modalService,
        string title,
        object content,
        string confirmText,
        string cancelText,
        CancellationToken cancellationToken = default)
    {
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var cancel = CreateButton(cancelText, primary: false);
        cancel.Click += async (_, _) => await modalService.CloseAsync(false);
        footer.Children.Add(cancel);

        var confirm = CreateButton(confirmText, primary: true);
        confirm.Click += async (_, _) => await modalService.CloseAsync(true);
        footer.Children.Add(confirm);

        var result = await modalService.ShowAsync(
            new ModalRequest(content, title, footer),
            cancellationToken);
        return result is true;
    }

    private static Button CreateButton(string text, bool primary)
    {
        var button = new Button { Content = text };
        button.Classes.Add(primary ? "Flat" : "Basic");
        button.Classes.Add("Compact");
        return button;
    }
}
