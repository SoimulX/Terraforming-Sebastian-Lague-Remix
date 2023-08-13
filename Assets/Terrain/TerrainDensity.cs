using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    public class TerrainDensity : DensityGenerator
    {
        [Header("Noise")]
        public int seed;

        [Range(1, 16)] public int octaves = 4;
        public float lacunarity = 2;
        public float persistence = .5f;

        public float noiseScale = .1f;
        public float noiseHeightMultiplier = .05f;

        public float floorOffset;
        public float floorFactor;

        public float weightMultiplier;

        public float hardFloor;
        public float hardFloorWeight;

        public override void ComputeDensity(Vector3 offset, int textureSize, float chunkSize)
        {
            // Noise parameters

            densityCompute.SetInt("octaves", octaves);
            densityCompute.SetFloat("lacunarity", lacunarity);
            densityCompute.SetFloat("persistence", persistence);

            densityCompute.SetFloat("noiseScale", noiseScale);
            densityCompute.SetFloat("noiseHeightMultiplier", noiseHeightMultiplier);

            densityCompute.SetFloat("floorOffset", floorOffset);
            densityCompute.SetFloat("floorFactor", floorFactor);

            densityCompute.SetFloat("weightMultiplier", weightMultiplier);

            densityCompute.SetFloat("hardFloor", hardFloor);
            densityCompute.SetFloat("hardFloorWeight", hardFloorWeight);

            base.ComputeDensity(offset, textureSize, chunkSize);
        }
    }
}
