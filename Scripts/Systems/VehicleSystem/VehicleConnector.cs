using Godot;

namespace BaseBuilding.Scripts.Systems.VehicleSystem;

public partial class VehicleConnector : Node3D
{
    public Vehicle ConnectedVehicle { get; private set; }
}