using System;

namespace MapWizard.Desktop.Models.Settings;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingAttribute(string section, string key) : Attribute
{
    public string Section { get; } = section;
    public string Key { get; } = key;
}
