using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Terrain
{
	public class Chunk
	{

		public float size;
		public Mesh mesh;

		public ComputeBuffer pointsBuffer;
		int numPointsPerAxis;
		public MeshFilter filter;
		MeshRenderer renderer;
		MeshCollider collider;
		public bool terra;
		public Vector3Int id;
		public GameObject gameObject;

		[HideInInspector] public RenderTexture rawDensityTexture;
		[HideInInspector] public RenderTexture processedDensityTexture;

		public Chunk(Vector3Int coord, float size, ChunkMeshGenerator chunkMeshGenerator, Material material, Transform parent = null)
		{
			this.id = coord;
			this.size = size;
			// this.numPointsPerAxis = numPointsPerAxis;

			mesh = new Mesh();
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

			// int numPointsTotal = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
			// ComputeHelper.CreateStructuredBuffer<PointData>(ref pointsBuffer, numPointsTotal);

			// Mesh rendering and collision components
			GameObject meshHolder = new GameObject($"Chunk ({coord.x}, {coord.y}, {coord.z})");
			meshHolder.transform.parent = parent;
			// meshHolder.layer = gameObject.layer;
			filter = meshHolder.AddComponent<MeshFilter>();
			renderer = meshHolder.AddComponent<MeshRenderer>();

			filter.mesh = mesh;
			collider = renderer.gameObject.AddComponent<MeshCollider>();

			meshHolder.transform.position = (Vector3)coord * size;

			chunkMeshGenerator.GenerateChunk(this);

			gameObject = meshHolder;


			SetMaterial(material);
		}

		public struct PointData
		{
			public Vector3 position;
			public Vector3 normal;
			public float density;
		}

		public void AddCollider()
		{
			collider.sharedMesh = mesh;
		}

		public void SetMaterial(Material material)
		{
			renderer.material = material;
		}

		public void Release()
		{
			ComputeHelper.Release(pointsBuffer);
		}

		// TODO: Fix an implement this function.
		// public void DrawBoundsGizmo(Color col)
		// {
		// 	Gizmos.color = col;
		// 	Gizmos.DrawWireCube(centre, Vector3.one * size);
		// }

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
