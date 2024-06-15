using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastNoise;
using UnityEditor;
using UnityEngine;

public class Test : MonoBehaviour
{
    public FastNoise.FastNoise fastNoise = new DomainScale(new FractalFBm(new OpenSimplex2Noise()), 0.01f);

    
    public float[] GenerateNoiseMap(int width, int height, float scale, Vector2 offset, int seed)
    {
        Debug.Log(fastNoise.GetSIMDLevel());
        float[] noiseMap = new float[width * height];
        float[] xPos = new float[width * height];
        float[] yPos = new float[width * height];
        
        float[] incrementingY = new float[height];
        Parallel.For(0, height, y =>
        {
            incrementingY[y] = y / scale;
        });
        
        Parallel.For(0, width, x =>
        {
            Array.Fill(xPos, x / scale, x * height, height);
            Array.Copy(incrementingY, 0, yPos, x * height, height);
        });

        fastNoise.GenPositionArray2D(noiseMap, xPos, yPos, offset.x, offset.y, seed);
        return noiseMap;
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(Test))]
public class TestEditor : UnityEditor.Editor
{
    
    public Texture2D texture;
    public float scale = 1f;
    public Vector2 offset = Vector2.zero;
    public int seed = 0;
    public float generationTime = 0f;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var test = (Test)target;
        
        if (texture == null)
        {
            texture = new Texture2D(512, 512);
            RegenerateTexture(test);
        }
        GUILayout.Label(texture);
        GUILayout.Label($"Generation time: {generationTime}ms");

        EditorGUI.BeginChangeCheck();
        scale = EditorGUILayout.FloatField("Scale", scale);
        offset = EditorGUILayout.Vector2Field("Offset", offset);
        seed = EditorGUILayout.IntField("Seed", seed);
        
        if (EditorGUI.EndChangeCheck())
        {
            RegenerateTexture(test);
        }
    }

    private void RegenerateTexture(Test test)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        var noiseMap = test.GenerateNoiseMap(512, 512, scale, offset, seed);
        stopwatch.Stop();
        generationTime = stopwatch.ElapsedMilliseconds;
        Color[] colors = new Color[noiseMap.Length];
        Parallel.For(0, noiseMap.Length, i => { colors[i] = new Color(noiseMap[i], noiseMap[i], noiseMap[i]); });
        texture.SetPixels(colors);
        texture.Apply();
    }
}
#endif