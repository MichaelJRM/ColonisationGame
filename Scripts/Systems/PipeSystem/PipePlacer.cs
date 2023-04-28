using System;
using System.Collections.Generic;
using System.Linq;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class PipePlacer : Node
{
    private readonly List<TemporaryPipeJoint> _intermediateJoints = new();
    private readonly Action<PipeJoint[]> _onPlace;
    private readonly PackedScene _pipeDetectorScene;
    private readonly PackedScene _temporaryJointScene;
    private bool _areJointsValid;
    private Node3D _context;
    private PipeJoint? _endJoint;
    private PipeDetector _pipeDetector = null!;
    private PipeJoint? _startJoint;

    private Status _status = Status.Disabled;
    private TemporaryPipeGenerator _temporaryPipeGenerator = null!;
    private PackedScene _temporaryPipeScene;

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
        _endJoint = startJoint;
        _status = _startJoint == null ? Status.PlacingStartJoint : Status.PlacingEndJoint;
    }

    public void Disable()
    {
        if (_startJoint is TemporaryPipeJoint) _startJoint.QueueFree();
        if (_endJoint is TemporaryPipeJoint) _endJoint.QueueFree();
        _intermediateJoints.ForEach(e => e.QueueFree());
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
                break;
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

    private void _calculateIntermediateJoints()
    {
        _intermediateJoints.ForEach(e => e.QueueFree());
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

        foreach (var (pipe, intersectionGlobalPosition) in intersectingPipes)
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