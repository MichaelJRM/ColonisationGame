using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

public partial class WireConnector : WireJoint, IResourceConnector
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