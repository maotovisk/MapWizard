namespace MapWizard.Desktop.Models.Settings;

public sealed class AudioOutputDeviceOption
{
    public AudioOutputDeviceOption(string id, string displayName, bool isDefault, bool isEnabled)
    {
        Id = id;
        DisplayName = displayName;
        IsDefault = isDefault;
        IsEnabled = isEnabled;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public bool IsDefault { get; }

    public bool IsEnabled { get; }

    public override string ToString() => DisplayName;
}
