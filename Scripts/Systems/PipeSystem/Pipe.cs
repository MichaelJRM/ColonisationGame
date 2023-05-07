using System;
using BaseBuilding.scripts.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class Pipe : Area3D
{
    [Export] private Vector2[] _meshShape = Array.Empty<Vector2>();
    [Export] public BaseMaterial3D Material { get; private set; } = null!;
    [Export] public float Length { get; private set; } = 3.0f;
    public uint? RenderId { get; private set; }
    public ulong FrontJointId { get; private set; }
    public ulong BackJointId { get; private set; }
    public CollisionShape3D CollisionShape = null!;
    public PipeJoint FrontJoint => PipeSystem.Instance.GetPipeJoint(FrontJointId);
    public PipeJoint BackJoint => PipeSystem.Instance.GetPipeJoint(BackJointId);


    public void SetRenderId(uint renderId)
    {
        RenderId = renderId;
    }

    public ArrayMesh CreateMesh(float? length = null)
    {
        var pipeMeshCurve = new Curve3D();
        pipeMeshCurve.BakeInterval = 5.0f;
        var meshLength = length ?? ((CylinderShape3D)CollisionShape.Shape).Height;
        pipeMeshCurve.AddPoint(Vector3.Back * (meshLength * 0.5f));
        pipeMeshCurve.AddPoint(Vector3.Forward * (meshLength * 0.5f));
        var mesh = MeshExtruder.Create(pipeMeshCurve, new[] { _meshShape });
        mesh.SurfaceSetMaterial(0, Material);
        return mesh;
    }

    public bool CanCreateJointAtPosition(Vector3 globalPosition)
    {
        var backJoint = PipeSystem.Instance.GetPipeJoint(BackJointId);
        var frontJoint = PipeSystem.Instance.GetPipeJoint(FrontJointId);
        var backDistance = backJoint.GlobalPosition.DistanceTo(globalPosition);
        var frontDistance = frontJoint.GlobalPosition.DistanceTo(globalPosition);
        var minDistanceBetweenJoints = backJoint.MinDistanceBetweenJoints;
        return backDistance > minDistanceBetweenJoints && frontDistance > minDistanceBetweenJoints;
    }

    public void SetFrontJoint(PipeJoint pipeJoint)
    {
        FrontJointId = pipeJoint.Id;
    }

    public void SetBackJoint(PipeJoint pipeJoint)
    {
        BackJointId = pipeJoint.Id;
    }
}