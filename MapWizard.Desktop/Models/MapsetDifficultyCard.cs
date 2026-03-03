using CommunityToolkit.Mvvm.ComponentModel;

namespace MapWizard.Desktop.Models;

public partial class MapsetDifficultyCard(string path) : ObservableObject
{
    [ObservableProperty] private bool _isSelected;

    public string Path { get; } = path;
    public string DifficultyLabel { get; } = ResolveDifficultyLabel(path);

    private static string ResolveDifficultyLabel(string beatmapPath)
    {
        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            return "Unknown Difficulty";
        }

        var fileName = System.IO.Path.GetFileNameWithoutExtension(beatmapPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Unknown Difficulty";
        }

        var openBracketIndex = fileName.LastIndexOf('[');
        var closeBracketIndex = fileName.LastIndexOf(']');
        if (openBracketIndex >= 0 && closeBracketIndex > openBracketIndex)
        {
            return fileName.Substring(openBracketIndex, closeBracketIndex - openBracketIndex + 1);
        }

        return fileName;
    }
}
