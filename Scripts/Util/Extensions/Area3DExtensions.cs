using System.Linq;
using Godot;

namespace BaseBuilding.Scripts.Util.Extensions;

public static class Area3DExtensions
{
    public static void DisableColliders(this Area3D area3D)
    {
        foreach (var collisionShape3D in area3D.GetChildren().OfType<CollisionShape3D>().ToArray())
        {
            collisionShape3D.Disabled = true;
        }
    }

    public static void EnableColliders(this Area3D area3D)
    {
        foreach (var collisionShape3D in area3D.GetChildren().OfType<CollisionShape3D>().ToArray())
        {
            collisionShape3D.Disabled = false;
        }
    }
}