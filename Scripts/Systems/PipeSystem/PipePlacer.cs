using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipePlacer : Node
{
    private readonly List<TemporaryPipeJoint> _intermediateJoints = new();
    private readonly Action<PipeJoint[]> _onPlace;
    private readonly PackedScene _pipeDetectorScene;
    private readonly PackedScene _temporaryJointScene;
    private readonly PackedScene _temporaryPipeScene;
    private readonly Node3D _context;
    private bool _areJointsValid;
    private PipeJoint? _startJoint;
    private PipeJoint? _endJoint;
    private PipeDetector _pipeDetector = null!;
    private TemporaryPipeGenerator _temporaryPipeGenerator = null!;
    private Status _status = Status.Disabled;


    public PipePlacer(
        Node3D context,
        Action<PipeJoint[]> onPlace,
        PackedScene pipeDetectorScene,
        PackedScene temporaryJointScene,
        PackedScene temporaryPipeScene
    )
    {
        _context = context;
        _onPlace = onPlace;
        _pipeDetectorScene = pipeDetectorScene;
        _temporaryJointScene = temporaryJointScene;
        _temporaryPipeScene = temporaryPipeScene;
    }

    public bool IsPlacementValid { get; private set; }

    public override void _Ready()
    {
        SetProcess(false);
        SetProcessUnhandledInput(false);
        _pipeDetector = _pipeDetectorScene.Instantiate<PipeDetector>();
        AddChild(_pipeDetector);
        _temporaryPipeGenerator = new TemporaryPipeGenerator(
            _temporaryPipeScene,
            () => IsPlacementValid
        );
        AddChild(_temporaryPipeGenerator);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("build_manager_place_item")) return;

        switch (_status)
        {
            case Status.Disabled:
                break;
            case Status.PlacingStartJoint:
                _status = Status.PlacingEndJoint;
                break;
            case Status.PlacingEndJoint:
                _status = Status.Disabled;
                _onPlace(_getAllJoints());
                break;
        }
    }

    public void Enable(PipeJoint? startJoint = null)
    {
        SetProcess(true);
        SetProcessUnhandledInput(true);
        _startJoint = startJoint;
        _endJoint = null;
        _status = _startJoint == null ? Status.PlacingStartJoint : Status.PlacingEndJoint;
    }

    public void Disable()
    {
        if (_startJoint is TemporaryPipeJoint) _startJoint.QueueFree();
        if (_endJoint is TemporaryPipeJoint) _endJoint.QueueFree();
        foreach (var joint in CollectionsMarshal.AsSpan(_intermediateJoints))
        {
            joint.QueueFree();
        }

        _temporaryPipeGenerator.Clear();
        QueueFree();
    }

    public override void _Process(double delta)
    {
        _calculate();
    }

    private void _calculate()
    {
        switch (_status)
        {
            case Status.Disabled:
                return;
            case Status.PlacingStartJoint:
                _calculateStartJoint();
                return;
            case Status.PlacingEndJoint:
                _calculateEndJoint();
                _calculateIntermediateJoints();
                _calculateIfPlacementIsValid();
                if (_startJoint != null && _endJoint != null)
                    _temporaryPipeGenerator.Update(_startJoint!.GlobalTransform, _endJoint!.GlobalTransform);
                return;
        }
    }


    private void _calculateStartJoint()
    {
        var closestDetectedPipeJoint = _pipeDetector.GetClosestDetectedPipeJoint();
        if (closestDetectedPipeJoint != null)
        {
            if (_startJoint is TemporaryPipeJoint) _startJoint.QueueFree();
            _startJoint = closestDetectedPipeJoint;
            return;
        }

        var closestDetectedPipe = _pipeDetector.GetClosestDetectedPipe();
        if (closestDetectedPipe != null)
        {
            if (_startJoint is TemporaryPipeJoint) _startJoint.QueueFree();
            _startJoint = _createTemporaryJointOnPipe(closestDetectedPipe, _pipeDetector.GlobalPosition);
            return;
        }

        if (_startJoint is TemporaryPipeJoint temporaryJoint)
        {
            temporaryJoint.OwnerPipe = null;
            temporaryJoint.GlobalPosition = _pipeDetector.GlobalPosition;
        }

        else
        {
            _startJoint = _createTemporaryJointAtPosition(_pipeDetector.GlobalPosition);
        }
    }

    private void _calculateEndJoint()
    {
        if (_endJoint?.GlobalPosition == _pipeDetector.GlobalPosition) return;

        var closestDetectedPipeJoint = _pipeDetector.GetClosestDetectedPipeJoint();
        if (closestDetectedPipeJoint != null)
        {
            if (closestDetectedPipeJoint == _startJoint)
            {
                _temporaryPipeGenerator.Clear();
                return;
            }

            if (_endJoint is TemporaryPipeJoint) _endJoint.QueueFree();
            _endJoint = closestDetectedPipeJoint;
            return;
        }


        var closestDetectedPipe = _pipeDetector.GetClosestDetectedPipe();
        if (closestDetectedPipe != null)
        {
            var newJoint = _createTemporaryJointOnPipe(closestDetectedPipe, _pipeDetector.GlobalPosition);
            if (_endJoint is TemporaryPipeJoint) _endJoint.QueueFree();
            _endJoint = newJoint;
            return;
        }

        if (_endJoint is TemporaryPipeJoint temporaryJoint)
        {
            temporaryJoint.OwnerPipe = null;
            temporaryJoint.GlobalPosition = _pipeDetector.GlobalPosition;
        }
        else
        {
            _endJoint = _createTemporaryJointAtPosition(_pipeDetector.GlobalPosition);
        }
    }

    private void _calculateIfPlacementIsValid()
    {
        var isAreaValid = _pipeDetector.IsAreaValid;
        var pipePlacementIsValid = _temporaryPipeGenerator.IsPlacementValid;

        var allJoints = new List<PipeJoint>(_intermediateJoints.Count + 2);
        if (_startJoint != null) allJoints.Add(_startJoint!);
        allJoints.AddRange(_intermediateJoints);
        if (_endJoint != null) allJoints.Add(_endJoint!);

        _areJointsValid = true;
        for (var i = 0; i < allJoints.Count - 1; i++)
            if (!allJoints[i].CanConnectToJoint(allJoints[i + 1]))
            {
                _areJointsValid = false;
                break;
            }

        IsPlacementValid = isAreaValid && pipePlacementIsValid && _areJointsValid;
    }

    private void _calculateIntermediateJoints()
    {
        foreach (var joint in CollectionsMarshal.AsSpan(_intermediateJoints))
        {
            joint.QueueFree();
        }

        _intermediateJoints.Clear();
        if (!_areJointsValid) return;

        var snap = new Vector3(0.01f, 0.01f, 0.01f);
        var intersectingPipes = _temporaryPipeGenerator.GetIntersectingPipes();
        if (intersectingPipes.Count > 0)
        {
            var removeFirst = intersectingPipes.First().Item2.Snapped(snap) ==
                              _startJoint!.GlobalPosition.Snapped(snap);
            if (intersectingPipes.Count > 1)
            {
                var removeLast = intersectingPipes.Last().Item2.Snapped(snap) ==
                                 _endJoint!.GlobalPosition.Snapped(snap);
                if (removeLast) intersectingPipes.RemoveAt(intersectingPipes.Count - 1);
            }

            if (removeFirst) intersectingPipes.RemoveAt(0);
        }

        foreach (var (pipe, intersectionGlobalPosition) in CollectionsMarshal.AsSpan(intersectingPipes))
        {
            if (!pipe.CanCreateJointAtPosition(intersectionGlobalPosition)) continue;
            var joint = _createTemporaryJointOnPipe(pipe, intersectionGlobalPosition);
            _intermediateJoints.Add(joint);
        }
    }

    private TemporaryPipeJoint _createTemporaryJointOnPipe(Pipe pipe, Vector3 globalPosition)
    {
        var pipeJoint = _temporaryJointScene.Instantiate<TemporaryPipeJoint>();
        pipeJoint.Position = MathUtil.GetParallelPosition(pipe.GlobalTransform, globalPosition);
        pipeJoint.OwnerPipe = pipe;
        _context.AddChild(pipeJoint);
        return pipeJoint;
    }

    private TemporaryPipeJoint _createTemporaryJointAtPosition(Vector3 globalPosition)
    {
        var pipeJoint = _temporaryJointScene.Instantiate<TemporaryPipeJoint>();
        pipeJoint.Position = globalPosition;
        _context.AddChild(pipeJoint);
        return pipeJoint;
    }

    private PipeJoint[] _getAllJoints()
    {
        var allJoints = new List<PipeJoint>(_intermediateJoints.Count + 2);
        allJoints.Add(_startJoint!);
        allJoints.AddRange(_intermediateJoints);
        allJoints.Add(_endJoint!);
        return allJoints.OrderBy(e => e.GlobalPosition.DistanceSquaredTo(_startJoint!.GlobalPosition)).ToArray();
    }

    private enum Status
    {
        Disabled,
        PlacingStartJoint,
        PlacingEndJoint
    }
}

internal partial class TemporaryPipeGenerator : Node
{
    private readonly Func<bool> _isPlacementValidCallback;
    private readonly List<TemporaryPipe> _pipes = new();
    private readonly List<TemporaryPipe> _removedPipes = new();
    private Color _invalidPlacementColor = new(1.0f, 0.0f, 0.0f, 0.2f);
    private Vector3 _lastPosition = Vector3.Zero;
    private StandardMaterial3D _materialOverlay;
    private PackedScene _temporaryPipeScene;
    private Color _validPlacementColor = new(0.0f, 1.0f, 0.0f, 0.2f);
    private Vector3 _pipeSize;
    private ArrayMesh _pipeCommonMesh = null!;


    public TemporaryPipeGenerator(PackedScene temporaryPipeScene, Func<bool> isPlacementValidCallback)
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
        _pipeCommonMesh = pipe.CreateMesh(false, pipe.Length);
        _pipeSize = _pipeCommonMesh.GetAabb().Size;
        pipe.QueueFree();
    }


    public void Update(
        Transform3D from,
        Transform3D to
    )
    {
        if (_lastPosition == to.Origin) return;
        _lastPosition = to.Origin;

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
                    shape.Radius = _pipeSize.X / 2;
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


        _calculateIfPlacementIsValid();
        _materialOverlay.AlbedoColor = IsPlacementValid && _isPlacementValidCallback.Invoke()
            ? _validPlacementColor
            : _invalidPlacementColor;
    }

    public List<(Pipe, Vector3)> GetIntersectingPipes()
    {
        var snap = new Vector3(0.01f, 0.01f, 0.01f);
        var allIntersectingPipes = new List<(Pipe, Vector3)>(_pipes.Count);
        var overlappingPerTemporaryPipe = OverlappingPipesPerTemporaryPipe();

        var firstTemporaryPipeGlobalPosition = overlappingPerTemporaryPipe.Length > 0
            ? overlappingPerTemporaryPipe[0].Item1.GlobalPosition
            : Vector3.Zero;

        foreach (var (temporaryPipe, pipes) in overlappingPerTemporaryPipe)
        {
            if (pipes.Count == 0) continue;
            var pipesSortedByDistance = pipes
                .Select(e => (e, e.GlobalPosition.DistanceSquaredTo(firstTemporaryPipeGlobalPosition)))
                .OrderBy(e => e.Item2)
                .Select(e => e.e)
                .ToArray();

            foreach (var pipe in pipesSortedByDistance)
            {
                var intersectionPoint = _calculateIntersectionPoint(pipe, temporaryPipe);
                if (intersectionPoint == null) continue;

                foreach (var intersectingPipe in CollectionsMarshal.AsSpan(allIntersectingPipes))
                {
                    if (intersectingPipe.Item2.Snapped(snap) == ((Vector3)intersectionPoint).Snapped(snap))
                    {
                        intersectionPoint = null;
                        break;
                    }
                }

                var canSave = intersectionPoint != null;
                if (!canSave) continue;
                allIntersectingPipes.Add((pipe, (Vector3)intersectionPoint!));
            }
        }

        return allIntersectingPipes;

        (TemporaryPipe, List<Pipe>)[] OverlappingPipesPerTemporaryPipe()
        {
            var overlappingPipesPerPipe = new (TemporaryPipe, List<Pipe>)[_pipes.Count];
            for (var i = 0; i < overlappingPipesPerPipe.Length; i++)
            {
                var overlappingAreas = _pipes[i].GetOverlappingAreas();
                var overlappingPipes = new List<Pipe>();
                for (var j = 0; j < overlappingAreas.Count; j++)
                {
                    var area = overlappingAreas[j];
                    if (area.GetType() == typeof(Pipe))
                    {
                        overlappingPipes.Add((Pipe)area);
                    }
                }

                overlappingPipesPerPipe[i] = (_pipes[i], overlappingPipes);
            }

            return overlappingPipesPerPipe;
        }
    }

    private Vector3? _calculateIntersectionPoint(Pipe pipe, TemporaryPipe temporaryPipe)
    {
        var intersectionPoint = MathUtil.CalculateIntersectionPoint(
            pipe.GlobalTransform,
            temporaryPipe.GlobalTransform
        );
        var pipeLength = ((CylinderShape3D)pipe.CollisionShape.Shape).Height;
        if (pipe.GlobalPosition.DistanceTo(intersectionPoint) > pipeLength) return null;
        return intersectionPoint;
    }

    private void _calculateIfPlacementIsValid()
    {
        foreach (var temporaryPipe in CollectionsMarshal.AsSpan(_pipes))
        {
            var overlappingAreas = temporaryPipe.GetOverlappingAreas();
            for (var i = 0; i < overlappingAreas.Count; i++)
            {
                var area = overlappingAreas[i];
                if (area.Owner != null && area.Owner.GetType() == typeof(Building))
                {
                    IsPlacementValid = false;
                    return;
                }
            }
        }

        IsPlacementValid = true;
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