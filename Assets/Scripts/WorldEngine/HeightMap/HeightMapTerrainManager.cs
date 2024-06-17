using System;
using System.Collections;
using System.Collections.Generic;
using FastNoiseAdaptater;
using Unity.VisualScripting;
using UnityEngine;

namespace WorldEngine.HeightMap
{
    public class HeightMapTerrainManager : TerrainManager<Vector2Int, HeightMapTerrainData, HeightMapGenerator, HeightMapMesher>
    {
        private static Func<int, Func<Vector2Int, Vector3>> chunkPositionConverter = (chunkSize) => (position) => new Vector3(position.x * chunkSize, 0, position.y * chunkSize);

        private int chunkSize;

        public int ChunkSize
        {
            get => chunkSize;
            set
            {
                chunkSize = value;
                generator.ChunkSize = value;
                mesher.ChunkSize = value;
            }
        }

        private float chunkHeight;

        public float ChunkHeight
        {
            get => chunkHeight;
            set
            {
                chunkHeight = value;
                generator.ChunkHeight = value;
            }
        }

        public HeightMapTerrainManager(FastNoise noise, int chunkSize, float chunkHeight) : base(new HeightMapGenerator(noise, chunkSize, chunkHeight), new HeightMapMesher(chunkSize), chunkPositionConverter(chunkSize))
        {
            ChunkSize = chunkSize;
            ChunkHeight = chunkHeight;
        }
        
        public HeightMapTerrainManager() : base(new HeightMapGenerator(new DomainScale(new FractalFBm(new OpenSimplex2Noise()), 0.001f), 64, 64), new HeightMapMesher(64), chunkPositionConverter(64))
        {
            ChunkSize = 64;
            ChunkHeight = 64;
        }
        
        public void SetNoise(FastNoise noise)
        {
            generator.Noise = noise;
        }
    }
}