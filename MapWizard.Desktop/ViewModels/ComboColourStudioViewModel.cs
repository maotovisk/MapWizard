using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BeatmapParser;
using BeatmapParser.Colours;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Services;
using MapWizard.Tools.ComboColourStudio;
using SukiUI.Toasts;
using ToolComboColourPoint = MapWizard.Tools.ComboColourStudio.ComboColourPoint;

namespace MapWizard.Desktop.ViewModels;

public partial class ComboColourStudioViewModel(
    IFilesService filesService,
    IComboColourStudioService comboColourStudioService,
    IOsuMemoryReaderService osuMemoryReaderService,
    ISukiToastManager toastManager) : ViewModelBase
{
    [NotifyPropertyChangedFor(nameof(HasOriginBeatmap))]
    [ObservableProperty] private SelectedMap _originBeatmap = new();

    [NotifyPropertyChangedFor(nameof(AdditionalBeatmaps))]
    [ObservableProperty]
    private ObservableCollection<SelectedMap> _destinationBeatmaps = [new SelectedMap()];

    [ObservableProperty] private bool _hasMultiple;
    [ObservableProperty] private string _preferredDirectory = string.Empty;

    [NotifyPropertyChangedFor(nameof(HasHeaderBackgroundImage))]
    [ObservableProperty] private Bitmap? _headerBackgroundImage;

    [ObservableProperty] private string _backgroundImagePath = string.Empty;
    [NotifyPropertyChangedFor(nameof(OriginContextTopLine))]
    [ObservableProperty] private string _originArtist = string.Empty;
    [NotifyPropertyChangedFor(nameof(OriginContextTopLine))]
    [ObservableProperty] private string _originSongName = string.Empty;
    [NotifyPropertyChangedFor(nameof(OriginContextBottomLine))]
    [ObservableProperty] private string _originDiffName = string.Empty;

    [ObservableProperty] private int _maxBurstLength = 1;
    [ObservableProperty] private int _paletteSize = 6;
    [ObservableProperty] private bool _isAdvancedOptionsExpanded;

    [ObservableProperty] private bool _updateComboColoursSection = true;
    [ObservableProperty] private bool _overrideHitObjectColourShifts = true;
    [ObservableProperty] private bool _createColoursSectionIfMissing = true;
    [ObservableProperty] private bool _createBackupBeforeWrite = true;

    [ObservableProperty] private ObservableCollection<AvaloniaComboColour> _comboColours = [];
    [ObservableProperty] private ObservableCollection<AvaloniaComboColourPoint> _colourPoints = [];
    [ObservableProperty] private ObservableCollection<ComboColourOption> _colourDropdownOptions = [];
    private ObservableCollection<AvaloniaComboColour>? _observedComboColours;
    private readonly HashSet<AvaloniaComboColour> _observedComboColourItems = [];

    public bool HasHeaderBackgroundImage => HeaderBackgroundImage is not null;
    public bool HasOriginBeatmap => !string.IsNullOrWhiteSpace(OriginBeatmap.Path);
    public string OriginContextTopLine
    {
        get
        {
            var artist = string.IsNullOrWhiteSpace(OriginArtist) ? "Unknown Artist" : OriginArtist;
            var song = string.IsNullOrWhiteSpace(OriginSongName) ? "Unknown Title" : OriginSongName;
            return $"{artist} - {song}";
        }
    }
    public string OriginContextBottomLine
    {
        get
        {
            var diff = string.IsNullOrWhiteSpace(OriginDiffName) ? "Unknown Difficulty" : OriginDiffName;
            return $"[{diff}]";
        }
    }

    public ObservableCollection<SelectedMap> AdditionalBeatmaps
    {
        get => new(DestinationBeatmaps.Skip(1));
        set
        {
            var first = DestinationBeatmaps.FirstOrDefault() ?? new SelectedMap();
            DestinationBeatmaps = new ObservableCollection<SelectedMap>(new[] { first }.Concat(value));
        }
    }

    partial void OnPaletteSizeChanged(int value)
    {
        PaletteSize = value switch
        {
            < 1 => 1,
            > 8 => 8,
            _ => PaletteSize
        };
    }

    [RelayCommand]
    private void RemoveMap(string path)
    {
        var remaining = DestinationBeatmaps.Where(x => x.Path != path).ToList();

        if (remaining.Count == 0)
        {
            remaining.Add(new SelectedMap());
        }

        DestinationBeatmaps = new ObservableCollection<SelectedMap>(remaining);
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    [RelayCommand]
    private void AddColour()
    {
        if (ComboColours.Count >= 8)
        {
            ShowToast(NotificationType.Warning, "Combo Colour Studio", "osu! supports at most 8 combo colours.");
            return;
        }

        var cloneColour = ComboColours.Count > 0
            ? ComboColours[^1].Colour ?? Color.White
            : Color.White;

        ComboColours.Add(new AvaloniaComboColour(ComboColours.Count + 1, cloneColour));
        RefreshComboColourOptions();
        NormalizeColourPointSequences();
    }

    [RelayCommand]
    private void RemoveColour(AvaloniaComboColour colour)
    {
        var index = ComboColours.IndexOf(colour);
        if (index < 0)
        {
            return;
        }

        ComboColours.RemoveAt(index);

        for (var i = 0; i < ComboColours.Count; i++)
        {
            ComboColours[i].Number = i + 1;
        }

        RefreshComboColourOptions();
        NormalizeColourPointSequences();
    }

    [RelayCommand]
    private void AddColourPoint()
    {
        var nextTime = ColourPoints.Count > 0 ? ColourPoints[^1].Time : 0;
        var point = new AvaloniaComboColourPoint
        {
            Time = nextTime,
            Mode = ColourPointMode.Normal,
            ColourSequence = []
        };

        if (ComboColours.Count > 0)
        {
            point.ColourSequence.Add(new AvaloniaComboColourToken { ComboNumber = 1 });
        }

        ColourPoints.Add(point);
    }

    [RelayCommand]
    private void RemoveColourPoint(AvaloniaComboColourPoint colourPoint)
    {
        ColourPoints.Remove(colourPoint);
    }

    [RelayCommand]
    private void AddColourToPoint(AvaloniaComboColourPoint colourPoint)
    {
        if (ComboColours.Count == 0)
        {
            ShowToast(NotificationType.Warning, "Combo Colour Studio", "Add at least one combo colour first.");
            return;
        }

        var defaultNumber = colourPoint.ColourSequence.Count > 0
            ? Math.Clamp(colourPoint.ColourSequence[^1].ComboNumber, 1, ComboColours.Count)
            : 1;

        colourPoint.ColourSequence.Add(new AvaloniaComboColourToken
        {
            ComboNumber = defaultNumber
        });
    }

    [RelayCommand]
    private void RemoveColourFromPoint(AvaloniaComboColourToken token)
    {
        foreach (var point in ColourPoints)
        {
            if (point.ColourSequence.Remove(token))
            {
                break;
            }
        }
    }

    [RelayCommand]
    private void ImportComboColoursFromOrigin()
    {
        if (!EnsureOriginSelected())
        {
            return;
        }

        try
        {
            var project = comboColourStudioService.ImportComboColours(OriginBeatmap.Path);
            LoadProjectIntoUi(project, replaceColourPoints: false);

            ShowToast(NotificationType.Success, "Combo Colour Studio", "Imported combo colours from beatmap.");
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private void GenerateColoursFromBackground()
    {
        if (string.IsNullOrWhiteSpace(BackgroundImagePath))
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", "No beatmap background image is available.");
            return;
        }

        GenerateColoursFromImagePath(BackgroundImagePath);
    }

    [RelayCommand]
    private async Task PickImageAndGenerateColours(CancellationToken token)
    {
        try
        {
            var preferredDirectory = await filesService.TryGetFolderFromPathAsync(
                string.IsNullOrWhiteSpace(PreferredDirectory)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : PreferredDirectory);

            var file = await filesService.OpenFileAsync(new FilePickerOpenOptions
            {
                Title = "Select an image",
                AllowMultiple = false,
                SuggestedStartLocation = preferredDirectory,
                FileTypeFilter =
                [
                    new FilePickerFileType("Image")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"]
                    }
                ]
            });

            if (token.IsCancellationRequested || file is null || file.Count == 0)
            {
                return;
            }

            GenerateColoursFromImagePath(file[0].Path.LocalPath);
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private async Task PickOriginFile(CancellationToken token)
    {
        try
        {
            var preferredDirectory = await filesService.TryGetFolderFromPathAsync(
                string.IsNullOrWhiteSpace(PreferredDirectory)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : PreferredDirectory);

            var file = await filesService.OpenFileAsync(new FilePickerOpenOptions
            {
                Title = "Select beatmap",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("osu! beatmap file")
                    {
                        Patterns = ["*.osu"],
                        MimeTypes = ["application/octet-stream"]
                    }
                ],
                SuggestedStartLocation = preferredDirectory
            });

            if (token.IsCancellationRequested || file is null || file.Count == 0)
            {
                return;
            }

            OriginBeatmap = new SelectedMap { Path = file.First().Path.LocalPath };
            PreferredDirectory = Path.GetDirectoryName(OriginBeatmap.Path) ?? string.Empty;

            LoadOriginBeatmapContext();
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private void SetOriginFromMemory()
    {
        var beatmapPath = GetBeatmapFromMemory();

        if (beatmapPath is null)
        {
            return;
        }

        OriginBeatmap = new SelectedMap { Path = beatmapPath };
        PreferredDirectory = Path.GetDirectoryName(OriginBeatmap.Path) ?? string.Empty;

        LoadOriginBeatmapContext();
    }

    [RelayCommand]
    private async Task PickDestinationFile(CancellationToken token)
    {
        try
        {
            var preferredDirectory = await filesService.TryGetFolderFromPathAsync(
                string.IsNullOrWhiteSpace(PreferredDirectory)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : PreferredDirectory);

            var file = await filesService.OpenFileAsync(new FilePickerOpenOptions
            {
                Title = "Select destination beatmap(s)",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType("osu! beatmap file")
                    {
                        Patterns = ["*.osu"],
                        MimeTypes = ["application/octet-stream"]
                    }
                ],
                SuggestedStartLocation = preferredDirectory
            });

            if (token.IsCancellationRequested || file is null || file.Count == 0)
            {
                return;
            }

            DestinationBeatmaps =
                new ObservableCollection<SelectedMap>(file.Select(f => new SelectedMap { Path = f.Path.LocalPath }));
            HasMultiple = DestinationBeatmaps.Count > 1;
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    [RelayCommand]
    private void AddDestinationFromMemory()
    {
        var beatmapPath = GetBeatmapFromMemory();

        if (beatmapPath is null)
        {
            return;
        }

        var destinationBeatmaps = DestinationBeatmaps;

        if (destinationBeatmaps.Count == 0 ||
            (destinationBeatmaps.Count == 1 && string.IsNullOrEmpty(destinationBeatmaps.First().Path)))
        {
            destinationBeatmaps = [];
        }

        if (destinationBeatmaps.Any(x => string.Equals(x.Path, beatmapPath, StringComparison.OrdinalIgnoreCase)))
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", "This beatmap is already in the destination list.");
            return;
        }

        destinationBeatmaps = new ObservableCollection<SelectedMap>(destinationBeatmaps.Append(new SelectedMap
        {
            Path = beatmapPath
        }));

        DestinationBeatmaps = destinationBeatmaps;
        HasMultiple = DestinationBeatmaps.Count > 1;
    }

    [RelayCommand]
    private void ExportColourHax()
    {
        try
        {
            var destinationPaths = DestinationBeatmaps
                .Select(x => x.Path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (destinationPaths.Length == 0)
            {
                ShowToast(NotificationType.Error, "Combo Colour Studio", "Please select at least one destination beatmap.");
                return;
            }

            var project = BuildProjectFromUi();

            var options = new ComboColourStudioOptions
            {
                UpdateComboColoursSection = UpdateComboColoursSection,
                OverrideHitObjectColourShifts = OverrideHitObjectColourShifts,
                CreateColoursSectionIfMissing = CreateColoursSectionIfMissing,
                CreateBackupBeforeWrite = CreateBackupBeforeWrite
            };

            comboColourStudioService.ApplyProject(project, destinationPaths, options);

            ShowToast(NotificationType.Success,
                "Combo Colour Studio",
                $"Exported colour hax to {destinationPaths.Length} beatmap(s).");
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    private void GenerateColoursFromImagePath(string imagePath)
    {
        try
        {
            var colours = comboColourStudioService.GenerateProminentColours(imagePath, PaletteSize);
            ComboColours = new ObservableCollection<AvaloniaComboColour>(colours
                .Select((colour, index) => new AvaloniaComboColour(index + 1, colour)));

            RefreshComboColourOptions();
            NormalizeColourPointSequences();
            ShowToast(NotificationType.Success, "Combo Colour Studio", "Generated combo colours from image.");
        }
        catch (Exception ex)
        {
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    private bool EnsureOriginSelected()
    {
        if (!string.IsNullOrWhiteSpace(OriginBeatmap.Path))
        {
            return true;
        }

        ShowToast(NotificationType.Error, "Combo Colour Studio", "Please select an origin beatmap.");
        return false;
    }

    private string? GetBeatmapFromMemory()
    {
        var currentBeatmap = osuMemoryReaderService.GetBeatmapPath();

        if (currentBeatmap.Status == ResultStatus.Error)
        {
            ShowToast(NotificationType.Error,
                "Memory Error",
                currentBeatmap.ErrorMessage ?? "Failed to read beatmap path from memory.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(currentBeatmap.Value))
        {
            ShowToast(NotificationType.Error, "Memory Error", "No beatmap is currently loaded in osu!.");
            return null;
        }

        return currentBeatmap.Value;
    }

    private void LoadOriginBeatmapContext()
    {
        try
        {
            var project = comboColourStudioService.ImportComboColours(OriginBeatmap.Path);
            LoadProjectIntoUi(project, replaceColourPoints: false);
            LoadOriginBeatmapMetadata();
            LoadBackgroundImage();

            ShowToast(NotificationType.Success, "Combo Colour Studio", "Loaded beatmap context.");
        }
        catch (Exception ex)
        {
            ClearOriginBeatmapMetadata();
            ShowToast(NotificationType.Error, "Combo Colour Studio", ex.Message);
        }
    }

    private void LoadOriginBeatmapMetadata()
    {
        var beatmap = Beatmap.Decode(File.ReadAllText(OriginBeatmap.Path));
        var metadata = beatmap.MetadataSection;

        OriginArtist = FirstNonEmpty(metadata.ArtistUnicode, metadata.Artist);
        OriginSongName = FirstNonEmpty(metadata.TitleUnicode, metadata.Title);
        OriginDiffName = metadata.Version;
    }

    private void ClearOriginBeatmapMetadata()
    {
        OriginArtist = string.Empty;
        OriginSongName = string.Empty;
        OriginDiffName = string.Empty;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private void LoadProjectIntoUi(ComboColourProject project, bool replaceColourPoints)
    {
        ComboColours = new ObservableCollection<AvaloniaComboColour>(project.ComboColours.Select((combo, index) =>
            new AvaloniaComboColour(index + 1, combo.Colour)));

        if (replaceColourPoints)
        {
            ColourPoints = new ObservableCollection<AvaloniaComboColourPoint>(project.ColourPoints.Select(point =>
                new AvaloniaComboColourPoint
                {
                    Time = point.Time,
                    Mode = point.Mode,
                    ColourSequence = new ObservableCollection<AvaloniaComboColourToken>(
                        point.ColourSequence.Select(index => new AvaloniaComboColourToken
                        {
                            ComboNumber = index + 1
                        }))
                }));
        }

        MaxBurstLength = project.MaxBurstLength;
        RefreshComboColourOptions();
        NormalizeColourPointSequences();
    }

    private void LoadBackgroundImage()
    {
        try
        {
            var backgroundPath = comboColourStudioService.GetBackgroundPath(OriginBeatmap.Path);
            BackgroundImagePath = backgroundPath ?? string.Empty;

            HeaderBackgroundImage?.Dispose();
            HeaderBackgroundImage = null;

            if (!string.IsNullOrWhiteSpace(backgroundPath) && File.Exists(backgroundPath))
            {
                HeaderBackgroundImage = new Bitmap(backgroundPath);
            }
        }
        catch
        {
            BackgroundImagePath = string.Empty;
            HeaderBackgroundImage?.Dispose();
            HeaderBackgroundImage = null;
        }
    }

    private ComboColourProject BuildProjectFromUi()
    {
        var comboColours = ComboColours
            .Select((combo, index) => new ComboColour((uint)(index + 1), combo.Colour ?? Color.White))
            .ToList();

        if (comboColours.Count == 0)
        {
            throw new InvalidOperationException("Please add at least one combo colour.");
        }

        var colourPoints = ColourPoints
            .Select(point => new ToolComboColourPoint
            {
                Time = point.Time,
                Mode = point.Mode,
                ColourSequence = point.ColourSequence
                    .Select(token => token.ComboNumber)
                    .Select(number =>
                    {
                        if (number < 1 || number > comboColours.Count)
                        {
                            throw new InvalidOperationException(
                                $"Colour point at {point.Time:0.##}ms references combo {number}, but only {comboColours.Count} combo colours exist.");
                        }

                        return number - 1;
                    })
                    .ToList()
            })
            .OrderBy(point => point.Time)
            .ToList();

        return new ComboColourProject
        {
            ComboColours = comboColours,
            ColourPoints = colourPoints,
            MaxBurstLength = Math.Max(1, MaxBurstLength)
        };
    }

    private void NormalizeColourPointSequences()
    {
        var comboCount = ComboColours.Count;

        foreach (var point in ColourPoints)
        {
            if (comboCount == 0)
            {
                point.ColourSequence.Clear();
                continue;
            }

            foreach (var token in point.ColourSequence)
            {
                token.ComboNumber = Math.Clamp(token.ComboNumber, 1, comboCount);
            }
        }
    }

    private void RefreshComboColourOptions()
    {
        ObserveComboColours();

        ColourDropdownOptions = new ObservableCollection<ComboColourOption>(
            ComboColours.Select(combo => new ComboColourOption
            {
                Number = combo.Number,
                Colour = combo.Colour ?? Color.White
            }));
    }

    private void ObserveComboColours()
    {
        if (!ReferenceEquals(_observedComboColours, ComboColours))
        {
            if (_observedComboColours is not null)
            {
                _observedComboColours.CollectionChanged -= ComboColoursOnCollectionChanged;
            }

            foreach (var observed in _observedComboColourItems)
            {
                observed.PropertyChanged -= ComboColourOnPropertyChanged;
            }

            _observedComboColourItems.Clear();
            _observedComboColours = ComboColours;
            _observedComboColours.CollectionChanged += ComboColoursOnCollectionChanged;
        }

        foreach (var comboColour in ComboColours)
        {
            if (_observedComboColourItems.Add(comboColour))
            {
                comboColour.PropertyChanged += ComboColourOnPropertyChanged;
            }
        }

        var removedItems = _observedComboColourItems
            .Where(comboColour => !ComboColours.Contains(comboColour))
            .ToList();

        foreach (var removedItem in removedItems)
        {
            removedItem.PropertyChanged -= ComboColourOnPropertyChanged;
            _observedComboColourItems.Remove(removedItem);
        }
    }

    private void ComboColoursOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ObserveComboColours();
        RefreshComboColourOptions();
    }

    private void ComboColourOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AvaloniaComboColour.Colour) || e.PropertyName == nameof(AvaloniaComboColour.Number))
        {
            RefreshComboColourOptions();
        }
    }

    private void ShowToast(NotificationType type, string title, string message)
    {
        toastManager.CreateToast()
            .OfType(type)
            .WithTitle(title)
            .WithContent(message)
            .Dismiss().ByClicking()
            .Dismiss().After(TimeSpan.FromSeconds(8))
            .Queue();
    }
}
