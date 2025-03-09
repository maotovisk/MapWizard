using System;
using System.Threading.Tasks;
using MapWizard.Desktop.Models;

namespace MapWizard.Desktop.Services;

public interface IMetadataManagerService
{
    public void ApplyMetadata(BeatmapMetadata metadata, string[] targetPaths);
}