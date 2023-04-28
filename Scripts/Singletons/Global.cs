using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.singletons;

public partial class Global : Node
{
    private Camera3D _camera = null!;
    private Viewport _viewport = null!;
    public Vector3 MousePositionInWorld { get; private set; } = new(0.0f, 0.0f, 0.0f);
    public double GameTimeInSeconds { get; private set; }

    public override void _Ready()
    {
        _viewport = GetViewport();
        _camera = _viewport.GetCamera3D();
    }

    public override void _Process(double delta)
    {
        _updateMousePositionInWorld();
    }

    public override void _PhysicsProcess(double delta)
    {
        GameTimeInSeconds += delta * 10;
    }

    private void _updateMousePositionInWorld()
    {
        MousePositionInWorld = WorldUtil.GetMousePositionInWorld(
            _camera, _viewport.GetMousePosition()
        ) ?? MousePositionInWorld;
    }
}