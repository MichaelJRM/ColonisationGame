using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.singletons;

public sealed partial class Global : Node
{
    private Camera3D _camera = null!;
    private Viewport _viewport = null!;
    private Vector3 _lastMousePositionInWorld = Vector3.Zero;
    public double GameTimeInSeconds { get; private set; }
    public bool IsDebugModeEnabled { get; private set; } = true;

    private Global()
    {
    }

    public static Global Instance { get; private set; } = null!;

    public Vector3 GetMousePositionInWorld()
    {
        _lastMousePositionInWorld = WorldUtil.GetMousePositionInWorld(
            _camera, _viewport.GetMousePosition()
        ) ?? _lastMousePositionInWorld;
        return _lastMousePositionInWorld;
    }

    public override void _Ready()
    {
        Instance = this;
        _viewport = GetViewport();
        _camera = _viewport.GetCamera3D();
    }

    public override void _PhysicsProcess(double delta)
    {
        GameTimeInSeconds += delta * 10;
    }
}