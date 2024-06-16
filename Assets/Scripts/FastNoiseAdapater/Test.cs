using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class Test : MonoBehaviour
{
    private FastNoiseLite _fastNoise;

    private void Awake()
    {
        InitializeNoise();
    }

    private void InitializeNoise()
    {
        if (_fastNoise == null)
        {
            _fastNoise = new FastNoiseLite();
            _fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        }
    }

    public float[] GenerateNoiseMap(int width, int height, float scale, Vector2 offset, int seed)
    {
        InitializeNoise();
        _fastNoise.SetSeed(seed);
        _fastNoise.SetFrequency(1.0f / scale);

        float[] noiseMap = new float[width * height];

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = (x + offset.x) * scale;
                float sampleY = (y + offset.y) * scale;
                float noiseValue = _fastNoise.GetNoise(sampleX, sampleY);
                noiseMap[y * width + x] = noiseValue;
            }
        });

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
