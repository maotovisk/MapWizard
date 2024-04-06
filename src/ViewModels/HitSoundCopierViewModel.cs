using Avalonia.Controls;
using Material.Dialog;

namespace MapWizard.ViewModels;

public class HitSoundCopierViewModel : ViewModelBase
{
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