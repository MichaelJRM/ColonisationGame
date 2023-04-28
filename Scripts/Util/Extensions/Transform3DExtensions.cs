using Godot;

namespace BaseBuilding.Scripts.Util.Extensions;

public static class Transform3DExtensions
{
    public static Vector3 ForwardVector(this Transform3D transform)
    {
        return -transform.Basis.Z;
    }
}