using Godot;

namespace BaseBuilding.Scripts.Util.Extensions;

public static class NodeExtensions
{
    public static SignalAwaiter WaitForNextFrame(this Node node)
    {
        return node.ToSignal(node.GetTree(), "process_frame");
    }

    public static SignalAwaiter WaitForNextPhysicsFrame(this Node node)
    {
        return node.ToSignal(node.GetTree(), "physics_frame");
    }
}