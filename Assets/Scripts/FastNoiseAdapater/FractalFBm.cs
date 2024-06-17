using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FastNoiseAdaptater
{
    public class FractalFBm : FastNoise
    {
        private FastNoise _source;
        private NoiseOrValue _gain;
        private NoiseOrValue _weightedStrength;
        private int _octaves;
        private float _lacunarity;
        
        public FractalFBm() : base("FractalFBm")
        {
            _source = null;
            Gain = new NoiseOrValue(0.65f);
            WeightedStrength = new NoiseOrValue(0.5f);
            Octaves = 4;
            Lacunarity = 2f;
        }
        
        public FractalFBm(FastNoise source) : base("FractalFBm")
        {
            Source = source;
            Gain = new NoiseOrValue(0.65f);
            WeightedStrength = new NoiseOrValue(0.5f);
            Octaves = 4;
            Lacunarity = 2f;
        }
        
        public FractalFBm(FastNoise source, NoiseOrValue gain, NoiseOrValue weightedStrength, int octaves, float lacunarity) : base("FractalFBm")
        {
            Source = source;
            Gain = gain;
            WeightedStrength = weightedStrength;
            Octaves = octaves;
            Lacunarity = lacunarity;
        }
        
        public FastNoise Source
        {
            get => _source;
            set
            {
                _source = value;
                Set("Source", value);
            }
        }
        
        public NoiseOrValue Gain
        {
            get => _gain;
            set
            {
                _gain = value;
                if(value.IsValue)
                    Set("Gain", value.Value);
                else
                    Set("Gain", value.Noise);
            }
        }
        
        public NoiseOrValue WeightedStrength
        {
            get => _weightedStrength;
            set
            {
                _weightedStrength = value;
                if(value.IsValue)
                    Set("WeightedStrength", value.Value);
                else
                    Set("WeightedStrength", value.Noise);
            }
        }
        
        public int Octaves
        {
            get => _octaves;
            set
            {
                _octaves = value;
                Set("Octaves", value);
            }
        }

        public float Lacunarity
        {
            get => _lacunarity;
            set
            {
                _lacunarity = value;
                Set("lacunarity", value);
            }
        }
    }   
}
