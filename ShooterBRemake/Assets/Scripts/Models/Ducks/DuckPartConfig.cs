using System;
using UnityEngine;

namespace ShooterB
{
    [Serializable]
    public struct DuckPartSkinConfig
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
        public DuckPartSkinConfig[] alternativeSkins;

        public DuckPartSkinConfig ToSkinConfig()
        {
            return new DuckPartSkinConfig
            {
                torsoSprite = torsoSprite,
                leftWingSprite = leftWingSprite,
                rightWingSprite = rightWingSprite,
                leftWingPivotOffset = leftWingPivotOffset,
                rightWingPivotOffset = rightWingPivotOffset,
                leftWingOffset = leftWingOffset,
                rightWingOffset = rightWingOffset,
                sizeMultiplier = sizeMultiplier,
                flapSpeed = flapSpeed,
                flapAmplitude = flapAmplitude,
                phaseOffset = phaseOffset,
                torsoBobAmount = torsoBobAmount,
                torsoBobSpeed = torsoBobSpeed
            };
        }
    }
}
