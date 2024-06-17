namespace WorldEngine
{
    public class Chunk<P, T> where T : TerrainData
    {
        public P Position { get; private set; }
        public T TerrainData { get; set;}
        
        public Chunk(P position)
        {
            Position = position;
        }
        
        
    }
}
