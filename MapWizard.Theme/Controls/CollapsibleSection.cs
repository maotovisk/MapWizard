using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace MapWizard.Theme.Controls;

public sealed class CollapsibleSection : HeaderedContentControl
{
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<CollapsibleSection, bool>(nameof(IsExpanded));

    protected override Type StyleKeyOverride => typeof(CollapsibleSection);

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsExpandedProperty)
        {
            PseudoClasses.Set(":expanded", IsExpanded);
        }
    }
}
