using System.Globalization;
using System.Linq;
using BaseBuilding.scripts.systems.BuildingSystem;
using BaseBuilding.Scripts.Systems.VehicleSystem;
using Godot;

namespace BaseBuilding.Scripts.WorldResources.SolidResourceExtractor;

public partial class SolidResourceExtractor : ResourceExtractor
{
    private VehicleConnector[] _vehicleConnectors = null!;

    public override void _Ready()
    {
        base._Ready();
        var building = GetParent<Building>();
        building.IsPlacementValidCallbacks.Add(_doesHaveRequiredResource);
        _vehicleConnectors = GetChildren().OfType<VehicleConnector>().ToArray();
    }

    private float _onResourceAsked(float amount)
    {
        var amountExtracted = Extract(amount);
        if (amountExtracted <= 0) return 0;

        GetNode<Label3D>("Label3D").Text = ResourceDeposit!.CurrentAmount.ToString(CultureInfo.CurrentCulture);
        return amountExtracted;
    }

    private bool _doesHaveRequiredResource()
    {
        var overlappingAreas = ResourceDetector!.GetOverlappingAreas();
        return overlappingAreas.Any(
            area => area is ResourceDeposit.ResourceDeposit deposit && deposit.Resource.Id == Resource.Id
        );
    }
}