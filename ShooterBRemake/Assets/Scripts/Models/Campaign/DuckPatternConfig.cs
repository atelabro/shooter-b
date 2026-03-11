using UnityEngine;
using System;

namespace ShooterB
{
    [Serializable]
    public struct PatternEntry
    {
        [Tooltip("Seconds to wait after the previous spawn.")]
        public float delay;
        [Tooltip("Starting vertical lane (1=bottom, 9=top).")]
        public Constants.DuckStartLane startLane;
        [Tooltip("Movement projection for this pattern entry.")]
        public Constants.DuckPathProjection pathProjection;
        [Tooltip("Per-duck speed multiplier. <= 0 defaults to 1.")]
        public float speedMultiplier;
    }

    [CreateAssetMenu(fileName = "DuckPattern", menuName = "ShooterB/Duck Pattern Config")]
    public class DuckPatternConfig : ScriptableObject
    {
        [Tooltip("Human-readable name shown in editor and debug logs.")]
        public string patternName;

        [Tooltip("Ordered pattern entries. Duck type is inherited from the referencing SpawnEntry.")]
        public PatternEntry[] entries;
    }
}
