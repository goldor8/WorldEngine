

namespace WorldEngine
{
    public abstract class Mesher<P, T> where T : TerrainData
    {
        public abstract MeshData GenerateMesh(Chunk<P, T> chunk);
    }
}