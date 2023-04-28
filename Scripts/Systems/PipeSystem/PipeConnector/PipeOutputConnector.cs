using System;
using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

public partial class PipeOutputConnector : PipeConnector
{
    public override void _Ready()
    {
        base._Ready();
        GetNode<Label3D>("Label3D").Text = "Output";
    }

    public void Activate(ResourceAskedCallback resourceAskedCallback)
    {
        base.Activate();
        ResourceAskedCallback = resourceAskedCallback;
    }

    public float AskForResource(WorldResource worldResource, float amount)
    {
        var isResourceAccepted = AcceptsResource(worldResource);
        if (!isResourceAccepted)
            throw new Exception(
                $"Connector {Name} was asked for resource {worldResource.Name} which it does not accept"
            );
        return ResourceAskedCallback.Invoke(amount);
    }
}