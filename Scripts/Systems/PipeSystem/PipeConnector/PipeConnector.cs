using System.Collections.Generic;
using System.Linq;
using BaseBuilding.Scripts.Systems.EnergySystem;
using BaseBuilding.scripts.systems.PipeSystem;
using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

public partial class PipeConnector : PipeJoint, IResourceConnector
{
    [Export] private WorldResource[] _acceptedResources = null!;
    [Export] protected float FlowRate;

    public override void _Ready()
    {
        Monitorable = false;
    }

    public void Activate()
    {
        Monitorable = true;
    }

    public bool AcceptsResource(WorldResource worldResource)
    {
        foreach (var resource in _acceptedResources)
        {
            if (resource.Id == worldResource.Id) return true;
        }

        return false;
    }

    public object GetOwner()
    {
        return Owner;
    }
}