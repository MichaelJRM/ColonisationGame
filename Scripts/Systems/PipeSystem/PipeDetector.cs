using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.singletons;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipeDetector : Area3D
{
    private Global _global = null!;
    public bool IsAreaValid { get; private set; } = true;

    public override void _Ready()
    {
        _global = GetNode<Global>("/root/Global");
        GlobalPosition = _global.MousePositionInWorld;
    }

    public override void _Process(double delta)
    {
        GlobalPosition = _global.MousePositionInWorld;
    }

    public override void _PhysicsProcess(double delta)
    {
        IsAreaValid = (
            !HasOverlappingAreas()
            || GetOverlappingAreas().All(e => e.GetType() == typeof(PipeJoint) || e.GetType() == typeof(Pipe))
        );
    }

    public PipeJoint? GetClosestDetectedPipeJoint()
    {
        if (!HasOverlappingAreas()) return null;
        var detectedPipeJoints = GetOverlappingAreas()
            .Where(e => e.GetType() == typeof(PipeJoint))
            .Select(e => (PipeJoint)e)
            .ToArray();

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