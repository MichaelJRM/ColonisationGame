using Godot;

namespace BaseBuilding.Scripts.Systems.VehicleSystem;

public partial class CargoVehicle : Vehicle
{
    [Export] private float _cargoCapacity = 1000f;
    private float _currentCargoAmount = 0f;
}