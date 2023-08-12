using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain2
{
    public class DensityGenerator : MonoBehaviour
    {
        const int threadGroupSize = 8;
        public ComputeShader densityCompute;

        public void InitTexture(ref RenderTexture rawDensityTexture)
        {
            densityCompute.SetTexture(0, "DensityTexture", rawDensityTexture);
        }

        public virtual void ComputeDensity(Vector3 offset, int textureSize, float chunkSize)
        {
            float[] offsetArray = {offset.x, offset.y, offset.z};
            densityCompute.SetInt("textureSize", textureSize);
            densityCompute.SetFloats("offset", offsetArray);
            densityCompute.SetFloat("chunkSize", chunkSize);

            ComputeHelper.Dispatch(densityCompute, textureSize, textureSize, textureSize);
        }

    }
}
