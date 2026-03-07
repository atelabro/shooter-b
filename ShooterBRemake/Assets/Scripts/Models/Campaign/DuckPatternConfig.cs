using UnityEngine;

namespace ShooterB
{
    [CreateAssetMenu(fileName = "DuckPattern", menuName = "ShooterB/Duck Pattern Config")]
    public class DuckPatternConfig : ScriptableObject
    {
        [Tooltip("Human-readable name shown in editor and debug logs.")]
        public string patternName;

        [Tooltip("Ordered spawn entries. delay=0 means spawn on the same frame as the previous entry.")]
        public SpawnEntry[] entries;
    }
}
