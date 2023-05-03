using System;
using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

public partial class WireInputOutputConnector : WireConnector, IResourceOutputConnector, IResourceInputConnector
{
    private bool _hasBeenActivated;
    private OnResourceRequestedCallback? _onResourceRequestedCallback;

    public new void Activate()
    {
        if (!_hasBeenActivated)
        {
            _hasBeenActivated = true;
            base.Activate();
        }
    }

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

    private ResourceAskedCallback _resourceAskedCallback = null!;

    public void BindOnResourceAsked(ResourceAskedCallback resourceLineOutputConnector)
    {
        _resourceAskedCallback = resourceLineOutputConnector;
    }

    public float AskForResource(WorldResource worldResource, float amountPerConnector)
    {
        var isResourceAccepted = AcceptsResource(worldResource);
        if (!isResourceAccepted)
            throw new Exception(
                $"Connector {Name} was asked for resource {worldResource.Name} which it does not accept"
            );
        return _resourceAskedCallback.Invoke(amountPerConnector);
    }
}