using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace MapWizard.Theme.Controls;

/// <summary>
/// A text field with an inline label and an optional clear affordance.
/// This keeps labels and clear affordances inside a single compact desktop field.
/// </summary>
public sealed class FieldTextBox : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<FieldTextBox, string?>(
            nameof(Text),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> PrefixProperty =
        AvaloniaProperty.Register<FieldTextBox, object?>(nameof(Prefix));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<FieldTextBox, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<bool> AddClearButtonProperty =
        AvaloniaProperty.Register<FieldTextBox, bool>(nameof(AddClearButton), true);

    public static readonly StyledProperty<bool> AcceptsReturnProperty =
        AvaloniaProperty.Register<FieldTextBox, bool>(nameof(AcceptsReturn));

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        AvaloniaProperty.Register<FieldTextBox, TextWrapping>(nameof(TextWrapping), TextWrapping.NoWrap);

    private Button? _clearButton;

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public object? Prefix
    {
        get => GetValue(PrefixProperty);
        set => SetValue(PrefixProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool AddClearButton
    {
        get => GetValue(AddClearButtonProperty);
        set => SetValue(AddClearButtonProperty, value);
    }

    public bool AcceptsReturn
    {
        get => GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_clearButton is not null)
        {
            _clearButton.Click -= ClearButtonOnClick;
        }

        _clearButton = e.NameScope.Find<Button>("PART_ClearButton");
        if (_clearButton is not null)
        {
            _clearButton.Click += ClearButtonOnClick;
        }
    }

    private void ClearButtonOnClick(object? sender, RoutedEventArgs e)
    {
        Text = string.Empty;
    }
}
