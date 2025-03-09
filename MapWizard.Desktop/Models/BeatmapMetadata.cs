using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using MapWizard.BeatmapParser;

namespace MapWizard.Desktop.Models;

public class BeatmapMetadata : INotifyPropertyChanged
{
    public string Title { get; set; } = string.Empty;
    public string RomanizedTitle { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string RomanizedArtist { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public int BeatmapId { get; set; } = 0;
    public int BeatmapSetId { get; set; } = -1;
    public string AudioFilename { get; set; }  = string.Empty;
    public string BackgroundFilename { get; set; } = string.Empty;
    public string VideoFilename { get; set; } = string.Empty;
    public int VideoOffset { get; set; } = 0;
    public int PreviewTime { get; set; } = -1;

    public AvaloniaList<AvaloniaComboColour> Colours { get; set; } = [];
    
    private Color? _sliderTrackColour;
    public Color? SliderTrackColour
    {
        get => _sliderTrackColour;
        set
        {
            if (_sliderTrackColour != value)
            {
                _sliderTrackColour = value;
                OnPropertyChanged(nameof(SliderTrackColour));
            }
        }
    }

    private Color? _sliderBorderColour;
    public Color? SliderBorderColour
    {
        get => _sliderBorderColour;
        set
        {
            if (_sliderBorderColour != value)
            {
                _sliderBorderColour = value;
                OnPropertyChanged(nameof(SliderBorderColour));
            }
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    
    public bool WidescreenStoryboard { get; set; } = false;
    
    public bool LetterboxInBreaks { get; set; } = false;
    
    public bool EpilepsyWarning { get; set; } = false;
    public bool SamplesMatch { get; set; } = false;
}

public class AvaloniaComboColour : INotifyPropertyChanged
{
    private Color? _colour;

    public Color? Colour
    {
        get => _colour;
        set
        {
            if (_colour != value)
            {
                _colour = value;
                OnPropertyChanged(nameof(Colour));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private int _number;
    
    public int Number
    {
        get => _number;
        set
        {
            if (_number != value)
            {
                _number = value;
                OnPropertyChanged(nameof(Number));
            }
        }
    }
    
    public AvaloniaComboColour(int number, Color? colour)
    {
        Number = number;
        Colour = colour;
    }
}