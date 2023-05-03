using BaseBuilding.Scripts.Util.Extensions;
using Godot;

namespace BaseBuilding.scripts.util.common;

public static class MathUtil
{
    public static Vector3 GetParallelPosition(Transform3D targetTransform, Vector3 sourcePosition)
    {
        var direction = sourcePosition.DirectionTo(targetTransform.Origin);
        var parallelVector = direction.Project(-targetTransform.Basis.Z);
        var parallelPosition = targetTransform.Origin + parallelVector;
        return parallelPosition;
    }

    public static Vector3 CalculateIntersectionPoint(Transform3D transformA, Transform3D transformB)
    {
        var lineVec3 = transformB.Origin - transformA.Origin;
        var crossVec1And2 = transformA.ForwardVector().Cross(transformB.ForwardVector());
        if (crossVec1And2.LengthSquared() < 0.0001f) return Vector3.Zero;
        var crossVec3And2 = lineVec3.Cross(transformB.ForwardVector());
        var s = crossVec3And2.Dot(crossVec1And2) / crossVec1And2.LengthSquared();
        return transformA.Origin + transformA.ForwardVector() * s;
    }

    public static float GetAngleBetweenVector2(in Vector2 a, in Vector2 b, in Vector2 c)
    {
        var ba = (a - b).Normalized();
        var bc = (c - b).Normalized();
        var dotProduct = ba.Dot(bc);
        var crossProduct = ba.Cross(bc);
        var angle = Mathf.Atan2(crossProduct, dotProduct);
        return angle;
    }

    public static Transform3D ToLocal(Transform3D parentGlobalTransform, Transform3D childGlobalTransform)
    {
        return parentGlobalTransform.AffineInverse() * childGlobalTransform;
    }

    public static Transform3D ToGlobal(Transform3D parentGlobalTransform, Transform3D childGlobalTransform)
    {
        return parentGlobalTransform * childGlobalTransform;
    }
}