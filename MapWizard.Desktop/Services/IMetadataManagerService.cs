using MapWizard.Desktop.Models;
using MapWizard.Tools.MetadataManager;

namespace MapWizard.Desktop.Services;

public interface IMetadataManagerService
{
    public void ApplyMetadata(AvaloniaBeatmapMetadata metadata, string[] targetPaths, MetadataManagerOptions options);
}