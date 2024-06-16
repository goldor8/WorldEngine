using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

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

        public List<NoiseConfig> noiseConfigs = new List<NoiseConfig>();
        public CombineMode combineMode = CombineMode.Add;
        public bool smooth;
        public bool normalize;
        public int textureWidth = 512;
        public int textureHeight = 512;

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

        public float[] GenerateNoiseMap(int width, int height, float scale, Vector2 offset, int seed)
        {
            InitializeNoise();

            float[][] noiseMaps = new float[noiseConfigs.Count][];
            Parallel.For(0, noiseConfigs.Count, i =>
            {
                _fastNoises[i].SetSeed(seed);
                noiseMaps[i] = GenerateSingleNoiseMap(_fastNoises[i], width, height, scale, offset, noiseConfigs[i].multiplier);
            });

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

        private float[] GenerateSingleNoiseMap(FastNoiseLite fastNoise, int width, int height, float scale, Vector2 offset, float multiplier)
        {
            float[] noiseMap = new float[width * height];
            int numThreads = Environment.ProcessorCount;
            int rowsPerThread = height / numThreads;

            Parallel.For(0, numThreads, threadIndex =>
            {
                int startRow = threadIndex * rowsPerThread;
                int endRow = (threadIndex == numThreads - 1) ? height : startRow + rowsPerThread;

                for (int y = startRow; y < endRow; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sampleX = (x + offset.x) * scale;
                        float sampleY = (y + offset.y) * scale;
                        float noiseValue = fastNoise.GetNoise(sampleX, sampleY) * multiplier;
                        noiseMap[y * width + x] = noiseValue;
                    }
                }
            });

            return noiseMap;
        }

        private float[] CombineNoiseMaps(float[][] noiseMaps, int width, int height)
        {
            float[] combinedNoiseMap = new float[width * height];
            int length = width * height;
            int vectorSize = Vector<float>.Count;
            int simdLength = length - (length % vectorSize);

            Parallel.For(0, simdLength / vectorSize, i =>
            {
                int index = i * vectorSize;
                Vector<float> combinedValue = Vector<float>.Zero;

                for (int j = 0; j < noiseMaps.Length; j++)
                {
                    Vector<float> noiseVector = new Vector<float>(noiseMaps[j], index);

                    switch (combineMode)
                    {
                        case CombineMode.Add:
                            combinedValue += noiseVector;
                            break;
                        case CombineMode.Multiply:
                            combinedValue *= noiseVector;
                            break;
                        case CombineMode.Average:
                            combinedValue += noiseVector;
                            break;
                    }
                }

                if (combineMode == CombineMode.Average)
                {
                    combinedValue /= new Vector<float>(noiseMaps.Length);
                }

                combinedValue.CopyTo(combinedNoiseMap, index);
            });

            for (int i = simdLength; i < length; i++)
            {
                float combinedValue = 0;

                for (int j = 0; j < noiseMaps.Length; j++)
                {
                    switch (combineMode)
                    {
                        case CombineMode.Add:
                            combinedValue += noiseMaps[j][i];
                            break;
                        case CombineMode.Multiply:
                            combinedValue *= noiseMaps[j][i];
                            break;
                        case CombineMode.Average:
                            combinedValue += noiseMaps[j][i];
                            break;
                    }
                }

                if (combineMode == CombineMode.Average)
                {
                    combinedValue /= noiseMaps.Length;
                }

                combinedNoiseMap[i] = combinedValue;
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
    public sealed class TestEditor : Editor
    {
        public Texture2D texture;
        public float scale = 0.2f;
        public Vector2 offset = Vector2.zero;
        public int seed;
        public float generationTime;

        public override void OnInspectorGUI()
        {
            var test = (Test)target;

            EditorGUI.BeginChangeCheck();

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

                if (GUILayout.Button("Remove Noise"))
                {
                    test.noiseConfigs.RemoveAt(i);
                    i--;
                }
            }

            if (GUILayout.Button("Add Noise"))
            {
                test.noiseConfigs.Add(new Test.NoiseConfig());
            }

            GUILayout.Label("Combine Settings");
            test.combineMode = (Test.CombineMode)EditorGUILayout.EnumPopup("Combine Mode", test.combineMode);

            test.smooth = EditorGUILayout.Toggle("Smooth", test.smooth);
            test.normalize = EditorGUILayout.Toggle("Normalize", test.normalize);
            test.textureWidth = EditorGUILayout.IntField("Texture Width", test.textureWidth);
            test.textureHeight = EditorGUILayout.IntField("Texture Height", test.textureHeight);
            scale = EditorGUILayout.FloatField("Scale", scale);
            offset = EditorGUILayout.Vector2Field("Offset", offset);
            seed = EditorGUILayout.IntField("Seed", seed);

            if (EditorGUI.EndChangeCheck())
            {
                test.InitializeNoise();
                RegenerateTexture(test);
            }

            if (texture != null)
            {
                GUILayout.Label($"Generation time: {generationTime}ms");
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Rect rect = GUILayoutUtility.GetRect(512, 512, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private void RegenerateTexture(Test test)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            float[] noiseMap = test.GenerateNoiseMap(test.textureWidth, test.textureHeight, scale, offset, seed);
            stopwatch.Stop();
            generationTime = stopwatch.ElapsedMilliseconds;

            var colors = new Color[test.textureWidth * test.textureHeight];
            Parallel.For(0, noiseMap.Length, i =>
            {
                float value = (noiseMap[i] + 1) * 0.5f;
                colors[i] = new Color(value, value, value);
            });

            if (texture == null || texture.width != test.textureWidth || texture.height != test.textureHeight)
            {
                texture = new Texture2D(test.textureWidth, test.textureHeight);
            }

            texture.SetPixels(colors);
            texture.Apply();
        }
    }
#endif
}
