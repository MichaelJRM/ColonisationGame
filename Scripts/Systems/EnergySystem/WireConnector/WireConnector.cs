using Godot;

namespace BaseBuilding.Scripts.Systems.EnergySystem.WireConnector;

public partial class WireConnector : Area3D
{
    public void Activate()
    {
        Monitorable = true;
    }

    public override void _Ready()
    {
        Monitorable = false;
    }
}