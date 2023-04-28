using System;
using BaseBuilding.scripts.systems.BuildingSystem;
using Godot;

namespace BaseBuilding.Scripts.Player.UI;

public partial class ToolbarSubItem : TextureButton
{
    private BuildingResource _resource = null!;

    public void Init(BuildingResource resource)
    {
        if (_resource != null) throw new Exception("Resource already initialized");
        _resource = resource;
        var label = GetNode<Label>("Label");
        label.Text = resource.Name;
    }
}