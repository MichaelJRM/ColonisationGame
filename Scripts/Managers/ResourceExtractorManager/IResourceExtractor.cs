using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Managers.ResourceExtractorManager;

public interface IResourceExtractor
{
    bool HasResource(WorldResource resource);
}