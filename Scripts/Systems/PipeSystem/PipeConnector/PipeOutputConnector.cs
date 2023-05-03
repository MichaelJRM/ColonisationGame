﻿using System;
using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

public partial class PipeOutputConnector : PipeConnector, IResourceOutputConnector
{
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