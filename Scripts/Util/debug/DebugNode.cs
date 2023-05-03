using BaseBuilding.scripts.singletons;
using Godot;

namespace BaseBuilding.Scripts.Util.debug;

public partial class DebugNode : Node3D
{
    public override void _Ready()
    {
        if (!Global.Instance.IsDebugModeEnabled)
        {
            QueueFree();
        }
    }
}