using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Linq;
using System.Windows.Input;

namespace MapWizard.Desktop.Views.Controls;

public partial class BeatmapSelectionPanel : UserControl
{
    private string _originPathBeforeEdit = string.Empty;

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

    public BeatmapSelectionPanel()
    {
        InitializeComponent();
    }

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

    private void OriginPathSummaryTextBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginOriginPathEdit();
        e.Handled = true;
    }

    private void DestinationPathSummaryTextBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginDestinationPathEdit();
        e.Handled = true;
    }

    private void OriginPathSummaryTextBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        BeginOriginPathEdit();
    }

    private void DestinationPathSummaryTextBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        BeginDestinationPathEdit();
    }

    private void OriginPathEditTextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        EndOriginPathEdit();
    }

    private void DestinationPathEditTextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        IsEditingDestinationPath = false;
        ShowDestinationSummary = true;
    }

    private void OriginPathEditTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Escape))
        {
            return;
        }

        EndOriginPathEdit();
        Focus();
        e.Handled = true;
    }

    private void DestinationPathEditTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Escape))
        {
            return;
        }

        IsEditingDestinationPath = false;
        ShowDestinationSummary = true;
        Focus();
        e.Handled = true;
    }

    private void AdditionalPathSummaryTextBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBox summaryTextBox)
        {
            BeginAdditionalPathEdit(summaryTextBox);
        }

        e.Handled = true;
    }

    private void AdditionalPathSummaryTextBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox summaryTextBox)
        {
            BeginAdditionalPathEdit(summaryTextBox);
        }
    }

    private void AdditionalPathEditTextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox editTextBox)
        {
            EndAdditionalPathEdit(editTextBox);
        }
    }

    private void AdditionalPathEditTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox editTextBox || e.Key is not (Key.Enter or Key.Escape))
        {
            return;
        }

        EndAdditionalPathEdit(editTextBox);
        Focus();
        e.Handled = true;
    }

    private void BeginOriginPathEdit()
    {
        if (IsEditingOriginPath)
        {
            return;
        }

        _originPathBeforeEdit = OriginPathEditTextBox.Text ?? string.Empty;
        ShowOriginSummary = false;
        IsEditingOriginPath = true;

        Dispatcher.UIThread.Post(() =>
        {
            OriginPathEditTextBox.Focus();
            OriginPathEditTextBox.CaretIndex = OriginPathEditTextBox.Text?.Length ?? 0;
        }, DispatcherPriority.Input);
    }

    private void BeginDestinationPathEdit()
    {
        if (IsEditingDestinationPath)
        {
            return;
        }

        ShowDestinationSummary = false;
        IsEditingDestinationPath = true;

        Dispatcher.UIThread.Post(() =>
        {
            DestinationPathEditTextBox.Focus();
            DestinationPathEditTextBox.CaretIndex = DestinationPathEditTextBox.Text?.Length ?? 0;
        }, DispatcherPriority.Input);
    }

    private static void BeginAdditionalPathEdit(TextBox summaryTextBox)
    {
        if (summaryTextBox.Parent is not Panel panel)
        {
            return;
        }

        var editTextBox = panel.Children
            .OfType<TextBox>()
            .FirstOrDefault(textBox => !ReferenceEquals(textBox, summaryTextBox));

        if (editTextBox is null || editTextBox.IsVisible)
        {
            return;
        }

        summaryTextBox.IsVisible = false;
        editTextBox.IsVisible = true;

        Dispatcher.UIThread.Post(() =>
        {
            editTextBox.Focus();
            editTextBox.CaretIndex = editTextBox.Text?.Length ?? 0;
        }, DispatcherPriority.Input);
    }

    private static void EndAdditionalPathEdit(TextBox editTextBox)
    {
        if (editTextBox.Parent is not Panel panel)
        {
            return;
        }

        var summaryTextBox = panel.Children
            .OfType<TextBox>()
            .FirstOrDefault(textBox => !ReferenceEquals(textBox, editTextBox));

        editTextBox.IsVisible = false;
        if (summaryTextBox is not null)
        {
            summaryTextBox.IsVisible = true;
        }
    }

    private void EndOriginPathEdit()
    {
        if (!IsEditingOriginPath)
        {
            return;
        }

        IsEditingOriginPath = false;
        ShowOriginSummary = true;

        var currentPath = OriginPathEditTextBox.Text ?? string.Empty;
        if (string.Equals(_originPathBeforeEdit, currentPath, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var command = OriginPathChangedCommand;
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }
}
