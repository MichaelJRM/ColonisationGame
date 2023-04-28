using System;
using BaseBuilding.scripts.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class Pipe : Area3D
{
    [Export] private Vector2[] _meshVertices = Array.Empty<Vector2>();
    public float ActualLength = -1.0f;
    public int? RendererId { get; private set; }

    [Export] public CollisionShape3D CollisionShape { get; private set; } = null!;
    [Export] public BaseMaterial3D Material { get; private set; } = null!;
    [Export] public float MaxLength { get; private set; } = 3.0f;
    [Export] public float Width { get; private set; } = 0.5f;
    [Export] public float Height { get; private set; } = 0.2f;
    public PipeJoint FrontPipeJoint { get; private set; } = null!;
    public PipeJoint BackPipeJoint { get; private set; } = null!;

    public void SetRenderId(int rendererId)
    {
        RendererId = rendererId;
        var label3D = GetNode<Label3D>("Label3D");
        label3D.Text = RendererId.ToString();
    }

    public ArrayMesh CreateMesh(float? length = null)
    {
        var pipeMeshCurve = new Curve3D();
        pipeMeshCurve.BakeInterval = 5.0f;
        pipeMeshCurve.AddPoint(Vector3.Back * ((length ?? ActualLength) * 0.5f));
        pipeMeshCurve.AddPoint(Vector3.Forward * ((length ?? ActualLength) * 0.5f));
        var meshExtruder = new MeshExtruder();
        var mesh = meshExtruder.Create(pipeMeshCurve, _meshVertices);
        mesh.SurfaceSetMaterial(0, Material);
        return mesh;
    }

    public bool CanCreateJointAtPosition(Vector3 globalPosition)
    {
        var backDistance = BackPipeJoint.GlobalPosition.DistanceTo(globalPosition);
        var frontDistance = FrontPipeJoint.GlobalPosition.DistanceTo(globalPosition);
        return backDistance > BackPipeJoint.MinDistanceBetweenJoints &&
               frontDistance > FrontPipeJoint.MinDistanceBetweenJoints;
    }

    public void SetFrontPipeJoint(PipeJoint pipeJoint)
    {
        FrontPipeJoint = pipeJoint;
    }

    public void SetBackPipeJoint(PipeJoint pipeJoint)
    {
        BackPipeJoint = pipeJoint;
    }
}