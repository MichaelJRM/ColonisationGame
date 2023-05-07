using System;
using System.Collections.Generic;
using BaseBuilding.scripts.systems.PipeSystem;
using BaseBuilding.Scripts.Util.Extensions;
using BaseBuilding.Scripts.WorldResources;
using Godot;

namespace BaseBuilding.Scripts.Systems.PipeSystem.PipeConnector;

[Tool]
public partial class PipeConnector : PipeJoint, IResourceConnector
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
        scripts.systems.PipeSystem.PipeSystem.Instance.RegisterPipeJoint(this);
    }

    public bool IsConnected()
    {
        return ConnectedJointsIds.Count > 0;
    }

    public bool AcceptsResource(WorldResource worldResource)
    {
        return ((WorldResource)_resource).Id == worldResource.Id;
    }

    public object GetOwner()
    {
        return Owner;
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
}