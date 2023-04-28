using System;
using System.Collections.Generic;
using BaseBuilding.scripts.util.common;
using Godot;

namespace BaseBuilding.scripts.common;

public partial class MeshExtruder : RefCounted
{
    private const float AutoNormalAngleInRadians = 0.523599f;
    private int[] _indices = Array.Empty<int>();
    private Vector3[] _normals = Array.Empty<Vector3>();
    private Vector2[] _uvs = Array.Empty<Vector2>();
    private Vector3[] _vertices = Array.Empty<Vector3>();


    public ArrayMesh Create(Curve3D curve, Vector2[] meshVertices)
    {
        return _createMesh(curve, meshVertices);
    }

    private ArrayMesh _createMesh(Curve3D curve, Vector2[] meshVertices)
    {
        var rings = curve.GetBakedPoints();
        var upVectors = curve.GetBakedUpVectors();
        var meshSmoothedVertices = new List<Vector2>();

        for (var i = 0; i < meshVertices.Length; i++)
        {
            var angle = MathUtil.GetAngleBetweenVector2(
                meshVertices[i > 0 ? i - 1 : ^1],
                meshVertices[i],
                meshVertices[i < meshVertices.Length - 1 ? i + 1 : 0]
            );
            meshSmoothedVertices.Add(meshVertices[i]);
            if (angle < AutoNormalAngleInRadians) meshSmoothedVertices.Add(meshVertices[i]);
        }

        _calculateVertices(meshSmoothedVertices, rings, upVectors);
        _calculateNormals();
        _calculateUVs(meshSmoothedVertices, rings.Length);
        return _generateMesh();
    }


    private void _calculateVertices(
        IReadOnlyList<Vector2> meshVertices,
        Vector3[] rings,
        IReadOnlyList<Vector3> upVectors)
    {
        _vertices = new Vector3[meshVertices.Count * rings.Length];
        _indices = new int[meshVertices.Count * rings.Length * 6];
        var vertexIndex = 0;
        var triangleIndex = 0;
        for (var i = 0; i < rings.Length; i++)
        {
            var forwardVector = i < rings.Length - 1
                ? rings[i].DirectionTo(rings[i + 1])
                : -rings[i].DirectionTo(rings[i - 1]);
            var upVector = upVectors[i];
            var rightVector = forwardVector.Cross(upVector).Normalized();

            for (var j = 0; j < meshVertices.Count; j++)
                _vertices[vertexIndex + j] = rings[i] + rightVector * meshVertices[j].X + upVector * meshVertices[j].Y;


            if (i < rings.Length - 1)
                for (var j = 0; j < meshVertices.Count; j++)
                {
                    _indices[triangleIndex] = vertexIndex + j;
                    _indices[triangleIndex + 1] = vertexIndex + j + meshVertices.Count;
                    _indices[triangleIndex + 2] = vertexIndex + (j + 1 < meshVertices.Count ? j + 1 : 0);

                    _indices[triangleIndex + 3] = vertexIndex + (j + 1 < meshVertices.Count ? j + 1 : 0);
                    _indices[triangleIndex + 4] = vertexIndex + j + meshVertices.Count;
                    _indices[triangleIndex + 5] = vertexIndex +
                                                  (j + 1 < meshVertices.Count
                                                      ? j + 1 + meshVertices.Count
                                                      : meshVertices.Count);
                    triangleIndex += 6;
                }

            vertexIndex += meshVertices.Count;
        }
    }


    private void _calculateNormals()
    {
        var perVertexNormals = new List<List<Vector3>>(_vertices.Length);
        for (var i = 0; i < _vertices.Length; i++) perVertexNormals.Add(new List<Vector3>(6));

        _normals = new Vector3[_vertices.Length];
        var triangleCount = _indices.Length / 3;

        for (var i = 0; i < triangleCount; i++)
        {
            var normalTriangleIndex = i * 3;
            var vertexIndexA = _indices[normalTriangleIndex];
            var vertexIndexB = _indices[normalTriangleIndex + 1];
            var vertexIndexC = _indices[normalTriangleIndex + 2];

            var triangleNormal = _calculateFaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            perVertexNormals[vertexIndexA].Add(triangleNormal);
            perVertexNormals[vertexIndexB].Add(triangleNormal);
            perVertexNormals[vertexIndexC].Add(triangleNormal);
        }

        for (var i = 0; i < _vertices.Length; i++)
        {
            for (var j = 0; j < perVertexNormals[i].Count; j++) _normals[i] += perVertexNormals[i][j];
            _normals[i] = _normals[i].Normalized();
        }
    }


    private void _calculateUVs(IReadOnlyList<Vector2> meshVertices, int ringCount)
    {
        _uvs = new Vector2[meshVertices.Count * ringCount];
        var unfoldedMeshVerticesLength = 0.0f;
        var distances = new float[meshVertices.Count];
        distances[0] = 0.0f;

        for (var i = 0; i < meshVertices.Count; i++)
        {
            var distance = meshVertices[i].DistanceTo(meshVertices[i < meshVertices.Count - 1 ? i + 1 : 0]);
            distances[i] = distance;
            unfoldedMeshVerticesLength += distance;
        }

        var uvVScale = 1.0f / unfoldedMeshVerticesLength;
        for (var i = 0; i < ringCount; i++)
        {
            var currentDis = 0.0f;
            for (var j = 0; j < meshVertices.Count; j++)
            {
                currentDis += distances[j];
                _uvs[i + j] = new Vector2(i % 2 == 0 ? 0.0f : 1.0f, currentDis * uvVScale);
            }
        }
    }


    private ArrayMesh _generateMesh()
    {
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);
        surfaceArray[(int)Mesh.ArrayType.Vertex] = _vertices;
        surfaceArray[(int)Mesh.ArrayType.Index] = _indices;
        surfaceArray[(int)Mesh.ArrayType.Normal] = _normals;
        surfaceArray[(int)Mesh.ArrayType.TexUV] = _uvs;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        return mesh;
    }


    private Vector3 _calculateFaceNormalFromIndices(int vertexIndexA, int vertexIndexB, int vertexIndexC)
    {
        var pointA = _vertices[vertexIndexA];
        var pointB = _vertices[vertexIndexB];
        var pointC = _vertices[vertexIndexC];
        var sideAb = pointB - pointA;
        var sideAc = pointC - pointA;
        var normal = sideAc.Cross(sideAb).Normalized();
        return normal;
    }
}