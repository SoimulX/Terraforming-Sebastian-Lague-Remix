using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Terrain
{
    // Copied from Sebastian Lague's "Terraforming", files: "GenTest.cs", "VertexData.cs"; edited.
    public class MeshGenerator : MonoBehaviour
    {
        #region Variables

        [SerializeField] ComputeShader meshCompute;
        [SerializeField] float isoLevel = 0f;

        int numPointsPerAxis;
        float chunkSize;

        // Vertex data processing
        ComputeBuffer triangleBuffer;
        ComputeBuffer triCountBuffer;
        VertexData[] vertexDataArray;

        // Mesh processing
        Dictionary<int2, int> vertexIndexMap = new Dictionary<int2, int>();
        List<Vector3> processedVertices = new List<Vector3>();
        List<Vector3> processedNormals = new List<Vector3>();
        List<int> processedTriangles = new List<int>();

        // Stopwatches
        System.Diagnostics.Stopwatch timer_fetchVertexData;
        System.Diagnostics.Stopwatch timer_processVertexData;

        #endregion Variables

        private void Awake()
        {
            numPointsPerAxis = gameObject.GetComponent<ChunkManager>().numPointsPerAxis;
            chunkSize = gameObject.GetComponent<ChunkManager>().chunkSize;

            // Debug.Log("Lasagna");
            CreateBuffers();
        }

        private void Start()
        {

        }

        public void GenerateMesh(ref Mesh mesh, Vector3 coord, ref RenderTexture densityTexture)
        {
            // Create timers:
            timer_fetchVertexData = new System.Diagnostics.Stopwatch();
            timer_processVertexData = new System.Diagnostics.Stopwatch();

            // Marching cubes
            int numVoxelsPerAxis = numPointsPerAxis - 1;
            int marchKernel = 0;

            // Debug.Log(triangleBuffer + "Lasagna sus");

            triangleBuffer.SetCounterValue(0);

            InitialiseMeshCompute(marchKernel, coord, ref densityTexture);
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
                    Debug.Log("next triangle!");
                }
            }

            mesh.Clear();
            mesh.SetVertices(processedVertices);
            mesh.SetTriangles(processedTriangles, 0, true);
            mesh.SetNormals(processedNormals);

            mesh.name = "mesh";
            timer_processVertexData.Stop();
        }

        void InitialiseMeshCompute(int marchKernel, Vector3 coord, ref RenderTexture densityTexture)
        {
            meshCompute.SetBuffer(marchKernel, "triangles", triangleBuffer);
            meshCompute.SetTexture(0, "DensityTexture", densityTexture);
            meshCompute.SetInt("textureSize", densityTexture.width);

            meshCompute.SetInt("numPointsPerAxis", numPointsPerAxis);
            meshCompute.SetFloat("planetSize", chunkSize);
            meshCompute.SetFloat("isoLevel", isoLevel);

            Vector3 chunkCoord = (Vector3)coord * (numPointsPerAxis - 1);
            meshCompute.SetVector("chunkCoord", chunkCoord);
        }

        void CreateBuffers()
        {
            int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            int numVoxelsPerAxis = numPointsPerAxis - 1;
            int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            int maxTriangleCount = numVoxels * 5;
            int maxVertexCount = maxTriangleCount * 3;
            // Debug.Log($"Sussy {maxVertexCount}, {maxTriangleCount}, {numVoxels}, {numVoxelsPerAxis}, {numPoints}");

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
        }

        public struct VertexData
        {
            public Vector3 position;
            public Vector3 normal;
            public int2 id;
        }
    }
}
