using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Terrain2
{
	public class Chunk
	{

		public Vector3 centre;
		public float size;
		public Mesh mesh;

		public ComputeBuffer pointsBuffer;
		int numPointsPerAxis;
		public MeshFilter filter;
		MeshRenderer renderer;
		MeshCollider collider;
		public bool terra;
		public Vector3Int id;

		[HideInInspector] public RenderTexture rawDensityTexture;
		[HideInInspector] public RenderTexture processedDensityTexture;

		public Chunk(Vector3Int coord, Vector3 centre, float size, int numPointsPerAxis, GameObject meshHolder)
		{
			this.id = coord;
			this.centre = centre;
			this.size = size;
			this.numPointsPerAxis = numPointsPerAxis;

			mesh = new Mesh();
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

			int numPointsTotal = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
			ComputeHelper.CreateStructuredBuffer<PointData>(ref pointsBuffer, numPointsTotal);

			// Mesh rendering and collision components
			filter = meshHolder.AddComponent<MeshFilter>();
			renderer = meshHolder.AddComponent<MeshRenderer>();


			filter.mesh = mesh;
			collider = renderer.gameObject.AddComponent<MeshCollider>();

			meshHolder.transform.position = (Vector3)coord * size;
		}

		/*
		public void CreateMesh(VertexData[] vertexData, int numVertices, bool useFlatShading)
		{

			vertexIndexMap.Clear();
			processedVertices.Clear();
			processedNormals.Clear();
			processedTriangles.Clear();

			int triangleIndex = 0;

			for (int i = 0; i < numVertices; i++)
			{
				VertexData data = vertexData[i];

				if (!useFlatShading && vertexIndexMap.TryGetValue(data.id, out int sharedVertexIndex))
				{
					processedTriangles.Add(sharedVertexIndex);
				}
				else
				{
					if (!useFlatShading)
					{
						vertexIndexMap.Add(data.id, triangleIndex);
					}
					processedVertices.Add(data.position);
					processedNormals.Add(data.normal);
					processedTriangles.Add(triangleIndex);
					triangleIndex++;
				}
			}

			collider.sharedMesh = null;

			mesh.Clear();
			mesh.SetVertices(processedVertices);
			mesh.SetTriangles(processedTriangles, 0, true);

			if (useFlatShading)
			{
				mesh.RecalculateNormals();
			}
			else
			{
				mesh.SetNormals(processedNormals);
			}

			collider.sharedMesh = mesh;
		}
		*/

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

		public void DrawBoundsGizmo(Color col)
		{
			Gizmos.color = col;
			Gizmos.DrawWireCube(centre, Vector3.one * size);
		}
	}
}
