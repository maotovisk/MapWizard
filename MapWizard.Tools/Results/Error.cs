namespace MapWizard.Tools.Results;

/// <summary>
/// Describes why an operation failed. <see cref="Code"/> is stable and meant for programs to switch on,
/// <see cref="Message"/> is meant to be shown to the user.
/// </summary>
/// <param name="Code">Stable identifier, e.g. "hitsounds.mania_not_supported".</param>
/// <param name="Message">Human readable description.</param>
public sealed record Error(string Code, string Message)
{
    public override string ToString() => Message;
}
