using BaseBuilding.scripts.singletons;
using Godot;

namespace BaseBuilding.Scripts.Util.debug;

public partial class DebugNode : Node3D
{
    public override void _Ready()
    {
        var global = GetNode<Global>("/root/Global");
        if (!global.IsDebugModeEnabled)
        {
            QueueFree();
        }
    }
}