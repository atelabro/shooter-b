using UnityEngine;
using System;

namespace ShooterB
{
    [Serializable]
    public struct SpawnEntry
    {
        public Constants.DuckType duckType;
        [Tooltip("Seconds to wait after the previous spawn before spawning this duck (or before the first duck of the pattern if patternRef is set).")]
        public float delay;
        [Tooltip("Movement path for this duck. Ignored when patternRef is set.")]
        public Constants.DuckPathType pathType;
        [Tooltip("Per-duck speed multiplier. <= 0 defaults to 1. Ignored when patternRef is set.")]
        public float speedMultiplier;
        [Tooltip("If set, expands this entry into all ducks from the referenced pattern. duckType is inherited from this entry. pathType and speedMultiplier are overridden per pattern entry.")]
        public DuckPatternConfig patternRef;
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
