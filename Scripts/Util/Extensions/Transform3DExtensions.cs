using System;
using Godot;

namespace BaseBuilding.Scripts.Util.Extensions;

public static class Transform3DExtensions
{
    public static Vector3 ForwardVector(this Transform3D transform)
    {
        return -transform.Basis.Z;
    }

    public static Span<float> GetBuffer(this Transform3D transform)
    {
        var basis = transform.Basis;
        var origin = transform.Origin;
        return new[]
        {
            basis.Row0.X,
            basis.Row0.Y,
            basis.Row0.Z,
            origin.X,
            basis.Row1.X,
            basis.Row1.Y,
            basis.Row1.Z,
            origin.Y,
            basis.Row2.X,
            basis.Row2.Y,
            basis.Row2.Z,
            origin.Z,
        }.AsSpan();
    }
}