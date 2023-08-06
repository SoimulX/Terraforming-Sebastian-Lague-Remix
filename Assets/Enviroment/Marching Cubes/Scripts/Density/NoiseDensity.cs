using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseDensity : DensityGenerator
{
    [Header("Noise")]
    public int seed;

    [Range(1, 16)] public int octaves = 4;
    public float lacunarity = 2;
    public float persistence = .5f;

    public float noiseScale = .1f;
    public float noiseHeightMultiplier = .05f;

    public override void ComputeDensity(int textureSize, float boundsSize)
    {
        // Noise parameters
        
        densityCompute.SetFloat("noiseScale", noiseScale);
        densityCompute.SetFloat("noiseHeightMultiplier", noiseHeightMultiplier);

        densityCompute.SetInt("octaves", octaves);
        densityCompute.SetFloat("lacunarity", lacunarity);
        densityCompute.SetFloat("persistence", persistence);

        base.ComputeDensity(textureSize, boundsSize);
    }
}