using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;


namespace Terrain
{
	// TODO: Fix the gap in the mesh generation when using blur.

	public class ChunkManager : MonoBehaviour
	{
		#region INSPECTOR VARIABLES

		[SerializeField] Transform viewer;
		[SerializeField][Range(10, 2000)] ushort renderDistance;
		[SerializeField][Range(1, 100)] float distanceThresholdForChunkUpdate;

		[SerializeField] Transform chunkContainer;

		[Header("Init Settings")]

		public float chunkSize = 10;
		public bool useFlatShading;

		[Header("References")]

		public Material material;

		#endregion

		#region PRIVATE VARIABLES

		ChunkMeshGenerator chunkMeshGenerator;

		Dictionary<Vector3Int, Chunk> existingChunks = new Dictionary<Vector3Int, Chunk>();

		List<Vector3Int> lastVisibleChunksCoords = new List<Vector3Int>();
		Vector3 lastPositionForChunkUpdate;

		#endregion

		void Start()
		{
			chunkMeshGenerator = gameObject.GetComponent<ChunkMeshGenerator>();
			chunkMeshGenerator.chunkSize = chunkSize;
			UpdateChunks();
			lastPositionForChunkUpdate = viewer.position;

			// ComputeHelper.CreateRenderTexture3D(ref originalMap, processedDensityTexture);
			// ComputeHelper.CopyRenderTexture3D(processedDensityTexture, originalMap);
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

		void Update()
		{
			if ((viewer.position - lastPositionForChunkUpdate).sqrMagnitude >= distanceThresholdForChunkUpdate * distanceThresholdForChunkUpdate)
			{
				UpdateChunks();
			}

			// TODO: move somewhere more sensible
			// material.SetTexture("DensityTex", originalMap);
			// // material.SetFloat("oceanRadius", FindObjectOfType<Water>().radius);
			// material.SetFloat("planetBoundsSize", boundsSize);

			/*
			if (Input.GetKeyDown(KeyCode.G))
			{
				Debug.Log("Generate");
				GenerateAllChunks();
			}
			*/
		}

		void OnDestroy()
		{
			foreach (Chunk chunk in existingChunks.Values)
			{
				chunk.Release();
			}
		}

		// TODO: Fix and implement this function.
		/*
		public void Terraform(Vector3 point, float weight, float radius)
		{

			int editTextureSize = rawDensityTexture.width;
			float editPixelWorldSize = boundsSize / editTextureSize;
			int editRadius = Mathf.CeilToInt(radius / editPixelWorldSize);
			//Debug.Log(editPixelWorldSize + "  " + editRadius);

			float tx = Mathf.Clamp01((point.x + boundsSize / 2) / boundsSize);
			float ty = Mathf.Clamp01((point.y + boundsSize / 2) / boundsSize);
			float tz = Mathf.Clamp01((point.z + boundsSize / 2) / boundsSize);

			int editX = Mathf.RoundToInt(tx * (editTextureSize - 1));
			int editY = Mathf.RoundToInt(ty * (editTextureSize - 1));
			int editZ = Mathf.RoundToInt(tz * (editTextureSize - 1));

			editCompute.SetFloat("weight", weight);
			editCompute.SetFloat("deltaTime", Time.deltaTime);
			editCompute.SetInts("brushCentre", editX, editY, editZ);
			editCompute.SetInt("brushRadius", editRadius);

			editCompute.SetInt("size", editTextureSize);
			ComputeHelper.Dispatch(editCompute, editTextureSize, editTextureSize, editTextureSize);

			//ProcessDensityMap();
			int size = rawDensityTexture.width;

			if (blurMap)
			{
				blurCompute.SetInt("textureSize", rawDensityTexture.width);
				blurCompute.SetInts("brushCentre", editX - blurRadius - editRadius, editY - blurRadius - editRadius, editZ - blurRadius - editRadius);
				blurCompute.SetInt("blurRadius", blurRadius);
				blurCompute.SetInt("brushRadius", editRadius);
				int k = (editRadius + blurRadius) * 2;
				ComputeHelper.Dispatch(blurCompute, k, k, k);
			}

			//ComputeHelper.CopyRenderTexture3D(originalMap, processedDensityTexture);

			float worldRadius = (editRadius + 1 + ((blurMap) ? blurRadius : 0)) * editPixelWorldSize;
			for (int i = 0; i < chunks.Length; i++)
			{
				Chunk chunk = chunks[i];
				if (MathUtility.SphereIntersectsBox(point, worldRadius, chunk.centre, Vector3.one * chunk.size))
				{

					chunk.terra = true;
					GenerateChunk(chunk);

				}
			}
		}
		*/

		#region CHUNK MANAGEMENT

		Chunk GenerateChunk(Vector3Int coord)
		{
			return new Chunk(coord, chunkSize, chunkMeshGenerator, material, chunkContainer);
		}

		List<Vector3Int> GetVisibleChunksCoords()
		{
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

		#endregion
	}
}
