using System;
using BaseBuilding.scripts.systems.BuildingSystem;
using Godot;

namespace BaseBuilding.Scripts.Player.UI;

public partial class ToolbarItem : TextureButton
{
    [Export] private PackedScene _subItemScene = null!;
    private HBoxContainer _resourcesContainer = null!;
    public event EventHandler<BuildingResource>? BuildingPressed;
    public event Action<ToolbarItem>? Toggle;

    public override void _Ready()
    {
        if (_subItemScene == null) throw new ArgumentException("SubItemScene is null");
        _resourcesContainer = GetNode<HBoxContainer>("HBoxContainer");
        _resourcesContainer.Visible = false;
        Pressed += _onPressed;
    }

    public void Init(BuildingResource[] resources)
    {
        foreach (var resource in resources)
        {
            var subItem = _subItemScene.Instantiate<ToolbarSubItem>();
            subItem.Init(resource);
            subItem.Pressed += () => _onSubItemPressed(resource);
            _resourcesContainer.AddChild(subItem);
        }
    }

    public void ShowSubItems()
    {
        _resourcesContainer.Visible = true;
    }

    public void HideSubItems()
    {
        _resourcesContainer.Visible = false;
    }

    private void _onPressed()
    {
        Toggle?.Invoke(this);
    }

    private void _onSubItemPressed(BuildingResource buildingResource)
    {
        BuildingPressed?.Invoke(this, buildingResource);
    }
}