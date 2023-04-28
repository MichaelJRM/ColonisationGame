using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.singletons;
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
        var overlappingAreas = GetOverlappingAreas();
        IsAreaValid = overlappingAreas.Count == 0 ||
                      overlappingAreas.All(overlappingArea => overlappingArea is PipeJoint or Pipe);
    }

    public PipeJoint? GetClosestDetectedPipeJoint()
    {
        var overlappingAreas = GetOverlappingAreas();
        var detectedPipeJoints = new List<PipeJoint>(overlappingAreas.Count);
        foreach (var overlappingArea in overlappingAreas)
            if (overlappingArea is PipeJoint pipeJoint)
                detectedPipeJoints.Add(pipeJoint);
        return _FindClosestNode(detectedPipeJoints);
    }

    public Pipe? GetClosestDetectedPipe()
    {
        var overlappingAreas = GetOverlappingAreas();
        var detectedPipes = new List<Pipe>(overlappingAreas.Count);
        foreach (var overlappingArea in overlappingAreas)
            if (overlappingArea is Pipe pipe and not TemporaryPipe)
                detectedPipes.Add(pipe);
        return _FindClosestNode(detectedPipes);
    }

    private T? _FindClosestNode<T>(List<T> nodes) where T : Node3D
    {
        T? closestNode = null;
        var closestDistance = float.MaxValue;
        foreach (var node in nodes)
        {
            var distance = GlobalPosition.DistanceSquaredTo(node.GlobalPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = node;
            }
        }

        return closestNode;
    }
}