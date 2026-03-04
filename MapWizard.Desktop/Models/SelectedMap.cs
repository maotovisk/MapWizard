using System;
using System.IO;
using Avalonia.Media.Imaging;
using BeatmapParser;
using CommunityToolkit.Mvvm.ComponentModel;
using MapWizard.Desktop.Utils;

namespace MapWizard.Desktop.Models;

public class SelectedMap : ObservableObject
{
    private string _path = string.Empty;
    private string _displayTitle = "Unknown beatmap";
    private string _displaySubtitle = string.Empty;
    private string _displayDetails = string.Empty;
    private Bitmap? _backgroundImage;

    public string Path
    {
        get => _path;
        set
        {
            if (!SetProperty(ref _path, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasPath));
            LoadBeatmapCardData();
        }
    }

    public bool HasPath => !string.IsNullOrWhiteSpace(Path);

    public string DisplayTitle
    {
        get => _displayTitle;
        private set => SetProperty(ref _displayTitle, value);
    }

    public string DisplaySubtitle
    {
        get => _displaySubtitle;
        private set => SetProperty(ref _displaySubtitle, value);
    }

    public string DisplayDetails
    {
        get => _displayDetails;
        private set => SetProperty(ref _displayDetails, value);
    }

    public Bitmap? BackgroundImage
    {
        get => _backgroundImage;
        private set => ReplaceBackgroundImage(value);
    }

    public bool HasBackgroundImage => BackgroundImage is not null;

    private void LoadBeatmapCardData()
    {
        var normalizedPath = Path.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            ResetCardData();
            return;
        }

        try
        {
            var fullPath = System.IO.Path.GetFullPath(normalizedPath);
            if (!File.Exists(fullPath))
            {
                ApplyFallbackCardData(fullPath);
                return;
            }

            var beatmap = Beatmap.Decode(File.ReadAllText(fullPath));
            var metadata = beatmap.MetadataSection;

            var artist = StringValueUtils.FirstNonEmpty(metadata.ArtistUnicode, metadata.Artist, "Unknown Artist");
            var title = StringValueUtils.FirstNonEmpty(metadata.TitleUnicode, metadata.Title, "Unknown Title");
            var difficulty = StringValueUtils.FirstNonEmpty(metadata.Version, "Unknown Difficulty");
            var creator = StringValueUtils.FirstNonEmpty(metadata.Creator, "Unknown Mapper");

            DisplayTitle = $"{artist} - {title}";
            DisplaySubtitle = $"[{difficulty}]";
            DisplayDetails = $"Mapped by {creator}";

            var backgroundRelativePath = beatmap.GetBgFilename();
            var resolvedBackgroundPath = MapsetAssetPathUtils.ResolveRelativePathFromBeatmap(fullPath, backgroundRelativePath);
            LoadBackgroundImage(resolvedBackgroundPath);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            ApplyFallbackCardData(normalizedPath);
        }
    }

    private void ResetCardData()
    {
        DisplayTitle = "Unknown beatmap";
        DisplaySubtitle = string.Empty;
        DisplayDetails = string.Empty;
        BackgroundImage = null;
    }

    private void ApplyFallbackCardData(string path)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        DisplayTitle = string.IsNullOrWhiteSpace(fileName) ? "Unknown beatmap" : fileName;
        DisplaySubtitle = System.IO.Path.GetFileName(path);
        DisplayDetails = "Metadata unavailable";
        BackgroundImage = null;
    }

    private void LoadBackgroundImage(string? backgroundPath)
    {
        if (string.IsNullOrWhiteSpace(backgroundPath))
        {
            BackgroundImage = null;
            return;
        }

        try
        {
            var fullPath = System.IO.Path.GetFullPath(backgroundPath);
            BackgroundImage = File.Exists(fullPath) ? new Bitmap(fullPath) : null;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            BackgroundImage = null;
        }
    }

    private void ReplaceBackgroundImage(Bitmap? newImage)
    {
        if (ReferenceEquals(_backgroundImage, newImage))
        {
            return;
        }

        var previous = _backgroundImage;
        _backgroundImage = newImage;
        OnPropertyChanged(nameof(BackgroundImage));
        OnPropertyChanged(nameof(HasBackgroundImage));
        previous?.Dispose();
    }
}
