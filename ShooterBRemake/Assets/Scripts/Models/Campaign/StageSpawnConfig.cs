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
        [Tooltip("Movement path for this duck. Random uses weight-based pattern switching.")]
        public Constants.DuckPathType pathType;
        [Tooltip("Per-duck speed multiplier. <= 0 defaults to 1.")]
        public float speedMultiplier;
    }

    [Serializable]
    public class WaveConfig
    {
        public bool showAnnouncement = true;
        public float announcementDuration = 3f;
        public SpawnEntry[] spawnSequence;
    }

    [CreateAssetMenu(fileName = "StageSpawnConfig", menuName = "ShooterB/Stage Spawn Config")]
    public class StageSpawnConfig : ScriptableObject
    {
        [Header("Spawn Sequence")]
        public SpawnEntry[] spawnSequence;

        [Header("Waves (overrides spawnSequence if non-empty)")]
        public WaveConfig[] waves;

        [Header("Movement Pattern Weights")]
        public float weightGoStraight = 0.4f;
        public float weightGoTop = 0.3f;
        public float weightGoBottom = 0.3f;

        [Header("Duck Speed")]
        public float duckSpeedMultiplier = 1f;
    }
}
