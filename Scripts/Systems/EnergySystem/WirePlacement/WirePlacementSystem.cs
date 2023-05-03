using System;
using System.Linq;
using BaseBuilding.scripts.common;
using Godot;
using OneOf;
using OneOf.Types;

namespace BaseBuilding.Scripts.Systems.EnergySystem.WirePlacement;

public partial class WirePlacementSystem : Node
{
    private readonly Action<OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector>[]>
        _onPlace;

    private OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector, None> _endJoint;
    private OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector, None> _startJoint;
    private Status _status = Status.PlacingStartJoint;
    private WireDetector _wireDetector = new();
    private PackedScene _temporaryWireJointScene;
    private TickComponent _inputTick = new();
    private TemporaryWireGenerator _temporaryWireGenerator = new();

    public WirePlacementSystem(
        Action<OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector>[]> onPlace,
        PackedScene temporaryWireJointScene
    )
    {
        _onPlace = onPlace;
        _temporaryWireJointScene = temporaryWireJointScene;
    }

    private bool _isPlacementValid;

    public override void _Ready()
    {
        SetProcessUnhandledInput(false);
        AddChild(_wireDetector);
        AddChild(_inputTick);
        AddChild(_temporaryWireGenerator);
        _inputTick.Pause();
        _inputTick.SetTickRateInFps(60);
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
                if (_isPlacementValid)
                {
                    _onPlace(_getAllJoints());
                }

                break;
        }
    }

    public void Enable(WireJoint? startJoint = null)
    {
        _inputTick.Resume();
        SetProcessUnhandledInput(true);
        _startJoint = startJoint != null ? startJoint : new None();
        _endJoint = new None();
        _status = startJoint != null ? Status.PlacingEndJoint : Status.PlacingStartJoint;
    }

    public void Disable()
    {
        QueueFree();
    }

    private void _calculate()
    {
        switch (_status)
        {
            case Status.PlacingStartJoint:
                _calculateStartJoint();
                return;
            case Status.PlacingEndJoint:
                var updated = _calculateEndJoint();
                if (!updated) return;

                if (_startJoint.TryPickT3(out _, out _)) return;
                if (_endJoint.TryPickT3(out _, out _)) return;
                _temporaryWireGenerator.Update((WireJoint)_startJoint.Value, (WireJoint)_endJoint.Value);
                return;
        }
    }


    private void _calculateStartJoint()
    {
        var closestDetectedWireJoint = _wireDetector.GetClosestDetectedWireJoint();
        if (closestDetectedWireJoint != null && closestDetectedWireJoint.CanConnect())
        {
            if (_startJoint.TryPickT1(out var temporaryWireJoint, out _)) temporaryWireJoint.QueueFree();
            _startJoint = closestDetectedWireJoint as Wire.WireConnector ?? closestDetectedWireJoint;
            return;
        }

        if (_startJoint.TryPickT1(out var t, out _))
        {
            t.GlobalPosition = _wireDetector.GlobalPosition;
        }
        else
        {
            _startJoint = _createTemporaryWireJointAtPosition(_wireDetector.GlobalPosition);
        }
    }


    /// <summary>
    /// Returns true if the end joint position was updated.
    /// </summary>
    /// <returns></returns>
    private bool _calculateEndJoint()
    {
        if (
            _startJoint.TryPickT1(out var startTemporaryJoint, out _)
            && startTemporaryJoint.GlobalPosition == _wireDetector.GlobalPosition
        )
        {
            return false;
        }

        var closestDetectedWireJoint = _wireDetector.GetClosestDetectedWireJoint();
        if (closestDetectedWireJoint != null && closestDetectedWireJoint.CanConnect())
        {
            return closestDetectedWireJoint is Wire.WireConnector
                ? CalculateEndJointFromClosestWireConnector()
                : CalculateEndJointFromClosestWireJoint();
        }

        return CalculateEndJointFromTemporaryJoint();


        // Helper Functions //
        bool CalculateEndJointFromClosestWireConnector()
        {
            if (
                _startJoint.TryPickT2(out var connector, out _)
                && connector.GlobalPosition == closestDetectedWireJoint.GlobalPosition
            )
            {
                return false;
            }

            if (_endJoint.TryPickT1(out var endTemporaryJoint, out _)) endTemporaryJoint.QueueFree();
            _endJoint = (Wire.WireConnector)closestDetectedWireJoint;
            return true;
        }

        bool CalculateEndJointFromClosestWireJoint()
        {
            if (
                _startJoint.TryPickT0(out var startWireJoint, out _)
                && startWireJoint.GlobalPosition == closestDetectedWireJoint.GlobalPosition
            )
            {
                if (_endJoint.TryPickT1(out var t, out _))
                {
                    t.GlobalPosition = startWireJoint.GlobalPosition;
                    return true;
                }

                return false;
            }

            if (_endJoint.TryPickT1(out var endTemporaryWireJoint, out _)) endTemporaryWireJoint.QueueFree();
            _endJoint = closestDetectedWireJoint;
            return true;
        }

        bool CalculateEndJointFromTemporaryJoint()
        {
            if (_endJoint.TryPickT1(out var t, out _))
            {
                t.GlobalPosition = _wireDetector.GlobalPosition;
            }
            else
            {
                _endJoint = _createTemporaryWireJointAtPosition(_wireDetector.GlobalPosition);
            }

            return true;
        }
    }

    private void _calculateIfPlacementIsValid()
    {
        var overlappingAreas = _wireDetector.GetOverlappingAreas();
        var isColliding = !_endJoint.IsT2
                          && overlappingAreas.Count != 0
                          && overlappingAreas.Any(e => e is not WireJoint);
        var areJointsAtDifferentPositions = ((WireJoint)_startJoint.Value).GlobalPosition !=
                                            ((WireJoint)_endJoint.Value).GlobalPosition;
        _isPlacementValid = areJointsAtDifferentPositions && !isColliding;
    }

    private TemporaryWireJoint _createTemporaryWireJointAtPosition(Vector3 wireDetectorGlobalPosition)
    {
        var wireJoint = _temporaryWireJointScene.Instantiate<TemporaryWireJoint>();
        wireJoint.Position = wireDetectorGlobalPosition;
        AddChild(wireJoint);
        return wireJoint;
    }

    private OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector>[] _getAllJoints()
    {
        return new[]
        {
            _startJoint.Match<OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector>>(
                wireJoint => wireJoint,
                temporaryWireJoint => temporaryWireJoint,
                wireConnector => wireConnector,
                null
            ),
            _endJoint.Match<OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector>>(
                wireJoint => wireJoint,
                temporaryWireJoint => temporaryWireJoint,
                wireConnector => wireConnector,
                null
            ),
        };
    }

    private enum Status
    {
        Disabled,
        PlacingStartJoint,
        PlacingEndJoint
    }
}

public partial class TemporaryWireGenerator : Node
{
    private Vector3 _lastPosition = Vector3.Zero;
    private MeshInstance3D _meshInstance = new();

    public override void _Ready()
    {
        AddChild(_meshInstance);
    }

    public void Update(
        WireJoint from,
        WireJoint to
    )
    {
        if (_lastPosition == to.GlobalPosition) return;
        _lastPosition = to.GlobalPosition;
        _meshInstance.GlobalTransform = to.GlobalTransform;
        var mesh = to.CreateWire(from.WireOrigin.GlobalTransform);
        _meshInstance.Mesh = mesh;
    }
}