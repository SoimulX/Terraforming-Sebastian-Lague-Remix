using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain2
{
	public class GenTest : MonoBehaviour
	{
		#region INSPECTOR VARIABLES

		public DensityGenerator densityGenerator;

		[Header("Init Settings")]
		public int numChunks = 5;

		public int numPointsPerAxis = 10;
		public float chunkSize = 10;
		public float isoLevel = 0f;
		public bool useFlatShading;

		public bool blurMap;
		public int blurRadius = 3;

		[Header("References")]
		public ComputeShader meshCompute;

		public ComputeShader blurCompute;
		// public ComputeShader editCompute;
		public Material material;

		#endregion

		private float boundsSize;

		#region PRIVATE VARIABLES

		// Private
		ComputeBuffer triangleBuffer;
		ComputeBuffer triCountBuffer;
		Chunk[] chunks;

		VertexData[] vertexDataArray;

		int totalVerts;

		// Stopwatches
		System.Diagnostics.Stopwatch timer_fetchVertexData;
		System.Diagnostics.Stopwatch timer_processVertexData;
		RenderTexture originalMap;

		// Mesh processing
		Dictionary<int2, int> vertexIndexMap = new Dictionary<int2, int>();
		List<Vector3> processedVertices = new List<Vector3>();
		List<Vector3> processedNormals = new List<Vector3>();
		List<int> processedTriangles = new List<int>();

		#endregion

		void Start()
		{
			boundsSize = numChunks * chunkSize;

			CreateBuffers(); // Good

			CreateChunks(); // Bad

			var sw = System.Diagnostics.Stopwatch.StartNew();
			GenerateAllChunks(boundsSize, ref blurCompute, blurMap, blurRadius); // Bad
			Debug.Log("Generation Time: " + sw.ElapsedMilliseconds + " ms");

			// ComputeHelper.CreateRenderTexture3D(ref originalMap, processedDensityTexture);
			// ComputeHelper.CopyRenderTexture3D(processedDensityTexture, originalMap);

		}

		// TODO: Migtate densityTextures to chunks.
		void InitTextures(ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture, bool blurMap)
		{

			// Explanation of texture size:
			// Each pixel maps to one point.
			// Each chunk has "numPointsPerAxis" points along each axis
			// The last points of each chunk overlap in space with the first points of the next chunk
			// Therefore we need one fewer pixel than points for each added chunk
			int size = numPointsPerAxis;
			Create3DTexture(ref rawDensityTexture, size, "Raw Density Texture");
			Create3DTexture(ref processedDensityTexture, size, "Processed Density Texture");

			if (!blurMap)
			{
				processedDensityTexture = rawDensityTexture;
			}
		}

		void GenerateAllChunks(float boundsSize, ref ComputeShader blurCompute, bool blurMap, int blurRadius)
		{
			// Create timers:
			timer_fetchVertexData = new System.Diagnostics.Stopwatch();
			timer_processVertexData = new System.Diagnostics.Stopwatch();

			totalVerts = 0;

			foreach (var chunk in chunks)
			{
				Vector3 offset = (Vector3)chunk.id * chunkSize;
				Debug.Log("ID " + chunk.id);
				InitTextures(ref chunk.rawDensityTexture, ref chunk.processedDensityTexture, blurMap);  // Good
				ComputeDensity(offset, ref densityGenerator, ref chunk.rawDensityTexture, ref chunk.processedDensityTexture, boundsSize, ref blurCompute, blurMap, blurRadius);
				GenerateMesh(ref chunk.mesh, chunk.id, ref chunk.rawDensityTexture, ref chunk.processedDensityTexture);
			}

			Debug.Log("Total verts " + totalVerts);

			// Print timers:
			Debug.Log("Fetch vertex data: " + timer_fetchVertexData.ElapsedMilliseconds + " ms");
			Debug.Log("Process vertex data: " + timer_processVertexData.ElapsedMilliseconds + " ms");
			Debug.Log("Sum: " + (timer_fetchVertexData.ElapsedMilliseconds + timer_processVertexData.ElapsedMilliseconds));


		}

		// FLEXIBLE
		void ComputeDensity(Vector3 offset, ref DensityGenerator densityGenerator, ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture, float boundsSize, ref ComputeShader blurCompute, bool blurMap, int blurRadius)
		{
			densityGenerator.InitTexture(ref rawDensityTexture);

			// Get points (each point is a vector4: xyz = position, w = density)
			int textureSize = rawDensityTexture.width;

			// TODO: Migrate to chunkSize
			densityGenerator.ComputeDensity(offset, textureSize, boundsSize, chunkSize);
			ProcessDensityMap(ref blurCompute, ref rawDensityTexture, ref processedDensityTexture, blurMap, blurRadius);
		}

		// FLEXIBLE
		void ProcessDensityMap(ref ComputeShader blurCompute, ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture, bool blurMap, int blurRadius)
		{
			if (blurMap)
			{
				blurCompute.SetTexture(0, "Source", rawDensityTexture);
				blurCompute.SetTexture(0, "Result", processedDensityTexture);

				int size = rawDensityTexture.width;
				blurCompute.SetInts("brushCentre", 0, 0, 0);
				blurCompute.SetInt("blurRadius", blurRadius);
				blurCompute.SetInt("textureSize", rawDensityTexture.width);
				ComputeHelper.Dispatch(blurCompute, size, size, size);
			}
		}

		void InitialiseMeshCompute(int marchKernel, Vector3 coord, ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture)
		{
			meshCompute.SetBuffer(marchKernel, "triangles", triangleBuffer);
			meshCompute.SetTexture(0, "DensityTexture", blurCompute ? processedDensityTexture : rawDensityTexture);
			meshCompute.SetInt("textureSize", processedDensityTexture.width);

			meshCompute.SetInt("numPointsPerAxis", numPointsPerAxis);

			// TODO: Migrate to chunkSize
			meshCompute.SetFloat("planetSize", boundsSize);
			meshCompute.SetFloat("isoLevel", isoLevel);

			int lowerRange = Mathf.CeilToInt(-numChunks / 2);

			Vector3 chunkCoord = coord * (numPointsPerAxis - 1) + Vector3.one * lowerRange;
			meshCompute.SetVector("chunkCoord", chunkCoord);
			meshCompute.SetFloat("chunkSize", chunkSize);
		}

		public void GenerateMesh(ref Mesh mesh, Vector3 coord, ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture)
		{
			// Create timers:
			timer_fetchVertexData = new System.Diagnostics.Stopwatch();
			timer_processVertexData = new System.Diagnostics.Stopwatch();

			// Marching cubes
			int numVoxelsPerAxis = numPointsPerAxis - 1;
			int marchKernel = 0;

			// Debug.Log(triangleBuffer + "Lasagna sus");

			triangleBuffer.SetCounterValue(0);

			InitialiseMeshCompute(marchKernel, coord, ref rawDensityTexture, ref processedDensityTexture);
			ComputeHelper.Dispatch(meshCompute, numVoxelsPerAxis, numVoxelsPerAxis, numVoxelsPerAxis, marchKernel);

			// Create mesh
			int[] vertexCountData = new int[1];
			triCountBuffer.SetData(vertexCountData);
			ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);

			timer_fetchVertexData.Start();
			triCountBuffer.GetData(vertexCountData);

			int numVertices = vertexCountData[0] * 3;

			// Debug.Log(triangleBuffer + "THERE2");

			// Debug.Log($"THERE, {numVertices}, {vertexCountData[0]}, {vertexCountData}");
			// Fetch vertex data from GPU
			triangleBuffer.GetData(vertexDataArray, 0, 0, numVertices);

			timer_fetchVertexData.Stop();

			// Process vertex data
			timer_processVertexData.Start();

			// Mesh processing
			vertexIndexMap.Clear();
			processedVertices.Clear();
			processedNormals.Clear();
			processedTriangles.Clear();

			// ? To be explained if understood, please.
			int triangleIndex = 0;
			// Merging and processing verticies.
			for (int i = 0; i < numVertices; i++)
			{
				VertexData data = vertexDataArray[i];

				// Tries to use an already existing vertex.
				if (vertexIndexMap.TryGetValue(data.id, out int sharedVertexIndex))
				{
					processedTriangles.Add(sharedVertexIndex);
				}
				// Adds a new vertex and processes it.
				else
				{
					vertexIndexMap.Add(data.id, triangleIndex);

					processedVertices.Add(data.position);
					processedNormals.Add(data.normal);
					processedTriangles.Add(triangleIndex);

					triangleIndex++;
					// Debug.Log("next triangle!");
				}
			}

			mesh.Clear();
			mesh.SetVertices(processedVertices);
			mesh.SetTriangles(processedTriangles, 0, true);
			mesh.SetNormals(processedNormals);

			mesh.name = "mesh";
			timer_processVertexData.Stop();
		}

		void Update()
		{

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

		void CreateBuffers()
		{
			int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
			int numVoxelsPerAxis = numPointsPerAxis - 1;
			int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
			int maxTriangleCount = numVoxels * 5;
			int maxVertexCount = maxTriangleCount * 3;

			triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
			triangleBuffer = new ComputeBuffer(maxVertexCount, ComputeHelper.GetStride<VertexData>(), ComputeBufferType.Append);
			vertexDataArray = new VertexData[maxVertexCount];
		}

		void ReleaseBuffers()
		{
			ComputeHelper.Release(triangleBuffer, triCountBuffer);
		}

		void OnDestroy()
		{
			ReleaseBuffers();
			foreach (Chunk chunk in chunks)
			{
				chunk.Release();
			}
		}

		// TODO: Convert from [0; numChunks) to [-numChunks/2; +numChunks/2]
		// WORKING ON
		void CreateChunks()
		{
			chunks = new Chunk[numChunks * numChunks * numChunks];
			int i = 0;

			int lowerRange = Mathf.CeilToInt(-numChunks / 2);
			int upperRange = Mathf.FloorToInt(+numChunks / 2) + 1; // CeilToInt doesn't work.

			Debug.Log($"LowerRange {lowerRange}, UpperRange {upperRange}");
			// int lowerRange = -2;
			// int upperRange = 3;

			for (int y = lowerRange; y < upperRange; y++)
			{
				for (int x = lowerRange; x < upperRange; x++)
				{
					for (int z = lowerRange; z < upperRange; z++)
					{
						Vector3Int coord = new Vector3Int(x, y, z);
						float posX = (-(numChunks - 1f) / 2 + x) * chunkSize;
						float posY = (-(numChunks - 1f) / 2 + y) * chunkSize;
						float posZ = (-(numChunks - 1f) / 2 + z) * chunkSize;
						Vector3 centre = new Vector3(posX, posY, posZ);

						GameObject meshHolder = new GameObject($"Chunk ({x}, {y}, {z})");
						meshHolder.transform.parent = transform;
						meshHolder.layer = gameObject.layer;

						Chunk chunk = new Chunk(coord, centre, chunkSize, numPointsPerAxis, meshHolder);
						chunk.SetMaterial(material);
						chunks[i] = chunk;
						i++;
					}
				}
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
