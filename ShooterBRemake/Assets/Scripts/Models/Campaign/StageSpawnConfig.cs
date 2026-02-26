using UnityEngine;

namespace ShooterB
{
    [CreateAssetMenu(fileName = "StageSpawnConfig", menuName = "ShooterB/Stage Spawn Config")]
    public class StageSpawnConfig : ScriptableObject
    {
        [Header("Duck Type Weights (all 0 = use global defaults)")]
        public float weightType0 = 0f;
        public float weightType1 = 0f;
        public float weightType2 = 0f;
        public float weightType3 = 0f;
        public float weightType4 = 0f;

        [Header("Movement Pattern Weights")]
        public float weightGoStraight = 0.4f;
        public float weightGoTop = 0.3f;
        public float weightGoBottom = 0.3f;

        [Header("Spawn Timing Overrides (0 = use difficulty default)")]
        public float spawnDelayBase = 0f;
        public float spawnDelayVariance = 0f;

        [Header("Active Duck Limit (0 = no limit)")]
        public int maxActiveDucks = 0;

        [Header("Duck Speed")]
        public float duckSpeedMultiplier = 1f;

        public bool HasTypeWeightOverride()
        {
            return weightType0 > 0f || weightType1 > 0f || weightType2 > 0f
                || weightType3 > 0f || weightType4 > 0f;
        }
    }
}
