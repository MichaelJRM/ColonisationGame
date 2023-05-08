using System;
using System.Collections.Generic;
using BaseBuilding.Scripts.Util.Extensions;
using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.Wire;

[Tool]
public partial class WireConnector : WireJoint, IResourceConnector
{
    private Resource _resource = null!;

    [Export]
    private Resource Resource
    {
        get => _resource;
        set
        {
            _resource = value;
            UpdateConfigurationWarnings();
        }
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        _validate();
        Monitoring = false;
        Monitorable = false;
        this.DisableColliders();
    }

    public void Activate()
    {
        Monitorable = true;
        this.EnableColliders();

        if (!LoadedFromSave)
        {
            Eid = EnergySystem.Instance.GetNewJointEid();
            EnergySystem.Instance.RegisterWireConnector(this);
        }
    }

    public object GetOwner()
    {
        return GetParent();
    }

    public bool IsConnected()
    {
        return ConnectedJointsIds.Count > 0;
    }

    public bool AcceptsResource(WorldResource worldResource)
    {
        return worldResource.Id == ((WorldResource)_resource).Id;
    }


    public WorldResource GetAcceptedResource()
    {
        return (WorldResource)_resource;
    }

    private void _validate()
    {
        if (_resource == null)
        {
            throw new Exception("Resource is not set!");
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new List<string>();

        if (_resource == null)
        {
            warnings.Add("Resource is not set!");
        }

        return warnings.ToArray();
    }

    public override void Load()
    {
        Eid = SaveContent!.Eid;
        LineId = SaveContent.Li;
        ConnectedJointsIds.AddRange(SaveContent.Cj);
        EnergySystem.Instance.RegisterWireConnector(this);
    }

    public override bool InstantiateOnLoad() => false;
}