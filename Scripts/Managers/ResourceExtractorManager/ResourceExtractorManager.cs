using System.Collections.Generic;
using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Managers.ResourceExtractorManager;

public class ResourceExtractorManager
{
    private List<IResourceExtractor> _resourceExtractors = new();


    public void Register(IResourceExtractor resourceExtractor)
    {
        _resourceExtractors.Add(resourceExtractor);
    }

    public void Unregister(IResourceExtractor resourceExtractor)
    {
        _resourceExtractors.Remove(resourceExtractor);
    }

    public List<IResourceExtractor> GetAllWithResource(WorldResource resource)
    {
        List<IResourceExtractor> resourceExtractors = new();
        foreach (var resourceExtractor in _resourceExtractors)
        {
            if (resourceExtractor.HasResource(resource))
            {
                resourceExtractors.Add(resourceExtractor);
            }
        }

        return resourceExtractors;
    }
}