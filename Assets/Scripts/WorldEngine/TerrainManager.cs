using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WorldEngine
{
    public abstract class TerrainManager<P, T, G, M> where T : TerrainData where G : Generator<P, T> where M : Mesher<P, T>
    {
        protected G generator;
        protected M mesher;
        private Func<P, Vector3> chunkPositionConverter;
        
        protected Dictionary<P, Chunk<P, T>> chunkMap = new Dictionary<P, Chunk<P, T>>();
        private Dictionary<P, MeshFilter> meshMap = new Dictionary<P, MeshFilter>();
        
        public TerrainManager(G generator, M mesher, Func<P, Vector3> chunkPositionConverter)
        {
            this.generator = generator;
            this.mesher = mesher;
            this.chunkPositionConverter = chunkPositionConverter;
        }
        
        public void GenerateChunk(P position)
        {
            Chunk<P, T> chunk = new Chunk<P, T>(position);
            generator.Generate(chunk);
            if(chunkMap.ContainsKey(position))
                chunkMap[position] = chunk;
            else
                chunkMap.Add(position, chunk);
        }
        
        public void GenerateMesh(P position)
        {
            if (!chunkMap.ContainsKey(position))
                return;

            Chunk<P, T> chunk = chunkMap[position];
            MeshData meshData = mesher.GenerateMesh(chunk);
            Mesh mesh = new Mesh();
            mesh.vertices = meshData.vertices;
            mesh.triangles = meshData.triangles;
            mesh.RecalculateNormals();
            
            if (meshMap.ContainsKey(position))
            {
                meshMap[position].mesh.Clear();
                meshMap[position].mesh = mesh;
            }
            else
            {
                GameObject meshObject = new GameObject("Mesh");
                meshObject.transform.position = chunkPositionConverter(position);
                meshObject.AddComponent<MeshFilter>().mesh = mesh;
                meshObject.AddComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
                meshMap.Add(position, meshObject.GetComponent<MeshFilter>());
            }
        }
        
        public Chunk<P, T> GetChunk(P position)
        {
            return chunkMap[position];
        }
        
        public void RemoveChunk(P position)
        {
            if (!chunkMap.ContainsKey(position))
                return;

            chunkMap.Remove(position);
            if (meshMap.ContainsKey(position))
            {
                Object.Destroy(meshMap[position].gameObject);
                meshMap.Remove(position);
            }
        }
    }
}