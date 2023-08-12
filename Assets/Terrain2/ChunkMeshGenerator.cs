using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Terrain2
{
    public class ChunkMeshGenerator : MonoBehaviour
    {
        #region INSPECTOR VARIABLES

        public DensityGenerator densityGenerator;

        [Header("Init Settings")]

        public int numPointsPerAxis = 10;
        public float isoLevel = 0f;

        public bool blurMap;
        public int blurRadius = 3;

        [Header("References")]
        public ComputeShader meshCompute;

        public ComputeShader blurCompute;

        #endregion

        #region NON-INSPECTOR AND PRIVATE VARIABLES
        
        [HideInInspector] public float chunkSize;

        // Private
        ComputeBuffer triangleBuffer;
        ComputeBuffer triCountBuffer;

        VertexData[] vertexDataArray;

        // Stopwatches
        System.Diagnostics.Stopwatch timer_fetchVertexData;
        System.Diagnostics.Stopwatch timer_processVertexData;

        // Mesh processing
        Dictionary<int2, int> vertexIndexMap = new Dictionary<int2, int>();
        List<Vector3> processedVertices = new List<Vector3>();
        List<Vector3> processedNormals = new List<Vector3>();
        List<int> processedTriangles = new List<int>();

        #endregion

        void Awake()
        {
            CreateBuffers();

            // ComputeHelper.CreateRenderTexture3D(ref originalMap, processedDensityTexture);
            // ComputeHelper.CopyRenderTexture3D(processedDensityTexture, originalMap);
        }

        void OnDestroy()
        {
            ReleaseBuffers();
        }

        #region CHUNK MESH GENERATION

        public void GenerateChunk(Chunk chunk)
        {
            Vector3 offset = (Vector3)chunk.id * chunkSize;
            InitTextures(ref chunk.rawDensityTexture, ref chunk.processedDensityTexture, blurMap);
            ComputeDensity(offset, ref densityGenerator, ref chunk.rawDensityTexture, ref chunk.processedDensityTexture, ref blurCompute, blurMap, blurRadius);
            GenerateMesh(ref chunk.mesh, chunk.id, ref chunk.rawDensityTexture, ref chunk.processedDensityTexture);
        }

        void InitTextures(ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture, bool blurMap)
        {
            int size = numPointsPerAxis;
            Create3DTexture(ref rawDensityTexture, size, "Raw Density Texture");
            Create3DTexture(ref processedDensityTexture, size, "Processed Density Texture");

            if (!blurMap)
            {
                processedDensityTexture = rawDensityTexture;
            }
        }

        void ComputeDensity(Vector3 offset, ref DensityGenerator densityGenerator, ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture, ref ComputeShader blurCompute, bool blurMap, int blurRadius)
        {
            densityGenerator.InitTexture(ref rawDensityTexture);

            // Get points (each point is a vector4: xyz = position, w = density)
            int textureSize = rawDensityTexture.width;

            densityGenerator.ComputeDensity(offset, textureSize, chunkSize);
            ProcessDensityMap(ref blurCompute, ref rawDensityTexture, ref processedDensityTexture, blurMap, blurRadius);
        }

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

        void GenerateMesh(ref Mesh mesh, Vector3 coord, ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture)
        {
            // Create timers:
            timer_fetchVertexData = new System.Diagnostics.Stopwatch();
            timer_processVertexData = new System.Diagnostics.Stopwatch();

            // Marching cubes
            int numVoxelsPerAxis = numPointsPerAxis - 1;
            int marchKernel = 0;

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

        void InitialiseMeshCompute(int marchKernel, Vector3 coord, ref RenderTexture rawDensityTexture, ref RenderTexture processedDensityTexture)
        {
            meshCompute.SetBuffer(marchKernel, "triangles", triangleBuffer);
            meshCompute.SetTexture(0, "DensityTexture", blurCompute ? processedDensityTexture : rawDensityTexture);
            meshCompute.SetInt("textureSize", processedDensityTexture.width);

            meshCompute.SetInt("numPointsPerAxis", numPointsPerAxis);

            meshCompute.SetFloat("isoLevel", isoLevel);

            Vector3 chunkCoord = coord * (numPointsPerAxis - 1);
            meshCompute.SetVector("chunkCoord", chunkCoord);
            meshCompute.SetFloat("chunkSize", chunkSize);
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

        #endregion
    }
}
