using System.Collections.Generic;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.common;

public static class MeshExtruder
{
    private const float AutoNormalAngleInRadians = 0.523599f;

    public static ArrayMesh Create(Curve3D curve, Vector2[][] meshShape)
    {
        return _createSurface(curve, meshShape);
    }

    private static ArrayMesh _createSurface(Curve3D curve, IReadOnlyList<Vector2[]> lods)
    {
        var rings = curve.GetBakedPoints();
        var upVectors = curve.GetBakedUpVectors();
        var lod0 = _smoothShape(lods[0]);

        var vertices = _calculateVertices(lod0, rings, upVectors);
        var lodIndices = new int[lods.Count][];
        for (var i = 0; i < lods.Count; i++)
        {
            lodIndices[i] = _calculateIndices(lods[i], rings);
        }

        var normals = _calculateNormals(vertices, lodIndices[0]);
        var uvs = _calculateUVs(lod0, rings.Length);
        return _generateSurface(lodIndices, normals, uvs, vertices);
    }

    private static List<Vector2> _smoothShape(IReadOnlyList<Vector2> shape)
    {
        var meshSmoothedVertices = new List<Vector2>(shape.Count);
        for (var i = 0; i < shape.Count; i++)
        {
            var angle = MathUtil.GetAngleBetweenVector2(
                shape[i > 0 ? i - 1 : ^1],
                shape[i],
                shape[i < shape.Count - 1 ? i + 1 : 0]
            );
            meshSmoothedVertices.Add(shape[i]);
            if (angle < AutoNormalAngleInRadians) meshSmoothedVertices.Add(shape[i]);
        }

        return meshSmoothedVertices;
    }


    private static Vector3[] _calculateVertices(
        IReadOnlyList<Vector2> shapeVertices,
        IReadOnlyList<Vector3> rings,
        IReadOnlyList<Vector3> upVectors)
    {
        var shapeLength = shapeVertices.Count;
        var vertices = new Vector3[shapeLength * rings.Count];
        var vertexIndex = 0;
        for (var i = 0; i < rings.Count; i++)
        {
            var forwardVector = i < rings.Count - 1
                ? rings[i].DirectionTo(rings[i + 1])
                : -rings[i].DirectionTo(rings[i - 1]);
            var upVector = upVectors[i];
            var rightVector = forwardVector.Cross(upVector).Normalized();
            for (var j = 0; j < shapeLength; j++)
                vertices[vertexIndex + j] = rings[i] + rightVector * shapeVertices[j].X + upVector * shapeVertices[j].Y;
            vertexIndex += shapeLength;
        }

        return vertices;
    }

    private static int[] _calculateIndices(
        IReadOnlyCollection<Vector2> shapeVertices,
        IReadOnlyCollection<Vector3> rings
    )
    {
        var indices = new int[shapeVertices.Count * (rings.Count - 1) * 6];
        var vertexIndex = 0;
        var triangleIndex = 0;
        var shapeLength = shapeVertices.Count;
        for (var i = 0; i < rings.Count - 1; i++)
        {
            for (var j = 0; j < shapeVertices.Count; j++)
            {
                var y = vertexIndex + j + shapeLength;

                if (j + 1 < shapeLength)
                {
                    var x = vertexIndex + j + 1;
                    indices[triangleIndex] = vertexIndex + j;
                    indices[triangleIndex + 1] = y;
                    indices[triangleIndex + 2] = x;
                    indices[triangleIndex + 3] = x;
                    indices[triangleIndex + 4] = y;
                    indices[triangleIndex + 5] = x + shapeLength;
                }
                else
                {
                    indices[triangleIndex] = vertexIndex + j;
                    indices[triangleIndex + 1] = y;
                    indices[triangleIndex + 2] = vertexIndex;
                    indices[triangleIndex + 3] = vertexIndex;
                    indices[triangleIndex + 4] = y;
                    indices[triangleIndex + 5] = vertexIndex + shapeLength;
                }

                triangleIndex += 6;
            }

            vertexIndex += shapeLength;
        }

        return indices;
    }


    private static Vector3[] _calculateNormals(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> indices)
    {
        var perVertexNormals = new Vector3[vertices.Count];
        for (var i = 0; i < perVertexNormals.Length; i++)
        {
            perVertexNormals[i] = new Vector3();
        }

        var triangleCount = indices.Count / 3;

        for (var i = 0; i < triangleCount; i++)
        {
            var normalTriangleIndex = i * 3;
            var vertexIndexA = indices[normalTriangleIndex];
            var vertexIndexB = indices[normalTriangleIndex + 1];
            var vertexIndexC = indices[normalTriangleIndex + 2];

            var triangleNormal = _calculateFaceNormalFromIndices(vertices, vertexIndexA, vertexIndexB, vertexIndexC);
            perVertexNormals[vertexIndexA] += triangleNormal;
            perVertexNormals[vertexIndexB] += triangleNormal;
            perVertexNormals[vertexIndexC] += triangleNormal;
        }

        for (var i = 0; i < perVertexNormals.Length; i++)
        {
            perVertexNormals[i] = perVertexNormals[i].Normalized();
        }

        return perVertexNormals;
    }


    private static Vector2[] _calculateUVs(IReadOnlyList<Vector2> shapeVertices, int ringCount)
    {
        var shapeLength = shapeVertices.Count;
        var uvs = new Vector2[shapeLength * ringCount];
        var unfoldedMeshVerticesLength = 0.0f;
        var distances = new float[shapeLength];
        distances[0] = 0.0f;

        for (var i = 0; i < shapeLength; i++)
        {
            var distance = shapeVertices[i].DistanceTo(shapeVertices[i < shapeLength - 1 ? i + 1 : 0]);
            distances[i] = distance;
            unfoldedMeshVerticesLength += distance;
        }

        var uvVScale = 1.0f / unfoldedMeshVerticesLength;
        var uv = Vector2.Zero;
        for (var i = 0; i < ringCount; i++)
        {
            var currentDis = 0.0f;
            for (var j = 0; j < shapeLength; j++)
            {
                currentDis += distances[j];
                uv.X = (i & 1) == 1 ? 1.0f : 0.0f;
                uv.Y = currentDis * uvVScale;
                uvs[i + j] = uv;
            }
        }

        return uvs;
    }

    private static Vector3 _calculateFaceNormalFromIndices(
        IReadOnlyList<Vector3> vertices,
        int vertexIndexA,
        int vertexIndexB,
        int vertexIndexC
    )
    {
        var pointA = vertices[vertexIndexA];
        var pointB = vertices[vertexIndexB];
        var pointC = vertices[vertexIndexC];
        var sideAb = pointB - pointA;
        var sideAc = pointC - pointA;
        var normal = sideAc.Cross(sideAb).Normalized();
        return normal;
    }

    private static ArrayMesh _generateSurface(
        IReadOnlyList<int[]> lodIndices,
        Vector3[] normals,
        Vector2[] uvs,
        Vector3[] vertices
    )
    {
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices;
        surfaceArray[(int)Mesh.ArrayType.Index] = lodIndices[0];
        surfaceArray[(int)Mesh.ArrayType.Normal] = normals;
        surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs;

        Godot.Collections.Dictionary? lods = null;
        if (lodIndices.Count > 1)
        {
            lods = new Godot.Collections.Dictionary();
            var lodDistanceInterval = 200 / lodIndices.Count;
            for (var i = 1; i < lodIndices.Count - 1; i++)
            {
                lods.Add(lodDistanceInterval * i, lodIndices[i]);
            }
        }

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(
            Mesh.PrimitiveType.Triangles,
            surfaceArray,
            null,
            lods
        );

        return mesh;
    }
}