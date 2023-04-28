using System;
using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

public partial class PipeInputConnector : PipeConnector
{
    public override void _Ready()
    {
        base._Ready();
        GetNode<Label3D>("Label3D").Text = "Input";
    }

    public float RequestResource(WorldResource resource)
    {
        var isResourceAccepted = AcceptsResource(resource);
        if (!isResourceAccepted)
            throw new Exception(
                $"Connector {Name} requested resource {resource.Name} which it does not accept"
            );
        return ResourceRequestedCallback.Invoke(resource, FlowRate, this);
    }
}