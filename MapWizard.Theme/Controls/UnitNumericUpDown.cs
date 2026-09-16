using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace MapWizard.Theme.Controls;

/// <summary>
/// Compact numeric input with an integrated unit-of-measure label.
/// </summary>
public sealed class UnitNumericUpDown : TemplatedControl
{
    public static readonly StyledProperty<decimal?> ValueProperty =
        AvaloniaProperty.Register<UnitNumericUpDown, decimal?>(
            nameof(Value),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<decimal> MinimumProperty =
        AvaloniaProperty.Register<UnitNumericUpDown, decimal>(nameof(Minimum), decimal.MinValue);

    public static readonly StyledProperty<decimal> MaximumProperty =
        AvaloniaProperty.Register<UnitNumericUpDown, decimal>(nameof(Maximum), decimal.MaxValue);

    public static readonly StyledProperty<decimal> IncrementProperty =
        AvaloniaProperty.Register<UnitNumericUpDown, decimal>(nameof(Increment), 1m);

    public static readonly StyledProperty<string?> FormatStringProperty =
        AvaloniaProperty.Register<UnitNumericUpDown, string?>(nameof(FormatString));

    public static readonly StyledProperty<object?> UnitProperty =
        AvaloniaProperty.Register<UnitNumericUpDown, object?>(nameof(Unit));

    private TextBox? _input;
    private Button? _decrementButton;
    private Button? _incrementButton;
    private Interactive? _attachedRoot;
    private bool _isUpdatingText;

    public UnitNumericUpDown()
    {
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Bubble);
    }

    public decimal? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public decimal Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public decimal Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public decimal Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    public string? FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    public object? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        UnhookTemplateParts();
        base.OnApplyTemplate(e);

        _input = e.NameScope.Find<TextBox>("PART_Input");
        _decrementButton = e.NameScope.Find<Button>("PART_DecrementButton");
        _incrementButton = e.NameScope.Find<Button>("PART_IncrementButton");

        if (_input is not null)
        {
            _input.LostFocus += InputOnLostFocus;
            _input.KeyDown += InputOnKeyDown;
        }

        if (_decrementButton is not null)
        {
            _decrementButton.Click += DecrementButtonOnClick;
        }

        if (_incrementButton is not null)
        {
            _incrementButton.Click += IncrementButtonOnClick;
        }

        UpdateInputText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == FormatStringProperty)
        {
            UpdateInputText();
        }
    }

    private void DecrementButtonOnClick(object? sender, RoutedEventArgs e) => ChangeValue(-1);

    private void IncrementButtonOnClick(object? sender, RoutedEventArgs e) => ChangeValue(1);

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!IsEnabled || Math.Abs(e.Delta.Y) < double.Epsilon)
        {
            return;
        }

        CommitInput();
        ChangeValue(e.Delta.Y > 0 ? 1 : -1);
        e.Handled = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TopLevel.GetTopLevel(this) is Interactive root)
        {
            _attachedRoot = root;
            root.AddHandler(PointerPressedEvent, OnRootPointerPressed, RoutingStrategies.Tunnel);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_attachedRoot is { } root)
        {
            root.RemoveHandler(PointerPressedEvent, OnRootPointerPressed);
            _attachedRoot = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_input is not { IsFocused: true } ||
            e.Source is not Visual source ||
            IsWithinSelf(source))
        {
            return;
        }

        CommitInput();
        if (TopLevel.GetTopLevel(this)?.FocusManager is { } focusManager)
        {
            focusManager.Focus(null);
        }
    }

    private bool IsWithinSelf(Visual visual)
    {
        Visual? current = visual;
        while (current is not null)
        {
            if (ReferenceEquals(current, this))
            {
                return true;
            }

            current = current.GetVisualParent();
        }

        return false;
    }

    private void InputOnLostFocus(object? sender, RoutedEventArgs e) => CommitInput();

    private void InputOnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                CommitInput();
                e.Handled = true;
                break;
            case Key.Escape:
                CommitInput();
                if (TopLevel.GetTopLevel(this)?.FocusManager is { } focusManager)
                {
                    focusManager.Focus(null);
                }

                e.Handled = true;
                break;
            case Key.Up:
                CommitInput();
                ChangeValue(1);
                e.Handled = true;
                break;
            case Key.Down:
                CommitInput();
                ChangeValue(-1);
                e.Handled = true;
                break;
        }
    }

    private void ChangeValue(int direction)
    {
        var next = Math.Clamp((Value ?? 0m) + (Increment * direction), Minimum, Maximum);
        SetCurrentValue(ValueProperty, next);
        UpdateInputText();
    }

    private void CommitInput()
    {
        if (_input is null || _isUpdatingText)
        {
            return;
        }

        if (decimal.TryParse(_input.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
        {
            SetCurrentValue(ValueProperty, Math.Clamp(parsed, Minimum, Maximum));
        }

        UpdateInputText();
    }

    private void UpdateInputText()
    {
        if (_input is null)
        {
            return;
        }

        _isUpdatingText = true;
        try
        {
            _input.Text = Value is { } value
                ? string.IsNullOrWhiteSpace(FormatString)
                    ? value.ToString(CultureInfo.CurrentCulture)
                    : value.ToString(FormatString, CultureInfo.CurrentCulture)
                : string.Empty;
        }
        finally
        {
            _isUpdatingText = false;
        }
    }

    private void UnhookTemplateParts()
    {
        if (_input is not null)
        {
            _input.LostFocus -= InputOnLostFocus;
            _input.KeyDown -= InputOnKeyDown;
        }

        if (_decrementButton is not null)
        {
            _decrementButton.Click -= DecrementButtonOnClick;
        }

        if (_incrementButton is not null)
        {
            _incrementButton.Click -= IncrementButtonOnClick;
        }
    }
}
