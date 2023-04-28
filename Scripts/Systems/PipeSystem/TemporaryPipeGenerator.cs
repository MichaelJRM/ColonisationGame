using System;
using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class TemporaryPipeGenerator : Node
{
    private readonly Func<bool> _isPlacementValidCallback;
    private readonly List<TemporaryPipe> _pipes = new();
    private Color _invalidPlacementColor = new(1.0f, 0.0f, 0.0f, 0.2f);
    private Vector3 _lastPosition = Vector3.Zero;
    private StandardMaterial3D _materialOverlay;
    private PackedScene _temporaryPipeScene;
    private Color _validPlacementColor = new(0.0f, 1.0f, 0.0f, 0.2f);


    public TemporaryPipeGenerator(PackedScene temporaryPipeScene, Func<bool> isPlacementValidCallback)
    {
        _isPlacementValidCallback = isPlacementValidCallback;
        _materialOverlay = new StandardMaterial3D();
        _materialOverlay.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        _temporaryPipeScene = temporaryPipeScene;
    }

    public bool IsPlacementValid { get; private set; }


    public void Update(
        Transform3D from,
        Transform3D to
    )
    {
        if (_lastPosition == to.Origin) return;
        _lastPosition = to.Origin;

        var firstPipe = _temporaryPipeScene.Instantiate<TemporaryPipe>();
        firstPipe.ActualLength = firstPipe.MaxLength;
        firstPipe.QueueFree();
        var pipeLength = firstPipe.MaxLength;
        var distance = from.Origin.DistanceTo(to.Origin);
        var count = Mathf.CeilToInt(distance / pipeLength);
        var transform = from.LookingAt(to.Origin, from.Basis.Y);

        if (count > _pipes.Count)
        {
            if (_pipes.Count != 0)
            {
                // The last pipe size was adjusted to fit the distance to the joint so we need to reset it.
                var pipe = _pipes.Last();
                pipe.CreateAndAssignMesh();
                ((BoxShape3D)pipe.CollisionShape.Shape).Size =
                    new Vector3(firstPipe.Width, firstPipe.Height, pipeLength);
            }


            for (var i = 0; i < count - _pipes.Count; i++)
            {
                var pipe = _temporaryPipeScene.Instantiate<TemporaryPipe>(PackedScene.GenEditState.Instance);
                pipe.ActualLength = pipeLength;
                pipe.CreateAndAssignMesh();
                pipe.MeshInstance3D.MaterialOverlay = _materialOverlay;
                var shape = new BoxShape3D();
                pipe.CollisionShape.Shape = shape;
                shape.Size = new Vector3(firstPipe.Width, firstPipe.Height, pipeLength);
                _pipes.Add(pipe);
                AddChild(pipe);
            }
        }
        else if (count < _pipes.Count)
        {
            for (var i = 0; i < _pipes.Count - count; i++)
            {
                var pipe = _pipes.Last();
                pipe.QueueFree();
                _pipes.RemoveAt(_pipes.Count - 1);
            }
        }

        if (count == 0) return;

        for (var i = 0; i < _pipes.Count; i++)
            _pipes[i].GlobalTransform =
                transform.TranslatedLocal(new Vector3(0.0f, 0.0f, -(pipeLength * i + pipeLength / 2)));


        // Adjust the size of the last pipe to fit the distance to the joint.
        var lastPipe = _pipes.Last();
        var isPipeOriginBehindTarget =
            lastPipe.GlobalPosition.DirectionTo(to.Origin).Dot(-lastPipe.Basis.Z) > 0;
        var lastPipeDistanceToJoint = lastPipe.GlobalPosition.DistanceTo(to.Origin);
        var overflow = isPipeOriginBehindTarget
            ? pipeLength / 2 - lastPipeDistanceToJoint
            : pipeLength / 2 + lastPipeDistanceToJoint;
        lastPipe.Transform =
            lastPipe.Transform.TranslatedLocal(new Vector3(0.0f, 0.0f, overflow / 2));
        lastPipe.CreateAndAssignMesh(pipeLength - overflow);
        ((BoxShape3D)lastPipe.CollisionShape.Shape).Size =
            new Vector3(firstPipe.Width, firstPipe.Height, pipeLength - overflow);

        _calculateIfPlacementIsValid();
        _materialOverlay.AlbedoColor = IsPlacementValid && _isPlacementValidCallback.Invoke()
            ? _validPlacementColor
            : _invalidPlacementColor;
    }

    public List<(Pipe, Vector3)> GetIntersectingPipes()
    {
        var snap = new Vector3(0.01f, 0.01f, 0.01f);
        var overlappingPerTempPipe = _pipes.Select(
            e => (e, e.GetOverlappingAreas().Where(c => c is Pipe and not TemporaryPipe))
        ).ToList();
        var allIntersectingPipes = new List<(Pipe, Vector3)>(_pipes.Count);
        var firstTemporaryPipeGlobalPosition = overlappingPerTempPipe.Count > 0
            ? overlappingPerTempPipe.First().Item1.GlobalPosition
            : Vector3.Zero;
        foreach (var (temporaryPipe, area3Ds) in overlappingPerTempPipe)
        {
            var overlappingPipes = area3Ds
                .Select(e => ((Pipe)e, e.GlobalPosition.DistanceSquaredTo(firstTemporaryPipeGlobalPosition)))
                .OrderBy(e => e.Item2).Select(e => e.Item1);

            foreach (var pipe in overlappingPipes)
            {
                var intersectionPoint = _calculateIntersectionPoint(pipe, temporaryPipe);
                var canSave =
                    intersectionPoint != null &&
                    (
                        allIntersectingPipes.Count == 0
                        || allIntersectingPipes.All(
                            e => e.Item2.Snapped(snap) != ((Vector3)intersectionPoint).Snapped(snap)
                        ));
                if (!canSave) continue;
                allIntersectingPipes.Add((pipe, (Vector3)intersectionPoint!));
            }
        }

        return allIntersectingPipes.ToList();
    }

    private Vector3? _calculateIntersectionPoint(Pipe pipe, TemporaryPipe temporaryPipe)
    {
        var intersectionPoint = MathUtil.CalculateIntersectionPoint(
            pipe.GlobalTransform,
            temporaryPipe.GlobalTransform
        );
        if (pipe.GlobalPosition.DistanceTo(intersectionPoint) > pipe.ActualLength) return null;
        return intersectionPoint;
    }

    private void _calculateIfPlacementIsValid()
    {
        foreach (var temporaryPipe in _pipes)
        {
            var overlappingAreas = temporaryPipe.GetOverlappingAreas();
            var isCollidingWithObstacle =
                overlappingAreas.Any(e => e is not PipeJoint && e.GetOwnerOrNull<Building>() != null);
            if (isCollidingWithObstacle)
            {
                IsPlacementValid = false;
                return;
            }
        }

        IsPlacementValid = true;
    }

    public void Clear()
    {
        _pipes.ForEach(e => e.QueueFree());
        _pipes.Clear();
    }
}