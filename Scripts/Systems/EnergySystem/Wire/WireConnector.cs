using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

public partial class WireConnector : WireJoint, IResourceConnector
{
    [Export] private WorldResource _acceptedResource = null!;


    public override void _Ready()
    {
        Monitorable = false;
        if (_acceptedResource == null) GD.PushError("Accepted resource is not set!");
    }

    public void Activate()
    {
        Monitorable = true;
    }

    public bool AcceptsResource(WorldResource worldResource)
    {
        return worldResource.Id == _acceptedResource.Id;
    }

    public object GetOwner()
    {
        return Owner;
    }

    public WorldResource GetAcceptedResource()
    {
        return _acceptedResource;
    }
}