using System;
using Unity.Mathematics;
using UnityEngine;

public class OldDensityGenerator : MonoBehaviour
{
    public NoiseLayer[] layers;
    private readonly Noise noise = new();

    [Serializable]
    public struct NoiseLayer
    {
        [Space] [Range(0.01f, 2f)] public float scale;
        public float3 offset;
        [Range(-2f, 2f)] public float strength;
    }

    public void GenerateDensity(ref float4[] points)
    {
        if (layers == null) return;

        for (var i = 0; i < points.Length; i++)
        {
            var resultVal = 0f;
            foreach (var layer in layers)
            {
                var posWS = (float3)transform.TransformPoint(points[i].xyz);
                resultVal += noise.Evaluate(posWS * layer.scale + layer.offset) * layer.strength;
            }

            points[i].w = -points[i].y + resultVal;
        }
    }
}