using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BaseBuilding.scripts.util.common;

public static class NodeUtil
{
    /// <summary>
    /// Returns the closest node to the given global position.
    /// </summary>
    /// <param name="globalPosition"></param>
    /// <param name="nodes"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? FindClosestNode<T>(Vector3 globalPosition, IEnumerable<T> nodes) where T : Node3D
    {
        switch (nodes.Count())
        {
            case 0:
                return null;
            case 1:
                return nodes.First();
            default:
                T? closestNode = null;
                var closestDistance = float.MaxValue;
                foreach (var node in nodes)
                {
                    var distance = globalPosition.DistanceSquaredTo(node.GlobalPosition);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNode = node;
                    }
                }

                return closestNode;
        }
    }

    /// <summary>
    /// Returns all nodes closest the given global position.
    /// If there are multiple nodes with the same distance, all of them will be returned.
    /// </summary>
    /// <param name="globalPosition"></param>
    /// <param name="nodes"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T>? FindClosestNodes<T>(Vector3 globalPosition, IEnumerable<T> nodes) where T : Node3D
    {
        switch (nodes.Count())
        {
            case 0:
                return null;
            case 1:
                return new List<T> { nodes.First() };
            default:
                var closestNodes = new List<T>();
                var closestDistance = float.MaxValue;
                foreach (var node in nodes)
                {
                    var distance = globalPosition.DistanceSquaredTo(node.GlobalPosition);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNodes.Clear();
                        closestNodes.Add(node);
                    }
                    else if (Mathf.IsEqualApprox(distance, closestDistance))
                    {
                        closestNodes.Add(node);
                    }
                }

                return closestNodes;
        }
    }
}