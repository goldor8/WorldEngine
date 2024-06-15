using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FastNoise
{
    public class NoiseOrValue
    {
        private float _value;
        private FastNoise _noise;
        
        public NoiseOrValue(float value)
        {
            _value = value;
            _noise = null;
        }
        
        public NoiseOrValue(FastNoise noise)
        {
            _noise = noise;
            _value = 0f;
        }
        
        public bool IsNoise => _noise != null;
        public bool IsValue => _noise == null;
        
        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                _noise = null;   
            }
        }
        
        public FastNoise Noise
        {
            get => _noise;
            set
            {
                _noise = value;
                _value = 0f;
            }
        }
    }
}
