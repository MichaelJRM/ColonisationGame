using Godot;

namespace BaseBuilding.scripts.util.common;

public static class WorldUtil
{
    public static Vector3? GetMousePositionInWorld(Camera3D camera, Vector2 mousePosition, int rayLength = 1000000)
    {
        var origin = camera.ProjectRayOrigin(mousePosition);
        var target = origin + camera.ProjectRayNormal(mousePosition) * rayLength;
        var plane = new Plane(Vector3.Up, 0.0f);
        return plane.IntersectsRay(origin, target);
    }
}