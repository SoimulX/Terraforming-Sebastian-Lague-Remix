using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetDensity : DensityGenerator
{
    public float noiseScale;
    public float noiseHeightMultiplier;

    public override void ComputeDensity(int textureSize, float boundsSize)
    {
        densityCompute.SetFloat("noiseScale", noiseScale);
        densityCompute.SetFloat("noiseHeightMultiplier", noiseHeightMultiplier);

        base.ComputeDensity(textureSize, boundsSize);
    }
}
