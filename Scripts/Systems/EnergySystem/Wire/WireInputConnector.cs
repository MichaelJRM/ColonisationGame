using System;
using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

public partial class WireInputConnector : WireConnector, IResourceInputConnector
{
    private OnResourceRequestedCallback? _onResourceRequestedCallback;

    public void BindOnResourceRequested(OnResourceRequestedCallback onResourceRequestedCallback)
    {
        _onResourceRequestedCallback = onResourceRequestedCallback;
    }

    public float RequestResource(WorldResource resource)
    {
        var isResourceAccepted = AcceptsResource(resource);
        if (!isResourceAccepted)
            throw new Exception(
                $"Connector {Name} requested resource {resource.Name} which it does not accept"
            );
        return _onResourceRequestedCallback!.Invoke(resource, FlowRate, this);
    }
}