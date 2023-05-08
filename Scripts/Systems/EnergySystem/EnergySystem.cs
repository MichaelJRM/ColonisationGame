using System;
using System.Collections.Generic;
using System.Linq;
using BaseBuilding.Scripts.Systems.EnergySystem.Wire;
using BaseBuilding.Scripts.Systems.EnergySystem.WirePlacement;
using BaseBuilding.Scripts.Systems.SaveSystem;
using BaseBuilding.Scripts.Util.objects;
using Godot;
using OneOf;

namespace BaseBuilding.Scripts.Systems.EnergySystem;

public sealed partial class EnergySystem : Node3D, IPersistentManager
{
    [Export] private PackedScene _wireJointScene = null!;
    [Export] private PackedScene _temporaryWireJointScene = null!;
    private bool _isEnabled;
    private WirePlacementSystem? _wirePlacementSystem;
    public readonly ResourceLineManager<WireJoint, WireConnector> WireLineManager = new();
    private readonly Dictionary<Eid, WireJoint> _wireJoints = new();
    private ulong _universalJointIdCounter = 1;


    private EnergySystem()
    {
    }

    public static EnergySystem Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
    }


    private WireJoint GetWireJoint(Eid rid) => _wireJoints[rid];

    public void RegisterWireConnector(WireConnector connector)
    {
        _wireJoints.Add(connector.Eid, connector);
    }

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
        _wirePlacementSystem?.Dispose();
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
            OneOf<WireJoint, TemporaryWireJoint, Wire.WireConnector> item
        )
        {
            return item.Match<OneOf<WireJoint, Wire.WireConnector>>(
                wireJoint => wireJoint,
                tempWireJoint => CreatePermanentWireJointFromTemporary(tempWireJoint),
                wireConnector => wireConnector
            );

            WireJoint CreatePermanentWireJointFromTemporary(TemporaryWireJoint tempWireJoint)
            {
                var instance = _wireJointScene.Instantiate<WireJoint>();
                instance.SetId(GetNewJointEid());
                instance.Position = tempWireJoint.GlobalPosition;
                _commitWireJoint(instance);
                tempWireJoint.QueueFree();
                return instance;
            }
        }

        void ConnectJoints(
            WireJoint startJoint,
            WireJoint endJoint
        )
        {
            startJoint.ConnectToJoint(endJoint);
            WireLineManager.Connect(startJoint, endJoint);
        }
    }

    private void _commitWireJoint(WireJoint joint)
    {
        AddChild(joint);
        _wireJoints.Add(joint.Eid, joint);
    }

    public void _AddSaveChild(Node child)
    {
        _commitWireJoint((WireJoint)child);
    }

    public IPersistent[] _GetPersistentNodes()
    {
        // We ignore WireConnectors as those are saved as part of the building they belong to.
        // ReSharper disable once CoVariantArrayConversion
        return _wireJoints.Values.Where(e => e is not WireConnector).ToArray();
    }

    public string _GetSavePath()
    {
        return "wireSystem.save";
    }

    public void _AfterLoad()
    {
        if (_wireJoints.Count == 0) return;

        var largestEid = _wireJoints.MaxBy(e => e.Value.Eid).Value.Eid;
        _universalJointIdCounter = largestEid.Id + 1;

        var jointsGroupedByLineId = _wireJoints.Values.GroupBy(joint => joint.GetLineId()).ToArray();

        var jointConnectionData = new Dictionary<Eid, List<Eid>>();
        foreach (var key in _wireJoints.Keys)
        {
            jointConnectionData.Add(key, new List<Eid>());
        }

        foreach (var group in jointsGroupedByLineId)
        {
            if (group.Key == null) continue;
            var lineId = (uint)group.Key;
            WireLineManager.CreateLine(lineId);

            var joints = group.ToArray();

            foreach (var wireJoint in joints)
            {
                ConnectJoints(wireJoint);
                WireLineManager.AddBasedOnType(lineId, wireJoint);
            }
        }

        void ConnectJoints(WireJoint joint)
        {
            var thisData = jointConnectionData[joint.Eid];
            foreach (var connectedJointId in joint.ConnectedJointsIds)
            {
                if (thisData.Contains(connectedJointId)) continue;
                thisData.Add(connectedJointId);
                jointConnectionData[connectedJointId].Add(joint.Eid);
                var wireJoint = GetWireJoint(connectedJointId);
                joint.CreateWireBetweenJoints(wireJoint);
            }
        }
    }

    public Eid GetNewJointEid()
    {
        return new Eid(_universalJointIdCounter++);
    }
}