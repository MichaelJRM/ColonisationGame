using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.singletons;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipeDetector : Area3D
{
    public bool IsAreaValid { get; private set; } = true;

    public override void _Ready()
    {
        GlobalPosition = Global.Instance.GetMousePositionInWorld();
    }

    public override void _Process(double delta)
    {
        GlobalPosition = Global.Instance.GetMousePositionInWorld();
    }

    public PipeJoint? GetClosestDetectedPipeJoint()
    {
        if (!HasOverlappingAreas()) return null;
        var detectedPipeJoints = GetOverlappingAreas().OfType<PipeJoint>().ToArray();
        return NodeUtil.FindClosestNode(GlobalPosition, detectedPipeJoints);
    }

    public Pipe? GetClosestDetectedPipe()
    {
        if (!HasOverlappingAreas()) return null;
        var detectedPipes = GetOverlappingAreas()
            .Where(e => e.GetType() == typeof(Pipe))
            .Select(e => (Pipe)e)
            .ToArray();

        return NodeUtil.FindClosestNode(GlobalPosition, detectedPipes);
    }
}