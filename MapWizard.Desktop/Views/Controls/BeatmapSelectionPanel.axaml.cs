using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MapWizard.Desktop.Models;
using MapWizard.Desktop.Utils;

namespace MapWizard.Desktop.Views.Controls;

public partial class BeatmapSelectionPanel : UserControl
{
    private SelectedMap? _observedOriginMap;
    private INotifyCollectionChanged? _observedDestinationCollection;
    private readonly HashSet<SelectedMap> _observedDestinationMaps = [];
    private readonly Dictionary<string, bool> _destinationMapsetExpansionStates = new(System.StringComparer.OrdinalIgnoreCase);
    private bool _isBulkUpdatingDestinationMaps;

    public static readonly StyledProperty<string> SectionTitleProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(SectionTitle), "Beatmap Selection");

    public static readonly StyledProperty<string> FromPrefixProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(FromPrefix), "From: ");

    public static readonly StyledProperty<string> ToPrefixProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(ToPrefix), "To: ");

    public static readonly StyledProperty<string> AdditionalPrefixProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(AdditionalPrefix), "To: ");

    public static readonly StyledProperty<string> FromWatermarkProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(FromWatermark), "Import metadata from...");

    public static readonly StyledProperty<string> ToWatermarkProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(ToWatermark), "Export metadata to...");

    public static readonly StyledProperty<string> OriginMemoryToolTipProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(
            nameof(OriginMemoryToolTip),
            "Use currently-selected osu! map");

    public static readonly StyledProperty<string> DestinationMemoryToolTipProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(
            nameof(DestinationMemoryToolTip),
            "Add currently-selected osu! map");

    public static readonly StyledProperty<bool> ShowDestinationSelectionProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowDestinationSelection), true);

    public static readonly StyledProperty<bool> ShowDestinationSectionProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowDestinationSection), true);

    public static readonly StyledProperty<string> OriginEmptyPromptProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(OriginEmptyPrompt), "Select an origin beatmap");

    public static readonly StyledProperty<bool> ShowHeaderBackgroundProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowHeaderBackground));

    public static readonly StyledProperty<IImage?> HeaderBackgroundImageProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, IImage?>(nameof(HeaderBackgroundImage));

    public static readonly StyledProperty<bool> ShowHeaderContextProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowHeaderContext), true);

    public static readonly StyledProperty<string> HeaderTopLineProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(HeaderTopLine), string.Empty);

    public static readonly StyledProperty<string> HeaderBottomLineProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, string>(nameof(HeaderBottomLine), string.Empty);

    public static readonly StyledProperty<bool> ShowHeaderBottomLineProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowHeaderBottomLine));

    public static readonly StyledProperty<double> HeaderOverlayHeightProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, double>(nameof(HeaderOverlayHeight), 56d);

    public static readonly StyledProperty<double> HeaderMaxHeightProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, double>(nameof(HeaderMaxHeight), 140d);

    public static readonly StyledProperty<bool> IsEditingOriginPathProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(IsEditingOriginPath));

    public static readonly StyledProperty<bool> ShowOriginSummaryProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowOriginSummary), true);

    public static readonly StyledProperty<bool> IsEditingDestinationPathProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(IsEditingDestinationPath));

    public static readonly StyledProperty<bool> ShowDestinationSummaryProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowDestinationSummary), true);

    public static readonly StyledProperty<ICommand?> OriginPathChangedCommandProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, ICommand?>(nameof(OriginPathChangedCommand));

    public static readonly StyledProperty<SelectedMap?> OriginMapProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, SelectedMap?>(nameof(OriginMap));

    public static readonly StyledProperty<ObservableCollection<SelectedMap>?> DestinationMapsProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, ObservableCollection<SelectedMap>?>(nameof(DestinationMaps));

    public static readonly StyledProperty<bool> HasOriginSelectionProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(HasOriginSelection));

    public static readonly StyledProperty<bool> ShowOriginEmptyStateProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowOriginEmptyState), true);

    public static readonly StyledProperty<bool> HasDestinationSelectionProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(HasDestinationSelection));

    public static readonly StyledProperty<bool> ShowDestinationEmptyStateProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowDestinationEmptyState), true);

    public static readonly StyledProperty<int> SelectedDestinationCountProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, int>(nameof(SelectedDestinationCount));

    public static readonly StyledProperty<bool> HasMapsetDifficultyOptionsProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(HasMapsetDifficultyOptions));

    public static readonly StyledProperty<int> SelectedMapsetDifficultyCountProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, int>(nameof(SelectedMapsetDifficultyCount));

    public static readonly StyledProperty<bool> HasSelectedMapsetDifficultiesProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(HasSelectedMapsetDifficulties));

    public static readonly StyledProperty<bool> CanSelectAllMapsetDifficultiesProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(CanSelectAllMapsetDifficulties));

    public static readonly StyledProperty<bool> AreAllMapsetDifficultiesSelectedProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(AreAllMapsetDifficultiesSelected));

    public static readonly StyledProperty<bool> IsMapsetDiffExpandedProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(IsMapsetDiffExpanded), true);

    public static readonly StyledProperty<bool> ShowMapsetOrSeparatorProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(ShowMapsetOrSeparator));

    public static readonly StyledProperty<bool> HasVisibleDestinationCardsProperty =
        AvaloniaProperty.Register<BeatmapSelectionPanel, bool>(nameof(HasVisibleDestinationCards));

    public BeatmapSelectionPanel()
    {
        InitializeComponent();

        OriginMap ??= new SelectedMap();
        DestinationMaps ??= [];

        AttachOriginMapObserver(OriginMap);
        AttachDestinationMapCollectionObserver(DestinationMaps);
        UpdateOriginSelectionState();
        UpdateDestinationSelectionState();
        RebuildMapsetDifficultyCards();
        UpdateOriginEmptyPrompt();
    }

    public ObservableCollection<MapsetDifficultyCard> MapsetDifficultyCards { get; } = [];
    public ObservableCollection<DestinationMapsetCard> VisibleDestinationMapsets { get; } = [];

    public string SectionTitle
    {
        get => GetValue(SectionTitleProperty);
        set => SetValue(SectionTitleProperty, value);
    }

    public string FromPrefix
    {
        get => GetValue(FromPrefixProperty);
        set => SetValue(FromPrefixProperty, value);
    }

    public string ToPrefix
    {
        get => GetValue(ToPrefixProperty);
        set => SetValue(ToPrefixProperty, value);
    }

    public string AdditionalPrefix
    {
        get => GetValue(AdditionalPrefixProperty);
        set => SetValue(AdditionalPrefixProperty, value);
    }

    public string FromWatermark
    {
        get => GetValue(FromWatermarkProperty);
        set => SetValue(FromWatermarkProperty, value);
    }

    public string ToWatermark
    {
        get => GetValue(ToWatermarkProperty);
        set => SetValue(ToWatermarkProperty, value);
    }

    public string OriginMemoryToolTip
    {
        get => GetValue(OriginMemoryToolTipProperty);
        set => SetValue(OriginMemoryToolTipProperty, value);
    }

    public string DestinationMemoryToolTip
    {
        get => GetValue(DestinationMemoryToolTipProperty);
        set => SetValue(DestinationMemoryToolTipProperty, value);
    }

    public bool ShowDestinationSelection
    {
        get => GetValue(ShowDestinationSelectionProperty);
        set => SetValue(ShowDestinationSelectionProperty, value);
    }

    public bool ShowDestinationSection
    {
        get => GetValue(ShowDestinationSectionProperty);
        private set => SetValue(ShowDestinationSectionProperty, value);
    }

    public string OriginEmptyPrompt
    {
        get => GetValue(OriginEmptyPromptProperty);
        private set => SetValue(OriginEmptyPromptProperty, value);
    }

    public bool ShowHeaderBackground
    {
        get => GetValue(ShowHeaderBackgroundProperty);
        set => SetValue(ShowHeaderBackgroundProperty, value);
    }

    public IImage? HeaderBackgroundImage
    {
        get => GetValue(HeaderBackgroundImageProperty);
        set => SetValue(HeaderBackgroundImageProperty, value);
    }

    public bool ShowHeaderContext
    {
        get => GetValue(ShowHeaderContextProperty);
        set => SetValue(ShowHeaderContextProperty, value);
    }

    public string HeaderTopLine
    {
        get => GetValue(HeaderTopLineProperty);
        set => SetValue(HeaderTopLineProperty, value);
    }

    public string HeaderBottomLine
    {
        get => GetValue(HeaderBottomLineProperty);
        set => SetValue(HeaderBottomLineProperty, value);
    }

    public bool ShowHeaderBottomLine
    {
        get => GetValue(ShowHeaderBottomLineProperty);
        set => SetValue(ShowHeaderBottomLineProperty, value);
    }

    public double HeaderOverlayHeight
    {
        get => GetValue(HeaderOverlayHeightProperty);
        set => SetValue(HeaderOverlayHeightProperty, value);
    }

    public double HeaderMaxHeight
    {
        get => GetValue(HeaderMaxHeightProperty);
        set => SetValue(HeaderMaxHeightProperty, value);
    }

    public bool IsEditingOriginPath
    {
        get => GetValue(IsEditingOriginPathProperty);
        set => SetValue(IsEditingOriginPathProperty, value);
    }

    public bool ShowOriginSummary
    {
        get => GetValue(ShowOriginSummaryProperty);
        set => SetValue(ShowOriginSummaryProperty, value);
    }

    public bool IsEditingDestinationPath
    {
        get => GetValue(IsEditingDestinationPathProperty);
        set => SetValue(IsEditingDestinationPathProperty, value);
    }

    public bool ShowDestinationSummary
    {
        get => GetValue(ShowDestinationSummaryProperty);
        set => SetValue(ShowDestinationSummaryProperty, value);
    }

    public ICommand? OriginPathChangedCommand
    {
        get => GetValue(OriginPathChangedCommandProperty);
        set => SetValue(OriginPathChangedCommandProperty, value);
    }

    public SelectedMap? OriginMap
    {
        get => GetValue(OriginMapProperty);
        set => SetValue(OriginMapProperty, value);
    }

    public ObservableCollection<SelectedMap>? DestinationMaps
    {
        get => GetValue(DestinationMapsProperty);
        set => SetValue(DestinationMapsProperty, value);
    }

    public bool HasOriginSelection
    {
        get => GetValue(HasOriginSelectionProperty);
        private set => SetValue(HasOriginSelectionProperty, value);
    }

    public bool ShowOriginEmptyState
    {
        get => GetValue(ShowOriginEmptyStateProperty);
        private set => SetValue(ShowOriginEmptyStateProperty, value);
    }

    public bool HasDestinationSelection
    {
        get => GetValue(HasDestinationSelectionProperty);
        private set => SetValue(HasDestinationSelectionProperty, value);
    }

    public bool ShowDestinationEmptyState
    {
        get => GetValue(ShowDestinationEmptyStateProperty);
        private set => SetValue(ShowDestinationEmptyStateProperty, value);
    }

    public int SelectedDestinationCount
    {
        get => GetValue(SelectedDestinationCountProperty);
        private set => SetValue(SelectedDestinationCountProperty, value);
    }

    public bool HasMapsetDifficultyOptions
    {
        get => GetValue(HasMapsetDifficultyOptionsProperty);
        private set => SetValue(HasMapsetDifficultyOptionsProperty, value);
    }

    public int SelectedMapsetDifficultyCount
    {
        get => GetValue(SelectedMapsetDifficultyCountProperty);
        private set => SetValue(SelectedMapsetDifficultyCountProperty, value);
    }

    public bool HasSelectedMapsetDifficulties
    {
        get => GetValue(HasSelectedMapsetDifficultiesProperty);
        private set => SetValue(HasSelectedMapsetDifficultiesProperty, value);
    }

    public bool CanSelectAllMapsetDifficulties
    {
        get => GetValue(CanSelectAllMapsetDifficultiesProperty);
        private set => SetValue(CanSelectAllMapsetDifficultiesProperty, value);
    }

    public bool AreAllMapsetDifficultiesSelected
    {
        get => GetValue(AreAllMapsetDifficultiesSelectedProperty);
        private set => SetValue(AreAllMapsetDifficultiesSelectedProperty, value);
    }

    public bool IsMapsetDiffExpanded
    {
        get => GetValue(IsMapsetDiffExpandedProperty);
        set => SetValue(IsMapsetDiffExpandedProperty, value);
    }

    public bool ShowMapsetOrSeparator
    {
        get => GetValue(ShowMapsetOrSeparatorProperty);
        private set => SetValue(ShowMapsetOrSeparatorProperty, value);
    }

    public bool HasVisibleDestinationCards
    {
        get => GetValue(HasVisibleDestinationCardsProperty);
        private set => SetValue(HasVisibleDestinationCardsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OriginMapProperty)
        {
            DetachOriginMapObserver(_observedOriginMap);
            AttachOriginMapObserver(change.GetNewValue<SelectedMap?>());
            UpdateOriginSelectionState();
            RebuildMapsetDifficultyCards();
            return;
        }

        if (change.Property == DestinationMapsProperty)
        {
            DetachDestinationMapCollectionObserver(_observedDestinationCollection);
            AttachDestinationMapCollectionObserver(change.GetNewValue<ObservableCollection<SelectedMap>?>());
            UpdateDestinationSelectionState();
            RefreshMapsetDifficultySelectionState();
            return;
        }

        if (change.Property == ShowDestinationSelectionProperty && !ShowDestinationSelection)
        {
            IsMapsetDiffExpanded = false;
        }

        if (change.Property == ShowDestinationSelectionProperty ||
            change.Property == HasOriginSelectionProperty)
        {
            UpdateShowDestinationSection();
        }

        if (change.Property == ShowDestinationSelectionProperty)
        {
            UpdateOriginEmptyPrompt();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachOriginMapObserver(OriginMap);
        AttachDestinationMapCollectionObserver(DestinationMaps);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DetachOriginMapObserver(_observedOriginMap);
        DetachDestinationMapCollectionObserver(_observedDestinationCollection);
    }

    private void AttachOriginMapObserver(SelectedMap? originMap)
    {
        if (originMap is null || ReferenceEquals(originMap, _observedOriginMap))
        {
            return;
        }

        _observedOriginMap = originMap;
        _observedOriginMap.PropertyChanged += OriginMapOnPropertyChanged;
    }

    private void DetachOriginMapObserver(SelectedMap? originMap)
    {
        if (originMap is null)
        {
            return;
        }

        originMap.PropertyChanged -= OriginMapOnPropertyChanged;
        if (ReferenceEquals(_observedOriginMap, originMap))
        {
            _observedOriginMap = null;
        }
    }

    private void OriginMapOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) &&
            e.PropertyName != nameof(SelectedMap.Path) &&
            e.PropertyName != nameof(SelectedMap.HasPath))
        {
            return;
        }

        UpdateOriginSelectionState();
        RebuildMapsetDifficultyCards();
    }

    private void AttachDestinationMapCollectionObserver(ObservableCollection<SelectedMap>? destinationMaps)
    {
        if (destinationMaps is null || ReferenceEquals(destinationMaps, _observedDestinationCollection))
        {
            return;
        }

        _observedDestinationCollection = destinationMaps;
        _observedDestinationCollection.CollectionChanged += DestinationMapsOnCollectionChanged;
        SyncDestinationMapItemObservers(destinationMaps);
    }

    private void DetachDestinationMapCollectionObserver(INotifyCollectionChanged? destinationMaps)
    {
        if (destinationMaps is not null)
        {
            destinationMaps.CollectionChanged -= DestinationMapsOnCollectionChanged;
        }

        if (ReferenceEquals(_observedDestinationCollection, destinationMaps))
        {
            _observedDestinationCollection = null;
        }

        foreach (var map in _observedDestinationMaps)
        {
            map.PropertyChanged -= DestinationMapOnPropertyChanged;
        }

        _observedDestinationMaps.Clear();
    }

    private void DestinationMapsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DestinationMaps is null)
        {
            return;
        }

        if (_isBulkUpdatingDestinationMaps)
        {
            return;
        }

        SyncDestinationMapItemObservers(DestinationMaps);
        UpdateDestinationSelectionState();
        RefreshMapsetDifficultySelectionState();
    }

    private void SyncDestinationMapItemObservers(IEnumerable<SelectedMap> maps)
    {
        var current = maps.ToList();

        foreach (var map in current.Where(map => _observedDestinationMaps.Add(map)))
        {
            map.PropertyChanged += DestinationMapOnPropertyChanged;
        }

        var removed = _observedDestinationMaps
            .Where(map => !current.Contains(map))
            .ToList();

        foreach (var map in removed)
        {
            map.PropertyChanged -= DestinationMapOnPropertyChanged;
            _observedDestinationMaps.Remove(map);
        }
    }

    private void DestinationMapOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) &&
            e.PropertyName != nameof(SelectedMap.Path) &&
            e.PropertyName != nameof(SelectedMap.HasPath))
        {
            return;
        }

        UpdateDestinationSelectionState();
        RefreshMapsetDifficultySelectionState();
    }

    private void UpdateOriginSelectionState()
    {
        var hasOrigin = OriginMap?.HasPath == true;
        HasOriginSelection = hasOrigin;
        ShowOriginEmptyState = !hasOrigin;
        UpdateShowDestinationSection();
    }

    private void UpdateShowDestinationSection()
    {
        ShowDestinationSection = ShowDestinationSelection && HasOriginSelection;
    }

    private void UpdateOriginEmptyPrompt()
    {
        OriginEmptyPrompt = ShowDestinationSelection
            ? "Select an origin beatmap"
            : "Select a beatmap";
    }

    private void UpdateDestinationSelectionState()
    {
        var selectedCount = DestinationMaps is null
            ? 0
            : DestinationMaps.Count(map => map.HasPath);

        SelectedDestinationCount = selectedCount;
        HasDestinationSelection = selectedCount > 0;
        ShowDestinationEmptyState = !HasDestinationSelection;
        RebuildVisibleDestinationMaps();
    }

    private void RebuildMapsetDifficultyCards()
    {
        MapsetDifficultyCards.Clear();

        if (OriginMap is null || !OriginMap.HasPath)
        {
            HasMapsetDifficultyOptions = false;
            SelectedMapsetDifficultyCount = 0;
            HasSelectedMapsetDifficulties = false;
            CanSelectAllMapsetDifficulties = false;
            AreAllMapsetDifficultiesSelected = false;
            ShowMapsetOrSeparator = false;
            IsMapsetDiffExpanded = false;
            RebuildVisibleDestinationMaps();
            return;
        }

        var selectedDestinationPaths = new HashSet<string>(
            DestinationMaps?.Where(map => map.HasPath).Select(map => map.Path) ?? [],
            System.StringComparer.OrdinalIgnoreCase);

        var siblingDiffPaths = BeatmapSelectionUtils.GetSiblingDifficultyPaths(OriginMap.Path)
            .ToArray();

        foreach (var path in siblingDiffPaths)
        {
            var card = new MapsetDifficultyCard(path)
            {
                IsSelected = selectedDestinationPaths.Contains(path)
            };
            MapsetDifficultyCards.Add(card);
        }

        HasMapsetDifficultyOptions = MapsetDifficultyCards.Count > 0;
        var totalMapsetCount = MapsetDifficultyCards.Count;
        var selectedMapsetCount = MapsetDifficultyCards.Count(card => card.IsSelected);
        SelectedMapsetDifficultyCount = selectedMapsetCount;
        HasSelectedMapsetDifficulties = selectedMapsetCount > 0;
        CanSelectAllMapsetDifficulties = totalMapsetCount > 0;
        AreAllMapsetDifficultiesSelected = totalMapsetCount > 0 && selectedMapsetCount == totalMapsetCount;
        var hasMapsetSelection = selectedMapsetCount > 0;
        ShowMapsetOrSeparator = HasMapsetDifficultyOptions && !hasMapsetSelection;
        if (hasMapsetSelection)
        {
            IsMapsetDiffExpanded = true;
        }
        if (!HasMapsetDifficultyOptions)
        {
            IsMapsetDiffExpanded = false;
        }

        RebuildVisibleDestinationMaps();
    }

    private void RefreshMapsetDifficultySelectionState()
    {
        var selectedDestinationPaths = new HashSet<string>(
            DestinationMaps?.Where(map => map.HasPath).Select(map => map.Path) ?? [],
            System.StringComparer.OrdinalIgnoreCase);

        foreach (var card in MapsetDifficultyCards)
        {
            card.IsSelected = selectedDestinationPaths.Contains(card.Path);
        }

        HasMapsetDifficultyOptions = MapsetDifficultyCards.Count > 0;
        var totalMapsetCount = MapsetDifficultyCards.Count;
        var selectedMapsetCount = MapsetDifficultyCards.Count(card => card.IsSelected);
        SelectedMapsetDifficultyCount = selectedMapsetCount;
        HasSelectedMapsetDifficulties = selectedMapsetCount > 0;
        CanSelectAllMapsetDifficulties = totalMapsetCount > 0;
        AreAllMapsetDifficultiesSelected = totalMapsetCount > 0 && selectedMapsetCount == totalMapsetCount;
        var hasMapsetSelection = selectedMapsetCount > 0;
        ShowMapsetOrSeparator = HasMapsetDifficultyOptions && !hasMapsetSelection;
        if (hasMapsetSelection)
        {
            IsMapsetDiffExpanded = true;
        }
        RebuildVisibleDestinationMaps();
    }

    private void RebuildVisibleDestinationMaps()
    {
        VisibleDestinationMapsets.Clear();

        if (DestinationMaps is null)
        {
            HasVisibleDestinationCards = false;
            return;
        }

        var selectedDestinationMaps = DestinationMaps
            .Where(map => map.HasPath)
            .ToList();

        var selectedPaths = new HashSet<string>(
            selectedDestinationMaps.Select(map => map.Path),
            System.StringComparer.OrdinalIgnoreCase);

        var groupedByMapset = selectedDestinationMaps
            .GroupBy(map => GetDestinationMapsetKey(map.Path), System.StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.First().DisplayTitle, System.StringComparer.OrdinalIgnoreCase);

        var originMapsetKey = OriginMap is not null && OriginMap.HasPath
            ? GetDestinationMapsetKey(OriginMap.Path)
            : null;

        var visibleMapsetKeys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedByMapset)
        {
            var mapsetKey = group.Key;

            // Origin mapset selections are already represented by the "Difficulties from origin" section.
            if (HasMapsetDifficultyOptions &&
                !string.IsNullOrWhiteSpace(originMapsetKey) &&
                string.Equals(mapsetKey, originMapsetKey, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            visibleMapsetKeys.Add(mapsetKey);

            var referenceMap = group.First();
            var mapsetDifficultyPaths = GetMapsetDifficultyPaths(referenceMap.Path, group.Select(map => map.Path));
            var difficulties = mapsetDifficultyPaths
                .Select(path => new MapsetDifficultyCard(path)
                {
                    IsSelected = selectedPaths.Contains(path)
                })
                .ToList();

            var card = new DestinationMapsetCard(
                mapsetKey,
                referenceMap,
                difficulties)
            {
                IsExpanded = !_destinationMapsetExpansionStates.TryGetValue(mapsetKey, out var isExpanded) || isExpanded
            };

            VisibleDestinationMapsets.Add(card);
        }

        var staleExpansionKeys = _destinationMapsetExpansionStates.Keys
            .Where(key => !visibleMapsetKeys.Contains(key))
            .ToArray();
        foreach (var staleKey in staleExpansionKeys)
        {
            _destinationMapsetExpansionStates.Remove(staleKey);
        }

        HasVisibleDestinationCards = VisibleDestinationMapsets.Count > 0;
    }

    private static string GetDestinationMapsetKey(string beatmapPath)
    {
        var mapsetDirectoryPath = BeatmapPathUtils.TryGetMapsetDirectoryPath(beatmapPath);
        if (!string.IsNullOrWhiteSpace(mapsetDirectoryPath))
        {
            return mapsetDirectoryPath;
        }

        try
        {
            return System.IO.Path.GetFullPath(beatmapPath);
        }
        catch
        {
            return beatmapPath;
        }
    }

    private static IReadOnlyList<string> GetMapsetDifficultyPaths(string referencePath, IEnumerable<string> selectedPathsInMapset)
    {
        var siblingPaths = BeatmapSelectionUtils.GetSiblingDifficultyPaths(referencePath)
            .ToList();

        if (siblingPaths.Count == 0)
        {
            return selectedPathsInMapset
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var seen = new HashSet<string>(siblingPaths, System.StringComparer.OrdinalIgnoreCase);
        foreach (var selectedPath in selectedPathsInMapset)
        {
            if (seen.Add(selectedPath))
            {
                siblingPaths.Add(selectedPath);
            }
        }

        return siblingPaths;
    }

    private void MapsetCollapseHeaderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!HasMapsetDifficultyOptions)
        {
            return;
        }

        IsMapsetDiffExpanded = !IsMapsetDiffExpanded;
        e.Handled = true;
    }

    private void MapsetSelectAllButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DestinationMaps is null || MapsetDifficultyCards.Count == 0)
        {
            return;
        }

        _isBulkUpdatingDestinationMaps = true;
        try
        {
            var selectedPaths = new HashSet<string>(
                DestinationMaps.Where(map => map.HasPath).Select(map => map.Path),
                System.StringComparer.OrdinalIgnoreCase);

            var allMapsetSelected = MapsetDifficultyCards.All(card => selectedPaths.Contains(card.Path));
            if (allMapsetSelected)
            {
                var mapsetPaths = new HashSet<string>(
                    MapsetDifficultyCards.Select(card => card.Path),
                    System.StringComparer.OrdinalIgnoreCase);

                for (var i = DestinationMaps.Count - 1; i >= 0; i--)
                {
                    var map = DestinationMaps[i];
                    if (!map.HasPath || !mapsetPaths.Contains(map.Path))
                    {
                        continue;
                    }

                    DestinationMaps.RemoveAt(i);
                }
            }
            else
            {
                foreach (var card in MapsetDifficultyCards)
                {
                    if (selectedPaths.Contains(card.Path))
                    {
                        continue;
                    }

                    DestinationMaps.Add(new SelectedMap
                    {
                        Path = card.Path
                    });
                }

                IsMapsetDiffExpanded = true;
            }
        }
        finally
        {
            _isBulkUpdatingDestinationMaps = false;
        }

        SyncDestinationMapItemObservers(DestinationMaps);
        UpdateDestinationSelectionState();
        RefreshMapsetDifficultySelectionState();
        e.Handled = true;
    }

    private void DestinationMapsetExpandButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: DestinationMapsetCard mapsetCard })
        {
            return;
        }

        mapsetCard.IsExpanded = !mapsetCard.IsExpanded;
        _destinationMapsetExpansionStates[mapsetCard.MapsetDirectoryPath] = mapsetCard.IsExpanded;
        e.Handled = true;
    }

    private void DestinationMapsetRemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DestinationMaps is null ||
            sender is not Control { DataContext: DestinationMapsetCard mapsetCard })
        {
            return;
        }

        for (var i = DestinationMaps.Count - 1; i >= 0; i--)
        {
            var map = DestinationMaps[i];
            if (!map.HasPath)
            {
                continue;
            }

            var mapsetDirectoryPath = BeatmapPathUtils.TryGetMapsetDirectoryPath(map.Path);
            if (string.Equals(mapsetDirectoryPath, mapsetCard.MapsetDirectoryPath, System.StringComparison.OrdinalIgnoreCase))
            {
                DestinationMaps.RemoveAt(i);
            }
        }

        _destinationMapsetExpansionStates.Remove(mapsetCard.MapsetDirectoryPath);
        e.Handled = true;
    }

}
