using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// TODO: D'autres types de bruits ??
namespace FastNoiseAdaptater
{
    public sealed class Test : MonoBehaviour
    {
        // Types de bruit disponibles
        public enum NoiseType
        {
            OpenSimplex2,
            Perlin,
            Cellular,
            Value,
            Fractal
        }

        // Paramètres des bruits
        [HideInInspector] public NoiseType noiseType = NoiseType.OpenSimplex2;
        [HideInInspector] public float frequency = 0.01f;
        [HideInInspector] public int octaves = 3;
        [HideInInspector] public float lacunarity = 2.0f;
        [HideInInspector] public float gain = 0.5f;
        [HideInInspector] public float jitter = 0.5f;
        [HideInInspector] public float multiplier = 1.0f;
        [HideInInspector] public bool smooth;
        [HideInInspector] public bool normalize;

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
                SetNoiseType();
            }
        }

        // Configurer le type de bruit sélectionné
        public void SetNoiseType()
        {
            switch (noiseType)
            {
                case NoiseType.Fractal:
                case NoiseType.OpenSimplex2:
                    _fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
                    _fastNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
                    break;
                case NoiseType.Perlin:
                    _fastNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
                    _fastNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
                    break;
                case NoiseType.Cellular:
                    _fastNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
                    _fastNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
                    _fastNoise.SetCellularJitter(jitter);
                    break;
                case NoiseType.Value:
                    _fastNoise.SetNoiseType(FastNoiseLite.NoiseType.Value);
                    break;
            }

            // Appliquer les paramètres de fractal si le type de bruit le supporte
            if (noiseType == NoiseType.OpenSimplex2 || noiseType == NoiseType.Perlin || noiseType == NoiseType.Fractal)
            {
                _fastNoise.SetFractalOctaves(octaves);
                _fastNoise.SetFractalLacunarity(lacunarity);
                _fastNoise.SetFractalGain(gain);
            }
            else
            {
                // Réinitialiser les paramètres de fractal pour les autres types de bruit
                _fastNoise.SetFractalOctaves(1);
                _fastNoise.SetFractalLacunarity(2.0f);
                _fastNoise.SetFractalGain(0.5f);
            }
        }

        // Générer la carte de bruit
        public float[] GenerateNoiseMap(int width, int height, float scale, Vector2 offset, int seed)
        {
            InitializeNoise();
            _fastNoise.SetSeed(seed);
            _fastNoise.SetFrequency(frequency);

            float[] noiseMap = new float[width * height];

            float[] map = noiseMap;
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float sampleX = (x + offset.x) * scale;
                    float sampleY = (y + offset.y) * scale;
                    float noiseValue = _fastNoise.GetNoise(sampleX, sampleY) * multiplier;
                    map[y * width + x] = noiseValue;
                }
            });

            if (smooth)
            {
                noiseMap = SmoothNoise(noiseMap, width, height);
            }

            if (normalize)
            {
                noiseMap = NormalizeNoise(noiseMap);
            }

            return noiseMap;
        }

        private float[] SmoothNoise(float[] noiseMap, int width, int height)
        {
            float[] smoothNoise = new float[width * height];
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float sum = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            sum += noiseMap[(y + ky) * width + (x + kx)];
                        }
                    }
                    smoothNoise[y * width + x] = sum / 9.0f;
                }
            }
            return smoothNoise;
        }

        private float[] NormalizeNoise(float[] noiseMap)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (var value in noiseMap)
            {
                if (value < min) min = value;
                if (value > max) max = value;
            }

            float range = max - min;
            for (int i = 0; i < noiseMap.Length; i++)
            {
                noiseMap[i] = (noiseMap[i] - min) / range;
            }
            return noiseMap;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Test))]
    public class TestEditor : Editor
    {
        public Texture2D texture;
        public float scale = 0.2f;
        public Vector2 offset = Vector2.zero;
        public int seed = 0;
        public float generationTime = 0f;

        public override void OnInspectorGUI()
        {
            var test = (Test)target;

            if (texture == null)
            {
                texture = new Texture2D(512, 512);
                RegenerateTexture(test);
            }
            GUILayout.Label(texture);
            GUILayout.Label($"Generation time: {generationTime}ms");

            EditorGUI.BeginChangeCheck();
            test.noiseType = (Test.NoiseType)EditorGUILayout.EnumPopup("Noise Type", test.noiseType);
            test.frequency = EditorGUILayout.FloatField("Frequency", test.frequency);

            if (test.noiseType == Test.NoiseType.OpenSimplex2 || test.noiseType == Test.NoiseType.Perlin || test.noiseType == Test.NoiseType.Fractal)
            {
                test.octaves = EditorGUILayout.IntField("Octaves", test.octaves);
                test.lacunarity = EditorGUILayout.FloatField("Lacunarity", test.lacunarity);
                test.gain = EditorGUILayout.FloatField("Gain", test.gain);
            }

            if (test.noiseType == Test.NoiseType.Cellular)
            {
                test.jitter = EditorGUILayout.FloatField("Jitter", test.jitter);
            }

            test.multiplier = EditorGUILayout.FloatField("Multiplier", test.multiplier);
            test.smooth = EditorGUILayout.Toggle("Smooth", test.smooth);
            test.normalize = EditorGUILayout.Toggle("Normalize", test.normalize);
            scale = EditorGUILayout.FloatField("Scale", scale);
            offset = EditorGUILayout.Vector2Field("Offset", offset);
            seed = EditorGUILayout.IntField("Seed", seed);

            if (EditorGUI.EndChangeCheck())
            {
                test.SetNoiseType();
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
            Parallel.For(0, noiseMap.Length, i =>
            {
                float value = (noiseMap[i] + 1) * 0.5f;
                colors[i] = new Color(value, value, value);
            });
            texture.SetPixels(colors);
            texture.Apply();
        }
    }
#endif
}
