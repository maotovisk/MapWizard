using Lucide.Avalonia;

namespace MapWizard.Desktop.Models;

/// <summary>
/// One entry in the Quick Start list. <see cref="Key"/> is resolved by the view
/// to the matching navigation target.
/// </summary>
public sealed record QuickStartTool(string Key, string Name, string Description, LucideIconKind Icon);
