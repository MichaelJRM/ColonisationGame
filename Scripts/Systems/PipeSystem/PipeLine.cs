using System.Collections.Generic;
using System.Linq;
using BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;
using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public class PipeLine
{
    private readonly uint _id;
    private readonly List<PipeJoint> _pipeJoints = new();
    public readonly List<PipeConnector> PipeConnectors = new();

    public PipeLine(uint id)
    {
        _id = id;
    }

    public void AddPipeJoint(PipeJoint pipeJoint)
    {
        if (pipeJoint is PipeConnector pipeConnector)
        {
            pipeConnector.ResourceRequestedCallback = _onResourceRequested;
            PipeConnectors.Add(pipeConnector);
        }
        else
        {
            _pipeJoints.Add(pipeJoint);
        }
    }

    private float _onResourceRequested(WorldResource worldResource, float amount, PipeInputConnector askerPipeConnector)
    {
        var owner = askerPipeConnector.GetOwner<Node3D>();
        var pipeConnectorsWithResource = PipeConnectors.Where(e =>
            e is PipeOutputConnector
            && e.AcceptsResource(worldResource)
            && e != askerPipeConnector
            && e.Owner != owner
        ).ToArray();
        if (pipeConnectorsWithResource.Length == 0) return 0f;

        var amountGathered = 0f;
        for (var i = 0; i < 5; i++)
        {
            var amountPerPipeConnector = (amount - amountGathered) / pipeConnectorsWithResource.Length;
            foreach (var pipeConnector in pipeConnectorsWithResource)
            {
                var gathered =
                    ((PipeOutputConnector)pipeConnector).AskForResource(worldResource, amountPerPipeConnector);
                amountGathered += gathered;
            }

            if (amountGathered >= amount) break;
        }

        return amountGathered;
    }

    public void MergeWith(PipeLine pipeLineB)
    {
        foreach (var pipeJoint in pipeLineB._pipeJoints)
        {
            AddPipeJoint(pipeJoint);
            pipeJoint.SetPipeLineId(_id);
        }

        foreach (var pipeConnector in pipeLineB.PipeConnectors)
        {
            AddPipeJoint(pipeConnector);
            pipeConnector.SetPipeLineId(_id);
        }
    }
}