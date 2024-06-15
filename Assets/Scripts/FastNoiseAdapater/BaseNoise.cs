using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FastNoise
{
    public class BaseNoise : FastNoise
    {
        public BaseNoise(string name) : base(name)
        {
        }

        public void Sample(float[] noiseMap, float[] xPos, float[] yPos)
        {
            
            GenPositionArray2D(noiseMap, xPos, yPos, 0, 0, 0);
        }
    }
}

