using System;
using System.Linq;
using BaseBuilding.Scripts.Systems.EnergySystem.WirePlacement;
using Godot;
using OneOf;

namespace BaseBuilding.Scripts.Systems.EnergySystem;

public partial class EnergySystem : Node3D
{
    private bool _isEnabled;
    private WirePlacementSystem? _wirePlacementSystem;
    [Export] private PackedScene _wireJointScene = null!;
    [Export] private PackedScene _temporaryWireJointScene = null!;

    private readonly ResourceLineManager<WireJoint, Wire.WireConnector> _wireLineManager = new();

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event.IsActionPressed("wire_system_toggle")) _toggle();
    }

    private void _toggle()
    {
        if (_isEnabled)
            _disable();
        else
            _enable();
    }

    private void _enable()
    {
        _isEnabled = true;
        _initWirePlacementSystem();
        _wirePlacementSystem!.Enable();
    }

    private void _disable()
    {
        _isEnabled = false;
        _disposeWirePlacementSystem();
    }

    private void _initWirePlacementSystem()
    {
        _wirePlacementSystem = new WirePlacementSystem(
            onPlace: _registerNewWireJoints,
            temporaryWireJointScene: _temporaryWireJointScene
        );
        AddChild(_wirePlacementSystem);
    }

    private void _disposeWirePlacementSystem()
    {
        _wirePlacementSystem?.Disable();
        _wirePlacementSystem = null;
    }

    private void _resetWirePlacementSystem()
    {
        _disposeWirePlacementSystem();
        _initWirePlacementSystem();
    }

    private void _registerNewWireJoints(OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector>[] items)
    {
        var permanentJoints = items.Select(TransformTempIntoPermIfNecessary).ToArray().AsSpan();
        for (var i = 0; i < permanentJoints.Length - 1; i++)
        {
            ConnectJoints((WireJoint)permanentJoints[i].Value, (WireJoint)permanentJoints[i + 1].Value);
        }

        _resetWirePlacementSystem();
        permanentJoints[^1].Switch(
            wireJoint => _wirePlacementSystem!.Enable(wireJoint),
            _ => _wirePlacementSystem!.Enable()
        );
        return;


        // Helper Functions //
        OneOf<WireJoint, Wire.WireConnector> TransformTempIntoPermIfNecessary(
            OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector> item)
        {
            return item.Match<OneOf<WireJoint, Wire.WireConnector>>(
                wireJoint => wireJoint,
                tempWireJoint => CreatePermanentWireJointFromTemporary(tempWireJoint),
                wireConnector => wireConnector
            );

            WireJoint CreatePermanentWireJointFromTemporary(TemporaryWireJoint tempWireJoint)
            {
                var instance = _wireJointScene.Instantiate<WireJoint>();
                instance.Position = tempWireJoint.GlobalPosition;
                CommitWireJoint(instance);
                tempWireJoint.QueueFree();
                return instance;
            }

            void CommitWireJoint(WireJoint joint)
            {
                AddChild(joint);
            }
        }

        void ConnectJoints(
            WireJoint startJoint,
            WireJoint endJoint
        )
        {
            startJoint.ConnectToJoint(endJoint);
            _wireLineManager.Connect(startJoint, endJoint);
        }
    }
}