using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    public class Chunk
    {
        public Vector3Int ID;
        public RenderTexture densityTexture;

        private GameObject gameObject;

        public Chunk(Vector3Int coord, float chunkSize, int numPointsPerAxis, MeshGenerator meshGenerator, DensityGenerator densityGenerator, Material material, Transform parent = null)
        {
            // TODO
            ID = coord;

            densityGenerator.ComputeDensity(ref densityTexture, chunkSize);

            if (false)
            {
                gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            else
            {
                gameObject = new GameObject();
                MeshFilter filter = gameObject.AddComponent<MeshFilter>();
                MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
                MeshCollider collider = renderer.gameObject.AddComponent<MeshCollider>();

                // Sebastian Lague
                Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

                meshGenerator.GenerateMesh(ref mesh, coord, ref densityTexture);
                Debug.Log(mesh);

                filter.mesh = mesh;

                collider.sharedMesh = null;
                collider.sharedMesh = mesh;
            }

            gameObject.transform.position = (Vector3)coord * chunkSize;
            gameObject.transform.localScale = Vector3.one * chunkSize;
            gameObject.transform.name = $"Chunk ({coord.x}, {coord.y}, {coord.z})";
            gameObject.transform.parent = parent;
            gameObject.GetComponent<MeshRenderer>().material = material;

        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
    }
}

