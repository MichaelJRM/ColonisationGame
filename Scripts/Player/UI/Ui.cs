using System.Linq;
using BaseBuilding.scripts.systems.BuildingSystem;
using Godot;

namespace BaseBuilding.Scripts.Player.UI;

public partial class Ui : Control
{
    private BuildingSystem _buildingSystem = null!;
    private ToolbarItem? _currentOpenItem;

    public override void _Ready()
    {
        _buildingSystem = GetNode<BuildingSystem>("/root/BuildingSystem");
        var buildingDatabase = GetNode<BuildingDataBase>("/root/BuildingDataBase");
        var constructionItem = GetNode<ToolbarItem>("MarginContainer/Toolbar/Construction");
        constructionItem.Init(
            buildingDatabase.ResourceExtractors.Concat(
                    buildingDatabase.ResourceStorages
                )
                .Concat(
                    buildingDatabase.ResourceConverters
                ).ToArray());
        constructionItem.Toggle += _onItemPressed;
        constructionItem.BuildingPressed += _onBuildingPressed;


        var energyItem = GetNode<ToolbarItem>("MarginContainer/Toolbar/Energy");
        energyItem.Init(buildingDatabase.Energy.ToArray());
        energyItem.Toggle += _onItemPressed;
        energyItem.BuildingPressed += _onBuildingPressed;
    }

    private void _onItemPressed(ToolbarItem item)
    {
        _currentOpenItem?.HideSubItems();
        _currentOpenItem = item;
        _currentOpenItem.ShowSubItems();
    }

    private void _onBuildingPressed(object? sender, BuildingResource buildingResource)
    {
        _buildingSystem.StartBuildingPlacement(buildingResource);
    }
}