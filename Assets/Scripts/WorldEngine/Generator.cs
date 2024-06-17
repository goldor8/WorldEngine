namespace WorldEngine
{
    public abstract class Generator<P, T> where T : TerrainData
    {
        public abstract void Generate(Chunk<P, T> chunk);
    }
}