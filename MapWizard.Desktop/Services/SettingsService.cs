using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MapWizard.Desktop.Models.Settings;

namespace MapWizard.Desktop.Services;

public class SettingsService : ISettingsService
{
    private const string AppDirectoryName = "MapWizard";
    private const string MainSettingsFileName = "MainSettings.ini";

    public string ConfigDirectoryPath { get; }

    public SettingsService()
    {
        ConfigDirectoryPath = ResolveConfigDirectoryPath();
        Directory.CreateDirectory(ConfigDirectoryPath);
    }

    public MainSettings GetMainSettings()
    {
        return LoadSettings<MainSettings>(MainSettingsFileName);
    }

    public void SaveMainSettings(MainSettings settings)
    {
        SaveSettings(settings, MainSettingsFileName);
    }

    private TSettings LoadSettings<TSettings>(string settingsFileName) where TSettings : new()
    {
        var filePath = GetSettingsFilePath(settingsFileName);
        var document = ReadIniDocument(filePath);
        var settings = new TSettings();

        foreach (var (property, settingAttribute) in GetAnnotatedProperties(typeof(TSettings)))
        {
            var defaultValue = property.GetValue(settings);
            if (!document.TryGetValue(settingAttribute.Section, out var sectionValues) ||
                !sectionValues.TryGetValue(settingAttribute.Key, out var rawValue))
            {
                continue;
            }

            if (TryConvert(rawValue, property.PropertyType, out var convertedValue) && convertedValue is not null)
            {
                property.SetValue(settings, convertedValue);
            }
            else if (defaultValue is not null)
            {
                property.SetValue(settings, defaultValue);
            }
        }

        return settings;
    }

    private void SaveSettings<TSettings>(TSettings settings, string settingsFileName)
    {
        var filePath = GetSettingsFilePath(settingsFileName);
        var document = ReadIniDocument(filePath);

        foreach (var (property, settingAttribute) in GetAnnotatedProperties(typeof(TSettings)))
        {
            var propertyValue = property.GetValue(settings);

            if (!document.TryGetValue(settingAttribute.Section, out var sectionValues))
            {
                sectionValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                document[settingAttribute.Section] = sectionValues;
            }

            sectionValues[settingAttribute.Key] = ConvertToString(propertyValue);
        }

        WriteIniDocument(filePath, document);
    }

    private static IEnumerable<(PropertyInfo Property, SettingAttribute Attribute)> GetAnnotatedProperties(Type settingsType)
    {
        return settingsType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.CanWrite)
            .Select(property => (Property: property, Attribute: property.GetCustomAttribute<SettingAttribute>()))
            .Where(mapped => mapped.Attribute is not null)
            .Select(mapped => (mapped.Property, mapped.Attribute!));
    }

    private static string ResolveConfigDirectoryPath()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppDirectoryName);
        }

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppDirectoryName);
        }

        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var basePath = string.IsNullOrWhiteSpace(xdgConfigHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
            : xdgConfigHome;

        return Path.Combine(basePath, AppDirectoryName);
    }

    private string GetSettingsFilePath(string settingsFileName)
    {
        return Path.Combine(ConfigDirectoryPath, settingsFileName);
    }

    private static Dictionary<string, Dictionary<string, string>> ReadIniDocument(string filePath)
    {
        var document = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(filePath))
        {
            return document;
        }

        var currentSection = string.Empty;
        foreach (var rawLine in File.ReadLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']') && line.Length >= 3)
            {
                currentSection = line[1..^1].Trim();
                if (!document.ContainsKey(currentSection))
                {
                    document[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || string.IsNullOrEmpty(currentSection))
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (key.Length == 0)
            {
                continue;
            }

            document[currentSection][key] = value;
        }

        return document;
    }

    private static void WriteIniDocument(string filePath, Dictionary<string, Dictionary<string, string>> document)
    {
        using var writer = new StreamWriter(filePath, false);
        foreach (var (section, values) in document)
        {
            writer.WriteLine($"[{section}]");
            foreach (var (key, value) in values)
            {
                writer.WriteLine($"{key}={value}");
            }

            writer.WriteLine();
        }
    }

    private static bool TryConvert(string rawValue, Type targetType, out object? value)
    {
        var nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (nonNullableType == typeof(string))
            {
                value = rawValue;
                return true;
            }

            if (nonNullableType.IsEnum)
            {
                value = Enum.Parse(nonNullableType, rawValue, true);
                return true;
            }

            if (nonNullableType == typeof(bool))
            {
                if (!bool.TryParse(rawValue, out var boolValue))
                {
                    value = null;
                    return false;
                }

                value = boolValue;
                return true;
            }

            value = Convert.ChangeType(rawValue, nonNullableType, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    private static string ConvertToString(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            bool boolValue => boolValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
