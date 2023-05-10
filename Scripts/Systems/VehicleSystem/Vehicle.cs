using Godot;

namespace BaseBuilding.Scripts.Systems.VehicleSystem;

public partial class Vehicle : Node3D
{
    [Export] private float _maxSpeed = 10f;
    [Export] private float _acceleration = 1f;
    [Export] private float _fuelCapacity = 100f;
    [Export] private float _fuelConsumptionRate = 1f;
}