using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityGenerator : MonoBehaviour
{
    const int threadGroupSize = 8;
    public ComputeShader densityCompute;
    
    public void InitTexture(ref RenderTexture rawDensityTexture)
    {
        densityCompute.SetTexture(0, "DensityTexture", rawDensityTexture);
    }

    public virtual void ComputeDensity(int textureSize, float boundsSize)
    {
        densityCompute.SetInt("textureSize", textureSize);
        densityCompute.SetFloat("planetSize", boundsSize);

        ComputeHelper.Dispatch(densityCompute, textureSize, textureSize, textureSize);
    }

}