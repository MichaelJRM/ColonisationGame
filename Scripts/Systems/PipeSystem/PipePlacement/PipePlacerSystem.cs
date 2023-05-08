using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseBuilding.scripts.common;
using BaseBuilding.scripts.systems.PipeSystem;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipePlacement;

public partial class PipePlacerSystem : Node
{
    private readonly List<TemporaryPipeJoint> _intermediateJoints = new();
    private readonly List<TemporaryPipeJoint> _intermediateJointsPoll = new();
    private readonly Action<PipeJoint[]> _onPlace;
    private readonly PackedScene _temporaryJointScene;
    private readonly PackedScene _temporaryPipeScene;
    private bool _areJointsValid;
    private PipeJoint? _startJoint;
    private PipeJoint? _endJoint;
    private PipeDetector _pipeDetector = new();
    private PipeGenerator _temporaryPipeGenerator = null!;
    private TickComponent _inputTick = new();
    private Status _status = Status.Disabled;


    public PipePlacerSystem(
        Action<PipeJoint[]> onPlace,
        PackedScene temporaryJointScene,
        PackedScene temporaryPipeScene
    )
    {
        _onPlace = onPlace;
        _temporaryJointScene = temporaryJointScene;
        _temporaryPipeScene = temporaryPipeScene;
    }

    public bool IsPlacementValid { get; private set; }

    public override void _Ready()
    {
        SetProcessUnhandledInput(false);
        AddChild(_pipeDetector);
        _temporaryPipeGenerator = new PipeGenerator(_temporaryPipeScene, () => IsPlacementValid);
        AddChild(_temporaryPipeGenerator);
        AddChild(_inputTick);
        _inputTick.SetTickRateInFps(90);
        _inputTick.SetOnTick(_calculate);
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
                _calculateIfPlacementIsValid();
                if (IsPlacementValid)
                {
                    _onPlace(_getAllJoints());
                }

                break;
        }
    }

    public void Enable(PipeJoint? startJoint = null)
    {
        _inputTick.Resume();
        SetProcessUnhandledInput(true);
        _startJoint = startJoint;
        _endJoint = null;
        _status = _startJoint == null ? Status.PlacingStartJoint : Status.PlacingEndJoint;
    }

    public void Disable()
    {
        QueueFree();
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
                var updated = _calculateEndJoint();
                if (!updated) return;

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

    private bool _calculateEndJoint()
    {
        if (_endJoint?.GlobalPosition == _pipeDetector.GlobalPosition) return false;

        var closestDetectedPipeJoint = _pipeDetector.GetClosestDetectedPipeJoint();
        if (closestDetectedPipeJoint != null)
        {
            if (closestDetectedPipeJoint == _startJoint)
            {
                _temporaryPipeGenerator.Clear();
                return true;
            }

            if (_endJoint is TemporaryPipeJoint) _endJoint.QueueFree();
            _endJoint = closestDetectedPipeJoint;
            return true;
        }


        var closestDetectedPipe = _pipeDetector.GetClosestDetectedPipe();
        if (closestDetectedPipe != null)
        {
            var newJoint = _createTemporaryJointOnPipe(closestDetectedPipe, _pipeDetector.GlobalPosition);
            if (_endJoint is TemporaryPipeJoint) _endJoint.QueueFree();
            _endJoint = newJoint;
            return true;
        }

        if (_endJoint is TemporaryPipeJoint temporaryJoint)
        {
            temporaryJoint.OwnerPipe = null;
            temporaryJoint.ConnectedPipes.Clear();
            temporaryJoint.GlobalPosition = _pipeDetector.GlobalPosition;
        }
        else
        {
            _endJoint = _createTemporaryJointAtPosition(_pipeDetector.GlobalPosition);
        }

        return true;
    }

    private void _calculateIfPlacementIsValid()
    {
        var isPipePlacementValid = _temporaryPipeGenerator.IsPlacementValid;
        if (!isPipePlacementValid)
        {
            IsPlacementValid = false;
            return;
        }

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

        IsPlacementValid = _areJointsValid;
    }

    private void _calculateIntermediateJoints()
    {
        foreach (var joint in CollectionsMarshal.AsSpan(_intermediateJointsPoll))
        {
            joint.OwnerPipe = null;
            joint.GlobalPosition = new Vector3(0.0f, -1000.0f, 0.0f);
        }

        _intermediateJoints.Clear();

        var intersectionPoints = _calculatePointsOfIntersectionWithPipes(_temporaryPipeGenerator.GetPipes());
        var pollJointIndex = _intermediateJointsPoll.Count - 1;
        foreach (var (pipe, intersectionGlobalPosition) in CollectionsMarshal.AsSpan(intersectionPoints))
        {
            if (pollJointIndex >= 0)
            {
                var pollJoint = _intermediateJointsPoll[pollJointIndex];
                pollJointIndex--;
                pollJoint.OwnerPipe = pipe;
                pollJoint.GlobalPosition =
                    MathUtil.GetParallelPosition(pipe.GlobalTransform, intersectionGlobalPosition);
                _intermediateJoints.Add(pollJoint);
            }
            else
            {
                var joint = _createTemporaryJointOnPipe(pipe, intersectionGlobalPosition);
                _intermediateJointsPoll.Add(joint);
                _intermediateJoints.Add(joint);
            }
        }
    }

    private List<(Pipe, Vector3)> _calculatePointsOfIntersectionWithPipes(List<TemporaryPipe> pipes)
    {
        var intersections = new List<(Pipe, Vector3)>();
        var overlappingPipes = new List<Pipe>();
        foreach (var temporaryPipe in CollectionsMarshal.AsSpan(pipes))
        {
            overlappingPipes.Clear();
            overlappingPipes.AddRange(temporaryPipe.GetOverlappingAreas().OfType<Pipe>());
            foreach (var overlappingPipe in CollectionsMarshal.AsSpan(overlappingPipes))
            {
                if (_startJoint!.ConnectedPipes.Contains(overlappingPipe)) continue;
                if (_endJoint.ConnectedPipes.Contains(overlappingPipe)) continue;
                if (intersections.Any(e => e.Item1 == overlappingPipe)) continue;
                var intersectionPoint = MathUtil.CalculateIntersectionPoint(
                    overlappingPipe.GlobalTransform,
                    temporaryPipe.GlobalTransform
                );
                if (intersections.Count != 0 && intersectionPoint.DistanceSquaredTo(intersections.Last().Item2) < 0.6f)
                {
                    continue;
                }

                var pipeLength = ((CylinderShape3D)overlappingPipe.CollisionShape.Shape).Height;
                if (!(overlappingPipe.GlobalPosition.DistanceTo(intersectionPoint) <= pipeLength) ||
                    !overlappingPipe.CanCreateJointAtPosition(intersectionPoint)) continue;

                intersections.Add((overlappingPipe, intersectionPoint));
            }
        }

        return intersections;
    }

    private TemporaryPipeJoint _createTemporaryJointOnPipe(Pipe pipe, Vector3 globalPosition)
    {
        var pipeJoint = _temporaryJointScene.Instantiate<TemporaryPipeJoint>();
        pipeJoint.Position = MathUtil.GetParallelPosition(pipe.GlobalTransform, globalPosition);
        pipeJoint.OwnerPipe = pipe;
        pipeJoint.ConnectedPipes.AddRange(pipe.BackJoint.ConnectedPipes.Intersect(pipe.FrontJoint.ConnectedPipes));
        pipeJoint.ConnectedJointsIds.Add(pipe.BackJoint.Eid);
        pipeJoint.ConnectedJointsIds.Add(pipe.FrontJoint.Eid);
        AddChild(pipeJoint);
        return pipeJoint;
    }

    private TemporaryPipeJoint _createTemporaryJointAtPosition(Vector3 globalPosition)
    {
        var pipeJoint = _temporaryJointScene.Instantiate<TemporaryPipeJoint>();
        pipeJoint.Position = globalPosition;
        AddChild(pipeJoint);
        return pipeJoint;
    }

    private PipeJoint[] _getAllJoints()
    {
        _calculateIntermediateJoints();
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