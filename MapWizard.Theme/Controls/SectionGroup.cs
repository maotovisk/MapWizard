using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace MapWizard.Theme.Controls;

public sealed class SectionGroup : HeaderedContentControl
{
    protected override Type StyleKeyOverride => typeof(SectionGroup);
}
