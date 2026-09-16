using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Utils;

namespace MapWizard.Desktop.Views.Dialogs;

public enum BeatmapSourceKind
{
    Lazer,
    Stable
}

public partial class BeatmapSourceCard : UserControl, IDisposable
{
    public static readonly StyledProperty<string> SourceLabelProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, string>(nameof(SourceLabel), string.Empty);

    public static readonly StyledProperty<string> ArtistAndTitleProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, string>(nameof(ArtistAndTitle), string.Empty);

    public static readonly StyledProperty<string> CreatorLabelProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, string>(nameof(CreatorLabel), string.Empty);

    public static readonly StyledProperty<IReadOnlyList<MapsetDifficultyCard>> DifficultiesProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, IReadOnlyList<MapsetDifficultyCard>>(
            nameof(Difficulties),
            Array.Empty<MapsetDifficultyCard>());

    public static readonly StyledProperty<Bitmap?> BackgroundImageProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, Bitmap?>(nameof(BackgroundImage));

    public static readonly StyledProperty<bool> HasBackgroundImageProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, bool>(nameof(HasBackgroundImage));

    public static readonly StyledProperty<IBrush> SourceBadgeBrushProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, IBrush>(nameof(SourceBadgeBrush), Brushes.Transparent);

    public static readonly StyledProperty<bool> IsFallbackProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, bool>(nameof(IsFallback));

    public static readonly StyledProperty<string> FallbackDetailLabelProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, string>(nameof(FallbackDetailLabel), string.Empty);

    public static readonly StyledProperty<string> FallbackTitleLabelProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, string>(nameof(FallbackTitleLabel), string.Empty);

    public static readonly StyledProperty<IBrush> FallbackBackgroundBrushProperty =
        AvaloniaProperty.Register<BeatmapSourceCard, IBrush>(
            nameof(FallbackBackgroundBrush),
            new SolidColorBrush(Color.Parse("#141414")));

    private string? fallbackPath;

    public string SourceLabel
    {
        get => GetValue(SourceLabelProperty);
        private set => SetValue(SourceLabelProperty, value);
    }

    public string ArtistAndTitle
    {
        get => GetValue(ArtistAndTitleProperty);
        private set => SetValue(ArtistAndTitleProperty, value);
    }

    public string CreatorLabel
    {
        get => GetValue(CreatorLabelProperty);
        private set => SetValue(CreatorLabelProperty, value);
    }

    public IReadOnlyList<MapsetDifficultyCard> Difficulties
    {
        get => GetValue(DifficultiesProperty);
        private set => SetValue(DifficultiesProperty, value);
    }

    public Bitmap? BackgroundImage
    {
        get => GetValue(BackgroundImageProperty);
        private set => SetValue(BackgroundImageProperty, value);
    }

    public bool HasBackgroundImage
    {
        get => GetValue(HasBackgroundImageProperty);
        private set => SetValue(HasBackgroundImageProperty, value);
    }

    public IBrush SourceBadgeBrush
    {
        get => GetValue(SourceBadgeBrushProperty);
        private set => SetValue(SourceBadgeBrushProperty, value);
    }

    public bool IsFallback
    {
        get => GetValue(IsFallbackProperty);
        private set => SetValue(IsFallbackProperty, value);
    }

    public string FallbackDetailLabel
    {
        get => GetValue(FallbackDetailLabelProperty);
        private set => SetValue(FallbackDetailLabelProperty, value);
    }

    public string FallbackTitleLabel
    {
        get => GetValue(FallbackTitleLabelProperty);
        private set => SetValue(FallbackTitleLabelProperty, value);
    }

    public IBrush FallbackBackgroundBrush
    {
        get => GetValue(FallbackBackgroundBrushProperty);
        private set => SetValue(FallbackBackgroundBrushProperty, value);
    }

    public event Action<string>? DifficultySelected;

    public BeatmapSourceCard()
    {
        InitializeComponent();
    }

    public BeatmapSourceCard(BeatmapSourceKind sourceKind, IReadOnlyList<string> beatmapPaths)
        : this()
    {
        if (beatmapPaths.Count == 0)
        {
            throw new ArgumentException("At least one beatmap path is required.", nameof(beatmapPaths));
        }

        var isLazer = sourceKind == BeatmapSourceKind.Lazer;
        SourceLabel = isLazer ? "Mounted in osu!lazer" : "Currently on osu!stable";
        SourceBadgeBrush = new SolidColorBrush(Color.Parse(isLazer ? "#D9463572" : "#D96E294A"));
        IsFallback = !isLazer;
        Difficulties = beatmapPaths
            .Select(path => new MapsetDifficultyCard(path))
            .ToArray();
        fallbackPath = IsFallback ? Difficulties[0].Path : null;

        var firstPath = beatmapPaths[0];
        var artist = "Unknown Artist";
        var title = "Unknown Title";
        var creator = "Unknown Mapper";
        string? backgroundFilename = null;

        try
        {
            var info = BeatmapCardInfoReader.Read(firstPath);
            artist = string.IsNullOrWhiteSpace(info.Artist) ? artist : info.Artist;
            title = string.IsNullOrWhiteSpace(info.Title) ? title : info.Title;
            creator = string.IsNullOrWhiteSpace(info.Creator) ? creator : info.Creator;
            backgroundFilename = info.BackgroundFilename;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
        }

        ArtistAndTitle = $"{artist} - {title}";
        CreatorLabel = $"Mapped by {creator}";
        FallbackTitleLabel = IsFallback
            ? $"{ArtistAndTitle} [{Difficulties[0].DifficultyLabel}]"
            : string.Empty;
        FallbackDetailLabel = IsFallback
            ? $"mapped by {creator}"
            : string.Empty;
        BackgroundImage = TryLoadBackground(firstPath, backgroundFilename);
        HasBackgroundImage = BackgroundImage is not null;
        FallbackBackgroundBrush = BackgroundImage is null
            ? new SolidColorBrush(Color.Parse("#141414"))
            : new ImageBrush(BackgroundImage)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
    }

    public void Dispose()
    {
        BackgroundImage?.Dispose();
    }

    private void DifficultyButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: MapsetDifficultyCard difficulty })
        {
            DifficultySelected?.Invoke(difficulty.Path);
        }
    }

    private void FallbackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(fallbackPath))
        {
            DifficultySelected?.Invoke(fallbackPath);
        }
    }

    private static Bitmap? TryLoadBackground(string beatmapPath, string? backgroundFilename)
    {
        try
        {
            var backgroundPath = MapsetAssetPathUtils.ResolveRelativePathFromBeatmap(beatmapPath, backgroundFilename);
            return string.IsNullOrWhiteSpace(backgroundPath) || !File.Exists(backgroundPath)
                ? null
                : ArtworkBitmapUtils.DecodePreview(backgroundPath, 480);
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return null;
        }
    }
}
