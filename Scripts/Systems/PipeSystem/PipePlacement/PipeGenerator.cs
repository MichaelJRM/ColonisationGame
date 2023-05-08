using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.scripts.systems.PipeSystem;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipePlacement;

public partial class PipeGenerator : Node
{
    private readonly List<TemporaryPipe> _pipes = new();
    private readonly List<TemporaryPipe> _removedPipes = new();
    private readonly Func<bool> _isPlacementValidCallback;
    private readonly StandardMaterial3D _materialOverlay;
    private readonly PackedScene _temporaryPipeScene;
    private readonly Color _invalidPlacementColor = new(1.0f, 0.0f, 0.0f, 0.2f);
    private readonly Color _validPlacementColor = new(0.0f, 1.0f, 0.0f, 0.2f);
    private Vector3 _targetPosition = Vector3.Zero;
    private Vector3 _pipeSize;
    private ArrayMesh _pipeCommonMesh = null!;


    public PipeGenerator(PackedScene temporaryPipeScene, Func<bool> isPlacementValidCallback)
    {
        _isPlacementValidCallback = isPlacementValidCallback;
        _materialOverlay = new StandardMaterial3D();
        _materialOverlay.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        _temporaryPipeScene = temporaryPipeScene;
    }

    public bool IsPlacementValid { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        var pipe = _temporaryPipeScene.Instantiate<TemporaryPipe>();
        _pipeCommonMesh = pipe.CreateMesh(pipe.Length);
        _pipeSize = _pipeCommonMesh.GetAabb().Size;
        pipe.QueueFree();
    }


    public void Update(
        Transform3D from,
        Transform3D to
    )
    {
        IsPlacementValid = _calculateIfPlacementIsValid();
        if (_targetPosition == to.Origin) return;
        _targetPosition = to.Origin;

        var distance = from.Origin.DistanceTo(to.Origin);
        var count = Mathf.CeilToInt(distance / _pipeSize.Z);
        var transform = from.LookingAt(to.Origin, from.Basis.Y);

        if (count > _pipes.Count)
        {
            if (_pipes.Count != 0)
            {
                // The last pipe size was adjusted to fit the distance to the joint so we need to reset it.
                var pipe = _pipes.Last();
                pipe.MeshInstance3D.Mesh = _pipeCommonMesh;
                ((CylinderShape3D)pipe.CollisionShape.Shape).Height = _pipeSize.Z;
            }

            for (var i = 0; i < count - _pipes.Count; i++)
            {
                if (_removedPipes.Count > 0)
                {
                    var pipe = _removedPipes.Last();
                    _removedPipes.RemoveAt(_removedPipes.Count - 1);
                    _pipes.Add(pipe);
                }
                else
                {
                    var pipe = _temporaryPipeScene.Instantiate<TemporaryPipe>(PackedScene.GenEditState.Instance);
                    pipe.MeshInstance3D.Mesh = _pipeCommonMesh;
                    pipe.MeshInstance3D.MaterialOverlay = _materialOverlay;
                    var collisionShape = new CollisionShape3D();
                    var shape = new CylinderShape3D();
                    shape.Height = _pipeSize.Z;
                    shape.Radius = _pipeSize.X;
                    collisionShape.Shape = shape;
                    pipe.CollisionShape = collisionShape;
                    // The collision shape is created with the Z axis pointing up, but we want it to point forward.
                    collisionShape.RotateObjectLocal(Vector3.Right, Mathf.Tau / 4);
                    pipe.AddChild(collisionShape);
                    _pipes.Add(pipe);
                    AddChild(pipe);
                }
            }
        }
        else if (count < _pipes.Count)
        {
            var last = _pipes.Last();
            last.Translate(new Vector3(0.0f, -1000.0f, 0.0f));
            ((CylinderShape3D)last.CollisionShape.Shape).Height = _pipeSize.Z;
            last.MeshInstance3D.Mesh = _pipeCommonMesh;
            _removedPipes.Add(last);
            _pipes.RemoveAt(_pipes.Count - 1);

            for (var i = 0; i < _pipes.Count - count - 1; i++)
            {
                var pipe = _pipes.Last();
                pipe.Translate(new Vector3(0.0f, -1000.0f, 0.0f));
                _removedPipes.Add(pipe);
                _pipes.RemoveAt(_pipes.Count - 1);
            }
        }

        if (count <= 0) return;

        for (var i = 0; i < _pipes.Count; i++)
        {
            _pipes[i].GlobalTransform = transform.TranslatedLocal(
                new Vector3(0.0f, 0.0f, -(_pipeSize.Z * i + _pipeSize.Z * 0.5f))
            );
        }


        // Adjust the size of the last pipe to fit the distance to the joint.
        var lastPipe = _pipes.Last();
        var isPipeOriginBehindTarget =
            lastPipe.GlobalPosition.DirectionTo(to.Origin).Dot(-lastPipe.Basis.Z) > 0;
        var lastPipeDistanceToJoint = lastPipe.GlobalPosition.DistanceTo(to.Origin);
        var overflow = isPipeOriginBehindTarget
            ? _pipeSize.Z * 0.5f - lastPipeDistanceToJoint
            : _pipeSize.Z * 0.5f + lastPipeDistanceToJoint;
        lastPipe.Transform =
            lastPipe.Transform.TranslatedLocal(new Vector3(0.0f, 0.0f, overflow * 0.5f));
        lastPipe.CreateAndAssignMesh(_pipeSize.Z - overflow);
        ((CylinderShape3D)lastPipe.CollisionShape.Shape).Height = Mathf.Max(_pipeSize.Z - overflow, 0.0f);


        _materialOverlay.AlbedoColor = IsPlacementValid && _isPlacementValidCallback.Invoke()
            ? _validPlacementColor
            : _invalidPlacementColor;
    }

    public List<TemporaryPipe> GetPipes()
    {
        return _pipes;
    }

    private bool _calculateIfPlacementIsValid()
    {
        if (_pipes.Count == 0)
        {
            return true;
        }

        // We check if the first pipe is overlapping with more than one building because the first pipe will always collide
        // with at least one building.
        if (!IsEdgePipePlacementValid(_pipes[0]))
        {
            return false;
        }

        switch (_pipes.Count)
        {
            case > 1 when !IsEdgePipePlacementValid(_pipes[^1]):
                return false;
            case < 3:
                return true;
        }

        foreach (var temporaryPipe in CollectionsMarshal.AsSpan(_pipes).Slice(1, _pipes.Count - 2))
        {
            var overlappingAreas = temporaryPipe.GetOverlappingAreas();
            for (var i = 0; i < overlappingAreas.Count; i++)
            {
                var isCollidingWithPipeJoint = overlappingAreas[i].GlobalPosition != _targetPosition &&
                                               overlappingAreas[i].GetType() == typeof(PipeJoint);
                if (isCollidingWithPipeJoint) return false;
                var isCollidingWithBuilding = overlappingAreas[i].GetType() == typeof(BuildingCollisionArea);
                if (isCollidingWithBuilding) return false;
            }
        }

        return true;

        // Helper functions
        bool IsEdgePipePlacementValid(TemporaryPipe edgePipe)
        {
            var overlappingAreas = edgePipe.GetOverlappingAreas();
            if (overlappingAreas.Count == 1)
            {
                var area = overlappingAreas[0];
                if (area is IResourceConnector)
                {
                    return true;
                }

                if (area.Owner is Building)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void Clear()
    {
        foreach (var pipe in CollectionsMarshal.AsSpan(_pipes))
        {
            pipe.QueueFree();
        }

        _pipes.Clear();
        foreach (var pipe in CollectionsMarshal.AsSpan(_removedPipes))
        {
            pipe.QueueFree();
        }

        _removedPipes.Clear();
    }
}