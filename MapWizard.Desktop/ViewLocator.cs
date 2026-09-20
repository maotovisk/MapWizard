using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using MapWizard.Desktop.ViewModels;
using MapWizard.Desktop.Views;
using MapWizard.Desktop.Views.ComboColourStudio;
using MapWizard.Desktop.Views.HitSoundVisualizer;
using MapWizard.Desktop.Views.MapCleaner;
using MapWizard.Desktop.Views.MetadataManager;
using MapWizard.Desktop.Views.Settings;
using MapWizard.Desktop.Views.WelcomePage;

namespace MapWizard.Desktop;

public class ViewLocator : IDataTemplate
{
    // Explicit mapping keeps view resolution free of reflection so NativeAOT can
    // trim and compile the whole application ahead of time.
    private static readonly Dictionary<Type, Func<Control>> ViewFactories = new()
    {
        [typeof(WelcomePageViewModel)] = static () => new WelcomePageView(),
        [typeof(HitSoundCopierViewModel)] = static () => new HitSoundCopierView(),
        [typeof(HitSoundVisualizerViewModel)] = static () => new HitSoundVisualizerView(),
        [typeof(MetadataManagerViewModel)] = static () => new MetadataManagerView(),
        [typeof(ComboColourStudioViewModel)] = static () => new ComboColourStudioView(),
        [typeof(MapCleanerViewModel)] = static () => new MapCleanerView(),
        [typeof(SettingsViewModel)] = static () => new SettingsView(),
    };

    // Page views have large XAML object graphs, so each one is built once and then
    // reused for the lifetime of the app. A cached entry is replaced only when it
    // is still attached to the visual tree (possible during a page transition).
    private static readonly Dictionary<Type, Control> ViewCache = new();

    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        if (data is ViewModelBase)
        {
            return GetOrCreate(data);
        }

        return new TextBlock { Text = "Not Found: " + data.GetType().FullName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }

    /// <summary>
    /// Builds (or returns the cached) view for a page view model without attaching
    /// it to the visual tree, so the first navigation to that page is instant.
    /// </summary>
    public static void Preload(ViewModelBase viewModel)
    {
        if (ViewCache.ContainsKey(viewModel.GetType()))
        {
            return;
        }

        GetOrCreate(viewModel);
    }

    public static Window? FindWindowByViewModel(INotifyPropertyChanged viewModel) =>
        Windows.FirstOrDefault(x => ReferenceEquals(viewModel, x.DataContext));

    private static Control GetOrCreate(object dataContext)
    {
        var viewModelType = dataContext.GetType();

        if (ViewCache.TryGetValue(viewModelType, out var cached) &&
            cached.Parent is null &&
            cached.GetVisualParent() is null)
        {
            cached.DataContext = dataContext;
            return cached;
        }

        if (!ViewFactories.TryGetValue(viewModelType, out var factory))
        {
            return new TextBlock { Text = "Not Found: " + viewModelType.FullName };
        }

        var stopwatch = Stopwatch.StartNew();
        var control = factory();
        control.DataContext = dataContext;
        ViewCache[viewModelType] = control;
        Debug.WriteLine($"[ViewLocator] Built {viewModelType.Name} in {stopwatch.Elapsed.TotalMilliseconds:F1}ms");

        return control;
    }

    private static IEnumerable<Window> Windows =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows ?? Array.Empty<Window>();
}
