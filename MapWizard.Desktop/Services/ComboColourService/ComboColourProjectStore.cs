using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using MapWizard.Tools.ComboColourStudio;

namespace MapWizard.Desktop.Services.ComboColourService;

public class ComboColourProjectStore : IComboColourProjectStore
{
    private const string AppDirectoryName = "MapWizard";
    private const string ProjectDirectoryName = "ComboColourStudio";
    private const string StorageFileName = "projects.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly object _syncRoot = new();
    private readonly string _storageFilePath;

    public ComboColourProjectStore()
    {
        var dataDirectory = ResolveDataDirectoryPath();
        var projectDirectory = Path.Combine(dataDirectory, ProjectDirectoryName);
        Directory.CreateDirectory(projectDirectory);
        _storageFilePath = Path.Combine(projectDirectory, StorageFileName);
    }

    public ComboColourProject? TryLoadProject(string beatmapPath, int beatmapId)
    {
        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            return null;
        }

        lock (_syncRoot)
        {
            var records = ReadRecords();
            var normalizedPath = NormalizePath(beatmapPath);

            var candidate = records
                .Where(record => PathEquals(record.BeatmapPath, normalizedPath) ||
                                 (beatmapId > 0 && record.BeatmapId > 0 && record.BeatmapId == beatmapId))
                .OrderByDescending(record => record.SavedAtUtc)
                .FirstOrDefault();

            return candidate?.Project is null ? null : FromPersistedProject(candidate.Project);
        }
    }

    public void SaveProject(string beatmapPath, int beatmapId, ComboColourProject project)
    {
        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            return;
        }

        lock (_syncRoot)
        {
            var records = ReadRecords();
            var normalizedPath = NormalizePath(beatmapPath);

            var record = records.FirstOrDefault(existing => PathEquals(existing.BeatmapPath, normalizedPath));
            if (record is null && beatmapId > 0)
            {
                record = records.FirstOrDefault(existing => existing.BeatmapId > 0 && existing.BeatmapId == beatmapId);
            }

            if (record is null)
            {
                record = new PersistedProjectRecord();
                records.Add(record);
            }

            record.BeatmapPath = normalizedPath;
            record.BeatmapId = beatmapId;
            record.SavedAtUtc = DateTimeOffset.UtcNow;
            record.Project = ToPersistedProject(project);

            WriteRecords(records);
        }
    }

    private List<PersistedProjectRecord> ReadRecords()
    {
        if (!File.Exists(_storageFilePath))
        {
            return [];
        }

        var json = File.ReadAllText(_storageFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<PersistedProjectRecord>>(json, JsonOptions) ?? [];
    }

    private void WriteRecords(List<PersistedProjectRecord> records)
    {
        var tempPath = _storageFilePath + ".tmp";
        var json = JsonSerializer.Serialize(records, JsonOptions);

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _storageFilePath, overwrite: true);
    }

    private static PersistedProject ToPersistedProject(ComboColourProject project)
    {
        return new PersistedProject
        {
            MaxBurstLength = Math.Max(1, project.MaxBurstLength),
            ComboColours = project.ComboColours
                .Select(combo => new PersistedComboColour
                {
                    R = combo.Colour.R,
                    G = combo.Colour.G,
                    B = combo.Colour.B
                })
                .ToList(),
            ColourPoints = project.ColourPoints
                .Select(point => new PersistedColourPoint
                {
                    Time = point.Time,
                    Mode = point.Mode,
                    ColourSequence = point.ColourSequence.ToList()
                })
                .ToList()
        };
    }

    private static ComboColourProject FromPersistedProject(PersistedProject persistedProject)
    {
        var comboColours = persistedProject.ComboColours
            .Take(8)
            .Select((colour, index) =>
                new BeatmapParser.Colours.ComboColour((uint)(index + 1), Color.FromArgb(255, colour.R, colour.G, colour.B)))
            .ToList();

        if (comboColours.Count == 0)
        {
            throw new InvalidOperationException("Saved combo colour project is invalid: no combo colours found.");
        }

        var maxColourIndex = comboColours.Count - 1;
        var colourPoints = persistedProject.ColourPoints
            .Select(point => new ComboColourPoint
            {
                Time = point.Time,
                Mode = point.Mode,
                ColourSequence = point.ColourSequence
                    .Where(index => index >= 0 && index <= maxColourIndex)
                    .ToList()
            })
            .ToList();

        return new ComboColourProject
        {
            ComboColours = comboColours,
            ColourPoints = colourPoints,
            MaxBurstLength = Math.Max(1, persistedProject.MaxBurstLength)
        };
    }

    private static string ResolveDataDirectoryPath()
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

        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var basePath = string.IsNullOrWhiteSpace(xdgDataHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share")
            : xdgDataHome;

        return Path.Combine(basePath, AppDirectoryName);
    }

    private static bool PathEquals(string left, string right)
    {
        return string.Equals(left, right, OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal);
    }

    private static string NormalizePath(string path)
    {
        var normalized = path;
        try
        {
            normalized = Path.GetFullPath(path);
        }
        catch
        {
            // Keep original value if path normalization fails.
        }

        normalized = normalized.Replace('\\', '/');
        return OperatingSystem.IsWindows() ? normalized.ToLowerInvariant() : normalized;
    }

    private sealed class PersistedProjectRecord
    {
        public string BeatmapPath { get; set; } = string.Empty;
        public int BeatmapId { get; set; }
        public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public PersistedProject Project { get; set; } = new();
    }

    private sealed class PersistedProject
    {
        public int MaxBurstLength { get; set; } = 1;
        public List<PersistedComboColour> ComboColours { get; set; } = [];
        public List<PersistedColourPoint> ColourPoints { get; set; } = [];
    }

    private sealed class PersistedComboColour
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    private sealed class PersistedColourPoint
    {
        public double Time { get; set; }
        public ColourPointMode Mode { get; set; } = ColourPointMode.Normal;
        public List<int> ColourSequence { get; set; } = [];
    }
}
