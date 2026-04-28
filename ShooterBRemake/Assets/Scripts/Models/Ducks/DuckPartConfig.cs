using System;
using UnityEngine;

namespace ShooterB
{
    [Serializable]
    public struct DuckPartConfig
    {
        public Sprite torsoSprite;
        public Sprite leftWingSprite;
        public Sprite rightWingSprite;
        public Vector2 leftWingPivotOffset;
        public Vector2 rightWingPivotOffset;
        public Vector2 leftWingOffset;
        public Vector2 rightWingOffset;
        public float sizeMultiplier;
        public float flapSpeed;
        public float flapAmplitude;
        public float phaseOffset;
        public float torsoBobAmount;
        public float torsoBobSpeed;
    }
}
