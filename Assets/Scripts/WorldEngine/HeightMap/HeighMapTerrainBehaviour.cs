using System;
using System.Threading.Tasks;
using FastNoiseAdaptater;
using UnityEditor;
using UnityEngine;

namespace WorldEngine.HeightMap
{
    public class HeighMapTerrainBehaviour : MonoBehaviour
    {
        public HeightMapTerrainManager terrainManager = new HeightMapTerrainManager();
        
        private void Start()
        {
            terrainManager.GenerateChunk(new Vector2Int(0, 0));
            terrainManager.GenerateMesh(new Vector2Int(0, 0));
        }
    }
    
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(HeighMapTerrainBehaviour))]
    public class HeighMapTerrainBehaviourEditor : UnityEditor.Editor
    {
        private float scale = 0.001f;
        private float height = 1f;
        private int chunkSize = 64;
        Vector2Int chunkPosition;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            HeighMapTerrainBehaviour terrainBehaviour = (HeighMapTerrainBehaviour) target;
            chunkPosition = UnityEditor.EditorGUILayout.Vector2IntField("Chunk Position", chunkPosition);
            EditorGUI.BeginChangeCheck();
            scale = EditorGUILayout.FloatField("Scale", scale);
            height = EditorGUILayout.FloatField("Height", height);
            chunkSize = EditorGUILayout.IntField("Chunk Size", chunkSize);
            if(EditorGUI.EndChangeCheck())
            {
                terrainBehaviour.terrainManager.ChunkHeight = height;
                terrainBehaviour.terrainManager.ChunkSize = chunkSize;
                terrainBehaviour.terrainManager.SetNoise(new DomainScale(new FractalFBm(new OpenSimplex2Noise()), scale));
            }
            
            if (GUILayout.Button("Generate Chunk"))
            {
                terrainBehaviour.terrainManager.GenerateChunk(chunkPosition);
            }
            
            if(GUILayout.Button("Generate Mesh"))
            {
                terrainBehaviour.terrainManager.GenerateMesh(chunkPosition);
            }
            
            if (GUILayout.Button("Unload Chunk"))
            {
                terrainBehaviour.terrainManager.RemoveChunk(chunkPosition);
            }
        }
    }
    #endif
}