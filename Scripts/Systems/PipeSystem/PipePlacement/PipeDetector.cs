using System.Linq;
using BaseBuilding.scripts.singletons;
using BaseBuilding.scripts.systems.PipeSystem;
using BaseBuilding.Scripts.Util;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipePlacement;

public partial class PipeDetector : Area3D
{
    public bool IsAreaValid { get; private set; } = true;

    public override void _Ready()
    {
        SetCollisionLayerValue(1, false);
        SetCollisionMaskValue(1, false);
        SetCollisionMaskValue(CollisionIndexes.Building, true);
        SetCollisionMaskValue(CollisionIndexes.Pipe, true);
        SetCollisionMaskValue(CollisionIndexes.Wire, true);
        var collisionShape = new CollisionShape3D();
        var shape = new SphereShape3D();
        shape.Radius = 0.2f;
        collisionShape.Shape = shape;
        AddChild(collisionShape);
        GlobalPosition = Global.Instance.GetMousePositionInWorld();
    }

    public override void _Process(double delta)
    {
        GlobalPosition = Global.Instance.GetMousePositionInWorld();
    }

    public PipeJoint? GetClosestDetectedPipeJoint()
    {
        var detectedPipeJoints = GetOverlappingAreas().OfType<PipeJoint>().ToArray();
        return NodeUtil.FindClosestNode(GlobalPosition, detectedPipeJoints);
    }

    public Pipe? GetClosestDetectedPipe()
    {
        var detectedPipes = GetOverlappingAreas().OfType<Pipe>().ToArray();
        return NodeUtil.FindClosestNode(GlobalPosition, detectedPipes);
    }
}