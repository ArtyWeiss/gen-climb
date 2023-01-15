using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;

public class OldChunk : MonoBehaviour
{
    public MeshFilter meshFilter;
    public OldDensityGenerator oldDensity;

    [Space(20)] [Range(0f, 1f)] public float isoLevel;
    public float boundsSize;
    public int numPointsPerAxis;

    [Space] public bool update;

    private float4[] points;
    private readonly List<float3[]> triangles = new();

    private void Start()
    {
        UpdateChunkMesh();
    }

    private void Update()
    {
        if (update)
        {
            UpdateChunkMesh();
        }
    }

    private void UpdateChunkMesh()
    {
        Profiler.BeginSample("Points init");
        GeneratePoints();
        Profiler.EndSample();
        
        Profiler.BeginSample("Density");
        oldDensity.GenerateDensity(ref points);
        Profiler.EndSample();
        
        Profiler.BeginSample("Mesh");
        GenerateMesh();
        Profiler.EndSample();
    }

    private void GeneratePoints()
    {
        points = new float4[numPointsPerAxis * numPointsPerAxis * numPointsPerAxis];
        var spacing = boundsSize / (numPointsPerAxis - 1f);
        for (var z = 0; z < numPointsPerAxis; z++)
        {
            for (var y = 0; y < numPointsPerAxis; y++)
            {
                for (var x = 0; x < numPointsPerAxis; x++)
                {
                    var id = int3(x, y, z);
                    var position = (float3) id * spacing - boundsSize / 2f;
                    points[indexFromCoord(x, y, z)] = float4(position, 0f);
                }
            }
        }
    }

    private void GenerateMesh()
    {
        triangles.Clear();
        var numVoxelsPerAxis = numPointsPerAxis - 1;
        for (var z = 0; z < numVoxelsPerAxis; z++)
        {
            for (var y = 0; y < numVoxelsPerAxis; y++)
            {
                for (var x = 0; x < numVoxelsPerAxis; x++)
                {
                    GenerateVoxelTriangles(int3(x, y, z));
                }
            }
        }

        // Construct mesh
        var mesh = new Mesh {indexFormat = IndexFormat.UInt32};
        var verts = new Vector3[triangles.Count * 3];
        var tris = new int[triangles.Count * 3];

        for (var i = 0; i < triangles.Count; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                tris[i * 3 + j] = i * 3 + j;
                verts[i * 3 + j] = triangles[i][j];
            }
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
    }

    private void GenerateVoxelTriangles(int3 id)
    {
        var cubeCorners = new[]
        {
            points[indexFromCoord(id.x, id.y, id.z)],
            points[indexFromCoord(id.x + 1, id.y, id.z)],
            points[indexFromCoord(id.x + 1, id.y, id.z + 1)],
            points[indexFromCoord(id.x, id.y, id.z + 1)],
            points[indexFromCoord(id.x, id.y + 1, id.z)],
            points[indexFromCoord(id.x + 1, id.y + 1, id.z)],
            points[indexFromCoord(id.x + 1, id.y + 1, id.z + 1)],
            points[indexFromCoord(id.x, id.y + 1, id.z + 1)]
        };

        var cubeIndex = 0;
        for (var i = 0; i < 8; i++)
        {
            if (cubeCorners[i].w > isoLevel)
            {
                cubeIndex |= 1 << i;
            }
        }

        var triangulation = TriangulationTable.triTable[cubeIndex];
        for (var i = 0; triangulation[i] != -1; i += 3)
        {
            var a0 = TriangulationTable.cornerIndexAFromEdge[triangulation[i]];
            var b0 = TriangulationTable.cornerIndexBFromEdge[triangulation[i]];

            var a1 = TriangulationTable.cornerIndexAFromEdge[triangulation[i + 1]];
            var b1 = TriangulationTable.cornerIndexBFromEdge[triangulation[i + 1]];

            var a2 = TriangulationTable.cornerIndexAFromEdge[triangulation[i + 2]];
            var b2 = TriangulationTable.cornerIndexBFromEdge[triangulation[i + 2]];

            var triangle = new float3[3];
            triangle[0] = interpolateVerts(cubeCorners[a0], cubeCorners[b0]);
            triangle[1] = interpolateVerts(cubeCorners[a1], cubeCorners[b1]);
            triangle[2] = interpolateVerts(cubeCorners[a2], cubeCorners[b2]);

            triangles.Add(triangle);
        }
    }

    private float3 interpolateVerts(float4 p1, float4 p2)
    {
        var t = (isoLevel - p1.w) / (p2.w - p1.w);
        return p1.xyz + t * (p2.xyz - p1.xyz);
    }

    private int indexFromCoord(int x, int y, int z)
    {
        return z * numPointsPerAxis * numPointsPerAxis + y * numPointsPerAxis + x;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * boundsSize);
        // if (points != null)
        // {
        //     foreach (var point in points)
        //     {
        //         if (point.w < isoLevel)
        //         {
        //             Gizmos.color = new Color(point.w, point.w, point.w);
        //             Gizmos.DrawSphere(point.xyz, pointSize);
        //         }
        //     }
        // }
    }
}