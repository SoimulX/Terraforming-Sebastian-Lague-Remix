using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    // Copied from Sebastian Lague's "Terraforming", files: "GenTest.cs", "DensityGenerator.cs"; edited.
    public class DensityGenerator : MonoBehaviour
    {
        #region My W/ Sebastian Lague

        // --- Should be here ---
        public bool blurMap;
        public int blurRadius = 3;

        public ComputeShader densityCompute;
        public ComputeShader blurCompute;

        // --- Shouldn't be here ---
        public int numChunks = 1;
        public int numPointsPerAxis = 10;

        // LEGACY
        RenderTexture processedDensityTexture = null;

        public virtual void ComputeDensity(ref RenderTexture densityTexture, float chunkSize)
        {
            // Create the texture if it isn't created properly already.
            // TODO: Determine the correct size for the texture.
            int size = numChunks * (numPointsPerAxis - 1) + 1;
            Create3DTexture(ref densityTexture, size, "Density Texture");
            Create3DTexture(ref processedDensityTexture, size, "Processed Density Texture");
            
            int textureSize = densityTexture.width;

            // Compute density
            // Set textures and variables on compute shaders
            densityCompute.SetTexture(0, "DensityTexture", densityTexture);
            densityCompute.SetInt("textureSize", textureSize);
            densityCompute.SetFloat("chunkSize", chunkSize);

            // Get points (each point is a vector4: xyz = position, w = density)
            ComputeHelper.Dispatch(densityCompute, textureSize, textureSize, textureSize);

            // ProcessDensityMap
            if (blurMap)
            {
                // blurCompute.SetTexture(0, "Texture", densityTexture);
                blurCompute.SetTexture(0, "Source", densityTexture);
                blurCompute.SetTexture(0, "Result", processedDensityTexture);

                blurCompute.SetInts("brushCentre", 0, 0, 0);
                blurCompute.SetInt("blurRadius", blurRadius);
                blurCompute.SetInt("textureSize", textureSize);
                ComputeHelper.Dispatch(blurCompute, textureSize, textureSize, textureSize);
            }

            densityTexture = processedDensityTexture;
        }

        void Create3DTexture(ref RenderTexture texture, int size, string name)
        {
            //
            var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
            if (texture == null || !texture.IsCreated() || texture.width != size || texture.height != size || texture.volumeDepth != size || texture.graphicsFormat != format)
            {
                //Debug.Log ("Create tex: update noise: " + updateNoise);
                if (texture != null)
                {
                    texture.Release();
                }
                const int numBitsInDepthBuffer = 0;
                texture = new RenderTexture(size, size, numBitsInDepthBuffer);
                texture.graphicsFormat = format;
                texture.volumeDepth = size;
                texture.enableRandomWrite = true;
                texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;


                texture.Create();
            }
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.name = name;
        }

        #endregion
    }
}
