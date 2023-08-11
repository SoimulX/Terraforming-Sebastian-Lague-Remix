using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    public class Testing : MonoBehaviour
    {
        // public DensityGenerator densityGenerator;
        // public MeshGenerator meshGenerator;


        // private void Start()
        // {
        //     Vector3 coord = Vector3.zero;
        //     float chunkSize = 10;

        //     Create3DTexture(ref densityTexture, 100, "testing");
        //     computeShader.SetTexture(0, "DensityTexture", densityTexture);
        // densityGenerator.ComputeDensity(ref densityTexture, chunkSize);


        // gameObj = new GameObject();
        // MeshFilter filter = gameObj.AddComponent<MeshFilter>();
        // MeshRenderer renderer = gameObj.AddComponent<MeshRenderer>();
        // MeshCollider collider = renderer.gameObject.AddComponent<MeshCollider>();

        // // Sebastian Lague
        // Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

        // meshGenerator.GenerateMesh(ref mesh, coord, ref densityTexture);

        // filter.mesh = mesh;

        // collider.sharedMesh = null;
        // collider.sharedMesh = mesh;

        // gameObj.transform.position = (Vector3)coord * chunkSize;
        // gameObj.transform.localScale = Vector3.one * chunkSize;
        // gameObj.transform.name = $"Chunk ({coord.x}, {coord.y}, {coord.z})";

        // gameObj = GameObject.CreatePrimitive(PrimitiveType.Plane);

        // material.SetTexture("_MainTex", densityTexture);
        // gameObj.GetComponent<Renderer>().material = material;

        // }

        public Material material;
        public ComputeShader computeShader;
        private GameObject gameObj;
        private RenderTexture densityTexture;

        private void Start()
        {
            Create3DTexture(ref densityTexture, 100, "testing");
            computeShader.SetTexture(0, "DensityTexture", densityTexture);

            gameObj = GameObject.CreatePrimitive(PrimitiveType.Plane);

            material.SetTexture("_MainTex", densityTexture);
            gameObj.GetComponent<Renderer>().material = material;
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
    }
}
