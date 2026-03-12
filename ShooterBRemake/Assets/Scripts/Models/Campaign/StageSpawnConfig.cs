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
        [Tooltip("Optional health override for this duck. Values <= 0 use the duck type default.")]
        public int healthOverride;
        [Tooltip("Starting vertical lane for this duck (1=bottom, 9=top). Ignored when patternRef is set.")]
        public Constants.DuckStartLane startLane;
        [Tooltip("Movement projection for this duck. Ignored when patternRef is set.")]
        public Constants.DuckPathProjection pathProjection;
        [Tooltip("Per-duck speed multiplier. <= 0 defaults to 1. Ignored when patternRef is set.")]
        public float speedMultiplier;
        [Tooltip("If set, expands this entry into all ducks from the referenced pattern. duckType is inherited from this entry. startLane/pathProjection/speedMultiplier are overridden per pattern entry.")]
        public DuckPatternConfig patternRef;
    }

    [Serializable]
    public class WaveConfig
    {
        public bool showAnnouncement = true;
        public float announcementDuration = 3f;
        [Tooltip("Optional wave label format override for English, e.g. 'Training Wave {0}'. Leave empty to use localization default.")]
        public string englishWaveLabelFormat;
        [Tooltip("Optional wave label format override for Macedonian, e.g. 'Тренинг Бран {0}'. Leave empty to use localization default.")]
        public string macedonianWaveLabelFormat;
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
