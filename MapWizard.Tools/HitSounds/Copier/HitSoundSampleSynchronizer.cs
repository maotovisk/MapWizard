using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BeatmapParser;
using MapWizard.Tools.HitSounds.Extensions;

namespace MapWizard.Tools.HitSounds.Copier;

internal static partial class HitSoundSampleSynchronizer
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    [GeneratedRegex(
        "^(?<set>normal|soft|drum)-(?<kind>[a-z]+?)(?<index>\\d+)?(?<ext>\\.wav|\\.ogg|\\.mp3)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IndexedSampleFileRegex();

    public static void SynchronizeIfNeeded(
        Beatmap source,
        string sourceBeatmapPath,
        Beatmap target,
        string targetBeatmapPath,
        HitSoundCopierOptions options)
    {
        if (!options.CopyUsedSamplesIfDifferentMapset)
        {
            return;
        }

        var sourceDirectory = ResolveMapsetDirectory(sourceBeatmapPath);
        var targetDirectory = ResolveMapsetDirectory(targetBeatmapPath);
        if (sourceDirectory is null || targetDirectory is null)
        {
            return;
        }

        if (PathComparer.Equals(sourceDirectory, targetDirectory))
        {
            return;
        }

        var hashCache = new Dictionary<string, string>(PathComparer);
        var targetFilesByName = Directory.EnumerateFiles(targetDirectory, "*", SearchOption.TopDirectoryOnly)
            .Select(path => (Name: Path.GetFileName(path), Path: path))
            .Where(file => !string.IsNullOrWhiteSpace(file.Name))
            .ToDictionary(file => file.Name!, file => file.Path, StringComparer.OrdinalIgnoreCase);

        var customSampleRenameMap = CopyCustomSamples(source, sourceDirectory, targetDirectory, targetFilesByName, hashCache);
        if (customSampleRenameMap.Count > 0)
        {
            target.RemapReferencedCustomSampleFileNames(customSampleRenameMap);
        }

        var sampleIndexRemap = CopyIndexedSamples(source, sourceDirectory, targetDirectory, targetFilesByName, hashCache);
        if (sampleIndexRemap.Count > 0)
        {
            target.RemapSampleIndices(sampleIndexRemap);
        }
    }

    private static string? ResolveMapsetDirectory(string beatmapPath)
    {
        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(beatmapPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        return directory;
    }

    private static Dictionary<string, string> CopyCustomSamples(
        Beatmap source,
        string sourceDirectory,
        string targetDirectory,
        Dictionary<string, string> targetFilesByName,
        Dictionary<string, string> hashCache)
    {
        var renameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var referencedSampleFiles = source.GetReferencedCustomSampleFileNames();

        foreach (var referencedName in referencedSampleFiles)
        {
            var sourceFilePath = Path.Combine(sourceDirectory, referencedName);
            if (!File.Exists(sourceFilePath))
            {
                continue;
            }

            var resolvedName = EnsureCopiedWithConflictResolution(
                preferredFileName: referencedName,
                sourceFilePath: sourceFilePath,
                targetDirectory: targetDirectory,
                targetFilesByName: targetFilesByName,
                hashCache: hashCache);

            if (!StringComparer.OrdinalIgnoreCase.Equals(resolvedName, referencedName))
            {
                renameMap[referencedName] = resolvedName;
            }
        }

        return renameMap;
    }

    private static Dictionary<int, int> CopyIndexedSamples(
        Beatmap source,
        string sourceDirectory,
        string targetDirectory,
        Dictionary<string, string> targetFilesByName,
        Dictionary<string, string> hashCache)
    {
        var remap = new Dictionary<int, int>();
        var usedIndices = source.GetUsedSampleIndices();
        if (usedIndices.Count == 0)
        {
            return remap;
        }

        var sourceAssets = ParseIndexedSampleAssets(sourceDirectory)
            .Where(x => usedIndices.Contains(x.Index))
            .ToList();
        if (sourceAssets.Count == 0)
        {
            return remap;
        }

        var targetAssets = ParseIndexedSampleAssets(targetDirectory);
        var sourcePackages = BuildPackages(sourceAssets, hashCache);
        var targetPackages = BuildPackages(targetAssets, hashCache);
        var usedTargetIndices = new HashSet<int>(targetPackages.Keys);

        foreach (var (sourceIndex, sourcePackage) in sourcePackages.OrderBy(x => x.Key))
        {
            if (sourcePackage.Count == 0)
            {
                continue;
            }

            var matchingIndex = FindExactMatchingIndex(sourcePackage, targetPackages);
            if (matchingIndex > 0)
            {
                if (matchingIndex != sourceIndex)
                {
                    remap[sourceIndex] = matchingIndex;
                }

                continue;
            }

            var mappedIndex = sourceIndex;
            if (!IsIndexAvailableForPackage(mappedIndex, sourcePackage, targetFilesByName, hashCache))
            {
                mappedIndex = FindNextAvailableIndex(sourcePackage, usedTargetIndices, targetFilesByName, hashCache);
            }

            CopyPackageFiles(sourcePackage, mappedIndex, targetDirectory, targetFilesByName, hashCache);
            targetPackages[mappedIndex] = BuildRemappedPackage(sourcePackage, mappedIndex, targetDirectory, hashCache);
            usedTargetIndices.Add(mappedIndex);

            if (mappedIndex != sourceIndex)
            {
                remap[sourceIndex] = mappedIndex;
            }
        }

        return remap;
    }

    private static int NormalizeSampleIndex(int sampleIndex)
    {
        return sampleIndex <= 1 ? 1 : sampleIndex;
    }

    private static List<IndexedSampleAsset> ParseIndexedSampleAssets(string directoryPath)
    {
        var assets = new List<IndexedSampleAsset>();
        var regex = IndexedSampleFileRegex();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(filePath);
            var match = regex.Match(fileName);
            if (!match.Success)
            {
                continue;
            }

            var setName = match.Groups["set"].Value.ToLowerInvariant();
            var kindName = match.Groups["kind"].Value.ToLowerInvariant();
            var extension = match.Groups["ext"].Value.ToLowerInvariant();
            var indexGroup = match.Groups["index"];
            var index = indexGroup.Success && int.TryParse(indexGroup.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedIndex)
                ? NormalizeSampleIndex(parsedIndex)
                : 1;

            assets.Add(new IndexedSampleAsset(filePath, setName, kindName, extension, index));
        }

        return assets;
    }

    private static Dictionary<int, Dictionary<string, IndexedPackageEntry>> BuildPackages(
        IEnumerable<IndexedSampleAsset> assets,
        Dictionary<string, string> hashCache)
    {
        var packages = new Dictionary<int, Dictionary<string, IndexedPackageEntry>>();

        foreach (var asset in assets)
        {
            if (!packages.TryGetValue(asset.Index, out var package))
            {
                package = new Dictionary<string, IndexedPackageEntry>(StringComparer.OrdinalIgnoreCase);
                packages[asset.Index] = package;
            }

            if (package.ContainsKey(asset.LogicalKey))
            {
                continue;
            }

            package[asset.LogicalKey] = new IndexedPackageEntry(asset, GetFileHash(asset.Path, hashCache));
        }

        return packages;
    }

    private static int FindExactMatchingIndex(
        Dictionary<string, IndexedPackageEntry> sourcePackage,
        Dictionary<int, Dictionary<string, IndexedPackageEntry>> targetPackages)
    {
        foreach (var (index, targetPackage) in targetPackages)
        {
            if (PackageEquals(sourcePackage, targetPackage))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool PackageEquals(
        Dictionary<string, IndexedPackageEntry> sourcePackage,
        Dictionary<string, IndexedPackageEntry> targetPackage)
    {
        if (sourcePackage.Count != targetPackage.Count)
        {
            return false;
        }

        foreach (var (logicalKey, sourceEntry) in sourcePackage)
        {
            if (!targetPackage.TryGetValue(logicalKey, out var targetEntry))
            {
                return false;
            }

            if (!string.Equals(sourceEntry.Hash, targetEntry.Hash, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIndexAvailableForPackage(
        int index,
        Dictionary<string, IndexedPackageEntry> sourcePackage,
        Dictionary<string, string> targetFilesByName,
        Dictionary<string, string> hashCache)
    {
        foreach (var (_, entry) in sourcePackage)
        {
            var destinationName = entry.Asset.WithIndex(index);
            if (!targetFilesByName.TryGetValue(destinationName, out var targetPath))
            {
                continue;
            }

            if (!FileHashesEqual(entry.Asset.Path, targetPath, hashCache))
            {
                return false;
            }
        }

        return true;
    }

    private static int FindNextAvailableIndex(
        Dictionary<string, IndexedPackageEntry> sourcePackage,
        HashSet<int> usedTargetIndices,
        Dictionary<string, string> targetFilesByName,
        Dictionary<string, string> hashCache)
    {
        var candidate = Math.Max(2, usedTargetIndices.Count == 0 ? 2 : usedTargetIndices.Max() + 1);
        while (true)
        {
            if (!usedTargetIndices.Contains(candidate) &&
                IsIndexAvailableForPackage(candidate, sourcePackage, targetFilesByName, hashCache))
            {
                return candidate;
            }

            candidate++;
        }
    }

    private static void CopyPackageFiles(
        Dictionary<string, IndexedPackageEntry> sourcePackage,
        int index,
        string targetDirectory,
        Dictionary<string, string> targetFilesByName,
        Dictionary<string, string> hashCache)
    {
        foreach (var (_, entry) in sourcePackage)
        {
            var destinationName = entry.Asset.WithIndex(index);
            var destinationPath = Path.Combine(targetDirectory, destinationName);

            if (targetFilesByName.TryGetValue(destinationName, out var existingPath))
            {
                if (!FileHashesEqual(entry.Asset.Path, existingPath, hashCache))
                {
                    throw new IOException($"Conflicting sample file '{destinationName}' already exists in target mapset.");
                }

                continue;
            }

            File.Copy(entry.Asset.Path, destinationPath, overwrite: false);
            targetFilesByName[destinationName] = destinationPath;
            hashCache[destinationPath] = GetFileHash(entry.Asset.Path, hashCache);
        }
    }

    private static Dictionary<string, IndexedPackageEntry> BuildRemappedPackage(
        Dictionary<string, IndexedPackageEntry> sourcePackage,
        int index,
        string targetDirectory,
        Dictionary<string, string> hashCache)
    {
        var package = new Dictionary<string, IndexedPackageEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var (logicalKey, entry) in sourcePackage)
        {
            var destinationName = entry.Asset.WithIndex(index);
            var destinationPath = Path.Combine(targetDirectory, destinationName);
            var remappedAsset = new IndexedSampleAsset(
                destinationPath,
                entry.Asset.SampleSet,
                entry.Asset.SampleType,
                entry.Asset.Extension,
                index);

            package[logicalKey] = new IndexedPackageEntry(remappedAsset, GetFileHash(destinationPath, hashCache));
        }

        return package;
    }

    private static string EnsureCopiedWithConflictResolution(
        string preferredFileName,
        string sourceFilePath,
        string targetDirectory,
        Dictionary<string, string> targetFilesByName,
        Dictionary<string, string> hashCache)
    {
        var existingName = TryGetReusableOrFreeName(preferredFileName, sourceFilePath, targetFilesByName, hashCache);
        if (existingName != null)
        {
            if (!targetFilesByName.ContainsKey(existingName))
            {
                var destinationPath = Path.Combine(targetDirectory, existingName);
                File.Copy(sourceFilePath, destinationPath, overwrite: false);
                targetFilesByName[existingName] = destinationPath;
                hashCache[destinationPath] = GetFileHash(sourceFilePath, hashCache);
            }

            return existingName;
        }

        var baseName = Path.GetFileNameWithoutExtension(preferredFileName);
        var extension = Path.GetExtension(preferredFileName);

        var counter = 2;
        while (true)
        {
            var candidateName = $"{baseName}-{counter}{extension}";
            var candidate = TryGetReusableOrFreeName(candidateName, sourceFilePath, targetFilesByName, hashCache);
            if (candidate != null)
            {
                if (!targetFilesByName.ContainsKey(candidate))
                {
                    var destinationPath = Path.Combine(targetDirectory, candidate);
                    File.Copy(sourceFilePath, destinationPath, overwrite: false);
                    targetFilesByName[candidate] = destinationPath;
                    hashCache[destinationPath] = GetFileHash(sourceFilePath, hashCache);
                }

                return candidate;
            }

            counter++;
        }
    }

    private static string? TryGetReusableOrFreeName(
        string candidateName,
        string sourceFilePath,
        Dictionary<string, string> targetFilesByName,
        Dictionary<string, string> hashCache)
    {
        if (!targetFilesByName.TryGetValue(candidateName, out var existingPath))
        {
            return candidateName;
        }

        return FileHashesEqual(sourceFilePath, existingPath, hashCache) ? candidateName : null;
    }

    private static bool FileHashesEqual(string firstPath, string secondPath, Dictionary<string, string> hashCache)
    {
        return string.Equals(
            GetFileHash(firstPath, hashCache),
            GetFileHash(secondPath, hashCache),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFileHash(string filePath, Dictionary<string, string> hashCache)
    {
        if (hashCache.TryGetValue(filePath, out var cachedHash))
        {
            return cachedHash;
        }

        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        var hashText = Convert.ToHexString(hash);
        hashCache[filePath] = hashText;
        return hashText;
    }


    private sealed record IndexedSampleAsset(
        string Path,
        string SampleSet,
        string SampleType,
        string Extension,
        int Index)
    {
        public string LogicalKey => $"{SampleSet}-{SampleType}{Extension}";

        public string WithIndex(int index)
        {
            var indexSuffix = index > 1 ? index.ToString(CultureInfo.InvariantCulture) : string.Empty;
            return $"{SampleSet}-{SampleType}{indexSuffix}{Extension}";
        }
    }

    private sealed record IndexedPackageEntry(IndexedSampleAsset Asset, string Hash);
}
