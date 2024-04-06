using Material.Dialog;
using ReactiveUI;

namespace MapWizard.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";

    public void ShowDialog()
    {
        var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams()
        {
            ContentHeader = "Hello",
            SupportingText = "This is a dialog",
            Width = 400,
            WindowTitle = "Alert",
            DialogButtons = [new DialogButton()
            {
                Content = "OK",
                IsPositive = true
            }, new DialogButton()
            {
                Content = "Cancel",
                IsNegative = true
            }
            ]
        });
        dialog.Show();
    }
}