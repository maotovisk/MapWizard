using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        string name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        Type? type = Type.GetType(name);

        if (type != null && data is ViewModelBase)
        {
            // pass the data context to the view directly at construction
            
            Control? control = (Control?)Activator.CreateInstance(type, data);
            
            return control;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }

    /// <summary>
    /// Finds a view from a given ViewModel
    /// </summary>
    /// <param name="vm">The ViewModel representing a View</param>
    /// <returns>The View that matches the ViewModel. Null is no match found</returns>
    public static Window? ResolveViewFromViewModel<T>(T vm) where T : ViewModelBase
    {
        string name = vm.GetType().AssemblyQualifiedName!.Replace("ViewModel", "View");
        Type? type = Type.GetType(name);
        return type != null ? (Window?)Activator.CreateInstance(type) : null;
    }
    
    public static UserControl? ResolveControlViewFromViewModel<T>(T vm) where T : ViewModelBase
    {
        string name = vm.GetType().AssemblyQualifiedName!.Replace("ViewModel", "View");
        Type? type = Type.GetType(name);
        return type != null ? (UserControl?)Activator.CreateInstance(type) : null;
    }

    private static IEnumerable<Window> Windows =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows ?? Array.Empty<Window>();

    public static Window? FindWindowByViewModel(INotifyPropertyChanged viewModel) =>
        Windows.FirstOrDefault(x => ReferenceEquals(viewModel, x.DataContext));
}
