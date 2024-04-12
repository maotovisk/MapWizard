using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
        var type = Type.GetType(name);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = data;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
