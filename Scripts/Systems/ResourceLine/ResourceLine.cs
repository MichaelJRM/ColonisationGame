﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseBuilding.Scripts.WorldResources;

namespace BaseBuilding.Scripts.Systems;

public class ResourceLine<TJoint, TConnector> where TJoint : IResourceJoint where TConnector : IResourceConnector
{
    private readonly List<TConnector> _connectors = new();
    private readonly uint _id;
    private readonly List<TJoint> _joints = new();

    public ResourceLine(uint id)
    {
        _id = id;
    }

    public void AddJoint(TJoint joint)
    {
        _joints.Add(joint);
    }

    public void AddConnector(TConnector connector)
    {
        _connectors.Add(connector);
        if (connector is IResourceInputConnector)
        {
            ((IResourceInputConnector)connector).BindOnResourceRequested(_onResourceRequested);
            ((IResourceInputConnector)connector).Activate();
        }
    }

    public void MergeWith(ResourceLine<TJoint, TConnector> resourceLineB)
    {
        foreach (var resourceLineJoint in CollectionsMarshal.AsSpan(resourceLineB._joints))
        {
            AddJoint(resourceLineJoint);
            resourceLineJoint.SetLineId(_id);
        }

        foreach (var resourceLineConnector in CollectionsMarshal.AsSpan(resourceLineB._connectors))
        {
            AddConnector(resourceLineConnector);
            resourceLineConnector.SetLineId(_id);
        }
    }

    private float _onResourceRequested(
        WorldResource worldResource,
        float amount,
        IResourceInputConnector inputConnector
    )
    {
        var inputOwner = inputConnector.GetOwner();
        var connectorsWithResource = _connectors.Where(e =>
            e is IResourceOutputConnector
            && e.AcceptsResource(worldResource)
            && e.GetOwner() != inputOwner
        ).ToArray();
        if (connectorsWithResource.Length == 0) return 0f;

        var amountGathered = 0f;
        for (var i = 0; i < 5; i++)
        {
            var amountPerConnector = (amount - amountGathered) / connectorsWithResource.Length;
            foreach (var connector in connectorsWithResource)
            {
                var gathered =
                    ((IResourceOutputConnector)connector).AskForResource(worldResource, amountPerConnector);
                amountGathered += gathered;
            }

            if (amountGathered >= amount) break;
        }

        return amountGathered;
    }
}