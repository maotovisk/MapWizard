using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using MapWizard.Desktop.ViewModels;

namespace MapWizard.Desktop.Views.Dialogs;

public partial class SongMapsetCard : UserControl
{
    private SongMapsetCardViewModel? _mapset;

    public SongMapsetCard()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SizeChanged += (_, _) => UpdateExpansionHeight();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_mapset is not null)
        {
            _mapset.PropertyChanged -= OnMapsetPropertyChanged;
        }

        _mapset = DataContext as SongMapsetCardViewModel;
        if (_mapset is not null)
        {
            _mapset.PropertyChanged += OnMapsetPropertyChanged;
        }

        UpdateExpansionHeight();
    }

    private void OnMapsetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SongMapsetCardViewModel.IsExpanded) ||
            e.PropertyName == nameof(SongMapsetCardViewModel.Difficulties))
        {
            UpdateExpansionHeight();
        }
    }

    private void UpdateExpansionHeight()
    {
        if (_mapset?.IsExpanded != true || DetailsPanel.Child is null)
        {
            DetailsPanel.MaxHeight = 0;
            return;
        }

        // Animate the measured height. A large fixed MaxHeight reaches the real
        // height early, making short lists appear to snap open.
        var contentWidth = Math.Max(0d, Bounds.Width - DetailsPanel.Padding.Left - DetailsPanel.Padding.Right);
        if (contentWidth <= 0d)
        {
            return;
        }

        DetailsPanel.Child.Measure(new Size(contentWidth, double.PositiveInfinity));
        DetailsPanel.MaxHeight = DetailsPanel.Child.DesiredSize.Height +
                                 DetailsPanel.Padding.Top + DetailsPanel.Padding.Bottom;
    }
}
