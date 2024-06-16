using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Définition des types de bruit disponibles
    public enum NoiseType
    {
        OpenSimplex2,
        Perlin,
        Cellular,
        Value,
        Fractal
    }

    [HideInInspector] public NoiseType noiseType = NoiseType.OpenSimplex2; // Type de bruit sélectionné
    [HideInInspector] public float frequency = 0.01f; // Fréquence du bruit
    [HideInInspector] public int octaves = 3; // Nombre d'octaves pour les bruits fractals
    [HideInInspector] public float lacunarity = 2.0f; // Lacunarity pour les bruits fractals
    [HideInInspector] public float gain = 0.5f; // Gain pour les bruits fractals
    [HideInInspector] public float multiplier = 1.0f; // Multiplicateur pour adoucir les valeurs

    private FastNoiseLite _fastNoise;

    // Initialisation du bruit à l'initialisation du script
    private void Awake()
    {
        InitializeNoise();
    }

    // Méthode pour initialiser le générateur de bruit
    private void InitializeNoise()
    {
        if (_fastNoise == null)
        {
            _fastNoise = new FastNoiseLite();
            SetNoiseType();
        }
    }

    // Méthode pour configurer le type de bruit sélectionné
    public void SetNoiseType()
    {
        switch (noiseType)
        {
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
                break;
            case NoiseType.Value:
                _fastNoise.SetNoiseType(FastNoiseLite.NoiseType.Value);
                break;
            case NoiseType.Fractal:
                _fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
                _fastNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
                break;
        }

        // Appliquer les paramètres de fractal pour les types de bruit qui le supportent
        if (noiseType == NoiseType.OpenSimplex2 || noiseType == NoiseType.Perlin || noiseType == NoiseType.Fractal)
        {
            _fastNoise.SetFractalOctaves(octaves);
            _fastNoise.SetFractalLacunarity(lacunarity);
            _fastNoise.SetFractalGain(gain);
        }
        else
        {
            // Si le type de bruit ne supporte pas les fractals, réinitialiser les paramètres
            _fastNoise.SetFractalOctaves(1);
            _fastNoise.SetFractalLacunarity(2.0f);
            _fastNoise.SetFractalGain(0.5f);
        }
    }

    // Méthode pour générer une carte de bruit
    public float[] GenerateNoiseMap(int width, int height, float scale, Vector2 offset, int seed)
    {
        InitializeNoise();
        _fastNoise.SetSeed(seed); // Définir la seed pour la génération de bruit
        _fastNoise.SetFrequency(frequency); // Définir la fréquence du bruit

        float[] noiseMap = new float[width * height]; // Tableau pour stocker les valeurs de bruit

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = (x + offset.x) * scale;
                float sampleY = (y + offset.y) * scale;
                float noiseValue = _fastNoise.GetNoise(sampleX, sampleY) * multiplier; // Appliquer le multiplicateur pour adoucir les valeurs
                noiseMap[y * width + x] = noiseValue;
            }
        });

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

    // Dessine l'interface
    public override void OnInspectorGUI()
    {
        var test = (Test)target;

        if (texture == null)
        {
            texture = new Texture2D(512, 512); // Créer une nouvelle texture
            RegenerateTexture(test);
        }
        GUILayout.Label(texture);
        GUILayout.Label($"Generation time: {generationTime}ms");

        // Créer les champs de saisie pour les paramètres de bruit
        EditorGUI.BeginChangeCheck();
        test.noiseType = (Test.NoiseType)EditorGUILayout.EnumPopup("Noise Type", test.noiseType);
        test.frequency = EditorGUILayout.FloatField("Frequency", test.frequency);
        test.octaves = EditorGUILayout.IntField("Octaves", test.octaves);
        test.lacunarity = EditorGUILayout.FloatField("Lacunarity", test.lacunarity);
        test.gain = EditorGUILayout.FloatField("Gain", test.gain);
        test.multiplier = EditorGUILayout.FloatField("Multiplier", test.multiplier);
        scale = EditorGUILayout.FloatField("Scale", scale);
        offset = EditorGUILayout.Vector2Field("Offset", offset);
        seed = EditorGUILayout.IntField("Seed", seed);

        if (EditorGUI.EndChangeCheck())
        {
            test.SetNoiseType(); // Appliquer les changements de type de bruit
            RegenerateTexture(test); // Régénérer la texture de bruit
        }
    }

    // Méthode pour régénérer la texture de bruit
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
            float value = (noiseMap[i] + 1) * 0.5f; // Normaliser les valeurs
            colors[i] = new Color(value, value, value); 
        });
        texture.SetPixels(colors);
        texture.Apply();
    }
}
#endif
