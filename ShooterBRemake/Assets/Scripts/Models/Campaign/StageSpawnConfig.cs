using UnityEngine;
using System;

namespace ShooterB
{
    [Serializable]
    public struct SpawnEntry
    {
        public Constants.DuckType duckType;
        [Tooltip("Seconds to wait after the previous spawn before spawning this duck")]
        public float delay;
    }

    [CreateAssetMenu(fileName = "StageSpawnConfig", menuName = "ShooterB/Stage Spawn Config")]
    public class StageSpawnConfig : ScriptableObject
    {
        [Header("Spawn Sequence")]
        public SpawnEntry[] spawnSequence;

        [Header("Movement Pattern Weights")]
        public float weightGoStraight = 0.4f;
        public float weightGoTop = 0.3f;
        public float weightGoBottom = 0.3f;

        [Header("Duck Speed")]
        public float duckSpeedMultiplier = 1f;
    }
}
