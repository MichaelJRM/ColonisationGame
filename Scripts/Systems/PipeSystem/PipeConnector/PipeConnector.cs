using BaseBuilding.scripts.systems.PipeSystem;
using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

public partial class PipeConnector : PipeJoint, IResourceConnector
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
        return _acceptedResource.Id == worldResource.Id;
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