using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Terrain
{
    public class ChunkManager : MonoBehaviour
    {
        #region INSPECTOR VARIABLES

        [SerializeField] Transform viewer;
        [SerializeField] Material material;
        [SerializeField] Transform chunkContainer;

        [SerializeField][Range(8, 32)] public int numPointsPerAxis = 10;
        [SerializeField][Range(10, 100)] public float chunkSize = 10;

        [SerializeField][Range(0, 200)] ushort renderDistance;
        [SerializeField][Range(1, 100)] float distanceThresholdForChunkUpdate;

        #endregion

        #region PRIVATE VARIABLES

        Dictionary<Vector3Int, Chunk> existingChunks = new Dictionary<Vector3Int, Chunk>();

        List<Vector3Int> lastVisibleChunksCoords = new List<Vector3Int>();
        Vector3 lastPositionForChunkUpdate;

        MeshGenerator meshGenerator;
        DensityGenerator densityGenerator;

        #endregion

        private void Awake()
        {
            meshGenerator = gameObject.GetComponent<MeshGenerator>();
            if(meshGenerator == null) Debug.LogError("meshGenerator == null");
            else Debug.Log("ok1");
            densityGenerator = gameObject.GetComponent<DensityGenerator>();
            if(densityGenerator == null) Debug.LogError("densityGenerator == null");
            else Debug.Log($"ok2 {densityGenerator.blurRadius}");
        }

        private void Start()
        {
            UpdateChunks();
            lastPositionForChunkUpdate = viewer.position;

            foreach (var coord in lastVisibleChunksCoords)
            {
                Debug.Log("w" + coord);
            }

        }

        private void Update()
        {
            if ((viewer.position - lastPositionForChunkUpdate).sqrMagnitude >= distanceThresholdForChunkUpdate * distanceThresholdForChunkUpdate)
            {
                UpdateChunks();
            }
        }

        void UpdateChunks()
        {
            List<Vector3Int> visibleChunksCoords = GetVisibleChunksCoords();
            foreach (var coord in visibleChunksCoords)
            {
                Debug.Log(coord);

                // If already in view, continue.
                if (lastVisibleChunksCoords.Contains(coord)) continue;

                // If not generated, generate.
                if (!existingChunks.ContainsKey(coord))
                {
                    existingChunks.Add(coord, GenerateChunk(coord));
                }
                // If hidden, show.
                else
                {
                    existingChunks[coord].Show();
                }
            }

            // Hide the chunks that went out of view.
            var notVisibleChunksCoords = lastVisibleChunksCoords.Except(visibleChunksCoords);
            foreach (var coord in notVisibleChunksCoords)
            {
                existingChunks[coord].Hide();
            }

            // Update the visibleChunksCoords.
            lastVisibleChunksCoords = visibleChunksCoords;
        }

        Chunk GenerateChunk(Vector3Int coord)
        {
            return new Chunk(coord, chunkSize, numPointsPerAxis, meshGenerator, densityGenerator, material, chunkContainer);
        }

        List<Vector3Int> GetVisibleChunksCoords()
        {
            // TODO
            List<Vector3Int> visibleChunksCoordsInRadius = GetChunkIDsInRadius(viewer.position, renderDistance);
            List<Vector3Int> visibleChunksCoords = new();

            foreach (var coord in visibleChunksCoordsInRadius)
            {
                // Copied from Sebastian Lague's "Marching Cubes" file "MeshGenerator.cs" and edited.
                Bounds bounds = new Bounds(ChunkPosition(coord), Vector3.one * chunkSize);
                if (IsVisibleFrom(bounds, Camera.main))
                {
                    visibleChunksCoords.Add(coord);
                }
                // 
            }
            return visibleChunksCoords;
        }

        // Copied from old project
        List<Vector3Int> GetChunkIDsInRadius(Vector3 position, float radius)
        {
            List<Vector3Int> chunkIDs = new List<Vector3Int>();

            // The position on a chunkSize unit of measurement.
            Vector3 chunkRelativePosition = position / chunkSize;

            // The position of the current chunk on a chunkSize unit of measurement.
            Vector3Int currentChunkPosition = Vector3Int.FloorToInt(chunkRelativePosition);

            // The integer-approximation of the radius on a chunkSize scale.
            int chunkRadius = Mathf.CeilToInt(radius / chunkSize);

            for (int x = currentChunkPosition.x - chunkRadius; x <= currentChunkPosition.x + chunkRadius; x++)
            {
                for (int y = currentChunkPosition.y - chunkRadius; y <= currentChunkPosition.y + chunkRadius; y++)
                {
                    for (int z = currentChunkPosition.z - chunkRadius; z <= currentChunkPosition.z + chunkRadius; z++)
                    {
                        Vector3Int chunkID = new Vector3Int(x, y, z);

                        // Foreach chunkID in the exterior cube of the sphere:

                        // If the distance to the chunk is smaller than `radius`
                        if (((Vector3)chunkID * chunkSize - position).sqrMagnitude < radius * radius)
                        {
                            // Foreach chunkID in the sphere at position: position, radius:radius: 
                            chunkIDs.Add(chunkID);
                        }
                    }
                }
            }

            return chunkIDs;
        }

        Vector3 ChunkPosition(Vector3Int coord)
        {
            return (Vector3)coord * chunkSize;
        }

        // Copied from Sebastian Lague's "Marching Cubes" file "MeshGenerator.cs", not edited.
        bool IsVisibleFrom(Bounds bounds, Camera camera)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }
    }
}
