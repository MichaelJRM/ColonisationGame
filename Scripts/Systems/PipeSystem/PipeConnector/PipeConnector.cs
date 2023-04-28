using System.Linq;
using BaseBuilding.scripts.systems.PipeSystem;
using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

public delegate float ResourceRequestedCallback(
    WorldResource worldResource,
    float amount,
    PipeInputConnector pipeConnector
);

public delegate float ResourceAskedCallback(float amount);

public partial class PipeConnector : PipeJoint
{
    [Export] private WorldResource[] _acceptedResources = null!;
    [Export] protected float FlowRate;
    protected ResourceAskedCallback ResourceAskedCallback = null!;
    public ResourceRequestedCallback ResourceRequestedCallback = null!;


    public virtual void Activate()
    {
        Monitorable = true;
        if (PipeLineId == null)
        {
            var pipeSystem = GetNode<scripts.systems.PipeSystem.PipeSystem>("/root/PipeSystem");
            pipeSystem.RegisterPipeConnector(this);
        }
    }

    public bool AcceptsResource(WorldResource resource)
    {
        return _acceptedResources.Any(e => e.Id == resource.Id);
    }
}