using System;
using System.Collections.Generic;
using BaseBuilding.scripts.common;
using BaseBuilding.Scripts.Systems;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class Pipe : Area3D
{
    [Export] private Vector2[] _meshShape = Array.Empty<Vector2>();
    [Export] public BaseMaterial3D Material { get; private set; } = null!;
    [Export] public float Length { get; private set; } = 3.0f;
    public uint? RenderId { get; private set; }
    public PipeJoint FrontJoint { get; private set; } = null!;
    public PipeJoint BackJoint { get; private set; } = null!;
    public CollisionShape3D CollisionShape = null!;


    public void SetRenderId(uint renderId)
    {
        RenderId = renderId;
    }

    public ArrayMesh CreateMesh(bool generateLods, float? length = null)
    {
        var pipeMeshCurve = new Curve3D();
        pipeMeshCurve.BakeInterval = 5.0f;
        var meshLength = length ?? ((CylinderShape3D)CollisionShape.Shape).Height;
        pipeMeshCurve.AddPoint(Vector3.Back * (meshLength * 0.5f));
        pipeMeshCurve.AddPoint(Vector3.Forward * (meshLength * 0.5f));
        // var mesh = meshExtruder.Create(pipeMeshCurve, generateLods ? GenerateLods() : new[] { _meshShape });
        var mesh = MeshExtruder.Create(pipeMeshCurve, new[] { _meshShape });
        mesh.SurfaceSetMaterial(0, Material);
        return mesh;

        // Vector2[][] GenerateLods()
        // {
        //     var lods = new Vector2[3][];
        //     lods[0] = _meshShape;
        //     lods[1] = GenerateLod(2);
        //     lods[2] = GenerateLod(3);
        //     return lods;
        //
        //     Vector2[] GenerateLod(int divider)
        //     {
        //         var lod = new List<Vector2>();
        //         var numVerticesToRemove = Mathf.Max(_meshShape.Length - 2 - (_meshShape.Length - 2) / divider, 0);
        //         if (numVerticesToRemove == 0)
        //         {
        //             return lod.ToArray();
        //         }
        //
        //         var interval = _meshShape.Length - 3 / numVerticesToRemove;
        //         lod.Add(_meshShape[0]);
        //         for (var j = 1; j < numVerticesToRemove + 1; j++)
        //         {
        //             var index = Mathf.RoundToInt(interval * j);
        //             lod.Add(_meshShape[index]);
        //         }
        //
        //         lod.Add(_meshShape[_meshShape.Length - 1]);
        //         return lod.ToArray();
        //     }
        // }
    }

    public bool CanCreateJointAtPosition(Vector3 globalPosition)
    {
        var backDistance = BackJoint.GlobalPosition.DistanceTo(globalPosition);
        var frontDistance = FrontJoint.GlobalPosition.DistanceTo(globalPosition);
        var minDistanceBetweenJoints = BackJoint.Mesh.GetAabb().GetLongestAxisSize();
        return backDistance > minDistanceBetweenJoints && frontDistance > minDistanceBetweenJoints;
    }

    public void SetFrontJoint(PipeJoint pipeJoint)
    {
        FrontJoint = pipeJoint;
    }

    public void SetBackJoint(PipeJoint pipeJoint)
    {
        BackJoint = pipeJoint;
    }
}