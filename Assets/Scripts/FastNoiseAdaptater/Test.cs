using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace FastNoiseAdaptater
{
    public sealed class Test : MonoBehaviour
    {
        public enum NoiseType
        {
            OpenSimplex2,
            Perlin,
            Cellular,
            Value,
            Fractal,
            OpenSimplex2S,
            ValueCubic
        }

        public enum CombineMode
        {
            Add,
            Multiply,
            Average
        }

        // Stock la config d'un bruit
        [System.Serializable]
        public class NoiseConfig
        {
            public NoiseType noiseType = NoiseType.OpenSimplex2;
            public float frequency = 0.01f;
            public int octaves = 3;
            public float lacunarity = 2.0f;
            public float gain = 0.5f;
            public float jitter = 0.5f;
            public float multiplier = 1.0f;
        }

        // Configure les bruits
        public List<NoiseConfig> noiseConfigs = new List<NoiseConfig>(); // Liste des configurations
        public CombineMode combineMode = CombineMode.Add;
        public bool smooth;
        public bool normalize;

        private readonly List<FastNoiseLite> _fastNoises = new List<FastNoiseLite>();

        private void Awake()
        {
            InitializeNoise();
        }

        public void InitializeNoise()
        {
            _fastNoises.Clear();
            foreach (NoiseConfig config in noiseConfigs)
            {
                var fastNoise = new FastNoiseLite();
                SetNoiseType(fastNoise, config);
                _fastNoises.Add(fastNoise);
            }
        }

        private void SetNoiseType(FastNoiseLite fastNoise, NoiseConfig config)
        {
            fastNoise.SetFrequency(config.frequency);

            switch (config.noiseType)
            {
                case NoiseType.Fractal:
                case NoiseType.OpenSimplex2:
                    fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
                    fastNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
                    break;
                case NoiseType.Perlin:
                    fastNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
                    fastNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
                    break;
                case NoiseType.Cellular:
                    fastNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
                    fastNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
                    fastNoise.SetCellularJitter(config.jitter);
                    break;
                case NoiseType.Value:
                    fastNoise.SetNoiseType(FastNoiseLite.NoiseType.Value);
                    break;
                case NoiseType.OpenSimplex2S:
                    fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
                    break;
                case NoiseType.ValueCubic:
                    fastNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
                    break;
            }

            if (config.noiseType is NoiseType.OpenSimplex2 or NoiseType.Perlin or NoiseType.Fractal)
            {
                fastNoise.SetFractalOctaves(config.octaves);
                fastNoise.SetFractalLacunarity(config.lacunarity);
                fastNoise.SetFractalGain(config.gain);
            }
            else
            {
                fastNoise.SetFractalOctaves(1);
                fastNoise.SetFractalLacunarity(2.0f);
                fastNoise.SetFractalGain(0.5f);
            }
        }

        // Génère la carte de bruit quand il y en a plusieurs
        public float[] GenerateNoiseMap(int width, int height, float scale, Vector2 offset, int seed)
        {
            InitializeNoise();

            float[][] noiseMaps = new float[noiseConfigs.Count][];
            for (int i = 0; i < noiseConfigs.Count; i++)
            {
                _fastNoises[i].SetSeed(seed);
                noiseMaps[i] = GenerateSingleNoiseMap(_fastNoises[i], width, height, scale, offset, noiseConfigs[i].multiplier);
            }

            float[] combinedNoiseMap = CombineNoiseMaps(noiseMaps, width, height);

            if (smooth)
            {
                combinedNoiseMap = SmoothNoise(combinedNoiseMap, width, height);
            }

            if (normalize)
            {
                combinedNoiseMap = NormalizeNoise(combinedNoiseMap);
            }

            return combinedNoiseMap;
        }

        // Générer une carte de bruit unique
        private float[] GenerateSingleNoiseMap(FastNoiseLite fastNoise, int width, int height, float scale, Vector2 offset, float multiplier)
        {
            float[] noiseMap = new float[width * height];

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float sampleX = (x + offset.x) * scale;
                    float sampleY = (y + offset.y) * scale;
                    float noiseValue = fastNoise.GetNoise(sampleX, sampleY) * multiplier;
                    noiseMap[y * width + x] = noiseValue;
                }
            });

            return noiseMap;
        }

        // Combiner les cartes de bruit
        private float[] CombineNoiseMaps(float[][] noiseMaps, int width, int height)
        {
            float[] combinedNoiseMap = new float[width * height];
            for (int i = 0; i < combinedNoiseMap.Length; i++)
            {
                float combinedValue = 0;
                foreach (float[] t in noiseMaps)
                {
                    switch (combineMode)
                    {
                        case CombineMode.Add:
                            combinedValue += t[i];
                            break;
                        case CombineMode.Multiply:
                            combinedValue *= (combinedValue == 0.0f) ? t[i] : combinedValue * t[i];
                            break;
                        case CombineMode.Average:
                            combinedValue += t[i];
                            break;
                    }
                }
                combinedNoiseMap[i] = (combineMode == CombineMode.Average) ? combinedValue / noiseMaps.Length : combinedValue;
            }

            return combinedNoiseMap;
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
            foreach (float value in noiseMap)
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
        public int seed;
        public float generationTime;

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

            // Afficher les paramètres pour chaque configuration de bruit
            for (int i = 0; i < test.noiseConfigs.Count; i++)
            {
                GUILayout.Label($"Noise {i + 1} Settings");
                Test.NoiseConfig config = test.noiseConfigs[i];
                config.noiseType = (Test.NoiseType)EditorGUILayout.EnumPopup("Noise Type", config.noiseType);
                config.frequency = EditorGUILayout.FloatField("Frequency", config.frequency);
                if (config.noiseType is Test.NoiseType.OpenSimplex2 or Test.NoiseType.Perlin or Test.NoiseType.Fractal)
                {
                    config.octaves = EditorGUILayout.IntField("Octaves", config.octaves);
                    config.lacunarity = EditorGUILayout.FloatField("Lacunarity", config.lacunarity);
                    config.gain = EditorGUILayout.FloatField("Gain", config.gain);
                }
                if (config.noiseType == Test.NoiseType.Cellular)
                {
                    config.jitter = EditorGUILayout.FloatField("Jitter", config.jitter);
                }
                config.multiplier = EditorGUILayout.FloatField("Multiplier", config.multiplier);

                // Bouton pour supprimer une configuration de bruit
                if (GUILayout.Button("Remove Noise"))
                {
                    test.noiseConfigs.RemoveAt(i);
                    i--;
                }
            }

            // Bouton pour ajouter une nouvelle configuration de bruit
            if (GUILayout.Button("Add Noise"))
            {
                test.noiseConfigs.Add(new Test.NoiseConfig());
            }

            // Paramètres de combinaison
            GUILayout.Label("Combine Settings");
            test.combineMode = (Test.CombineMode)EditorGUILayout.EnumPopup("Combine Mode", test.combineMode);

            test.smooth = EditorGUILayout.Toggle("Smooth", test.smooth);
            test.normalize = EditorGUILayout.Toggle("Normalize", test.normalize);
            scale = EditorGUILayout.FloatField("Scale", scale);
            offset = EditorGUILayout.Vector2Field("Offset", offset);
            seed = EditorGUILayout.IntField("Seed", seed);

            if (EditorGUI.EndChangeCheck())
            {
                test.InitializeNoise();
                RegenerateTexture(test);
            }
        }

        private void RegenerateTexture(Test test)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            float[] noiseMap = test.GenerateNoiseMap(512, 512, scale, offset, seed);
            stopwatch.Stop();
            generationTime = stopwatch.ElapsedMilliseconds;

            var colors = new Color[noiseMap.Length];
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
