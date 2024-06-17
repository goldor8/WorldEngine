using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using FastNoiseAdaptater;

namespace WorldEngine.HeightMap
{
    public class HeightMapGenerator : Generator<Vector2Int, HeightMapTerrainData>
    {
        public FastNoise Noise { get; set; }
        public int ChunkSize { get; set; }
        public float ChunkHeight { get; set; }

        public HeightMapGenerator(FastNoise noise, int chunkSize, float chunkHeight)
        {
            this.Noise = noise;
            this.ChunkSize = chunkSize;
            this.ChunkHeight = chunkHeight;
        }
        
        public override void Generate(Chunk<Vector2Int, HeightMapTerrainData> chunk)
        {
            Vector2Int position = chunk.Position;
            HeightMapTerrainData terrainData = new HeightMapTerrainData();
            terrainData.HeightMap = new float[ChunkSize, ChunkSize];
            float[] noiseValues = new float[ChunkSize * ChunkSize];
            
            Noise.GenUniformGrid2D(noiseValues, position.x * ChunkSize, position.y * ChunkSize, ChunkSize, ChunkSize, 0.02f, 1);

            Parallel.For(0, ChunkSize, y =>
            {
                Parallel.For(0, ChunkSize, x =>
                {
                    terrainData.HeightMap[x, y] = noiseValues[y * ChunkSize + x] * ChunkHeight;
                });
            });
            
            chunk.TerrainData = terrainData;
        }
    }
}
