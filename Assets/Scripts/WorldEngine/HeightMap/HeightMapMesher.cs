

using System.Threading.Tasks;
using UnityEngine;

namespace WorldEngine.HeightMap
{
    public class HeightMapMesher : Mesher<Vector2Int, HeightMapTerrainData>
    {
        public int ChunkSize { get; set; }

        public HeightMapMesher(int chunkSize)
        {
            this.ChunkSize = chunkSize;
        }
        
        public override MeshData GenerateMesh(Chunk<Vector2Int, HeightMapTerrainData> chunk)
        {
            float[,] heightMap = chunk.TerrainData.HeightMap;
            
            Vector3[] vertices = new Vector3[ChunkSize * ChunkSize];
            int[] triangles = new int[(ChunkSize - 1) * (ChunkSize - 1) * 2 * 3];

            Parallel.For(0, ChunkSize * ChunkSize, i =>
            {
                int x = i % ChunkSize;
                int y = i / ChunkSize;
                vertices[i] = new Vector3(x, heightMap[x, y], y);
            });

            Parallel.For(0, triangles.Length / 6, i =>
            {
                if (i % ChunkSize == ChunkSize - 1 || i / ChunkSize == ChunkSize - 1) // edge of the chunk
                    return;
                
                triangles[i * 6] = i;
                triangles[i * 6 + 1] = i + ChunkSize;
                triangles[i * 6 + 2] = i + 1;
                triangles[i * 6 + 3] = i + 1;
                triangles[i * 6 + 4] = i + ChunkSize;
                triangles[i * 6 + 5] = i + ChunkSize + 1;
            });
            
            return new MeshData(vertices, triangles);
        }
    }
}