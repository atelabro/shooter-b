using System;
using UnityEngine;

namespace ShooterB
{
    [Serializable]
    public struct DuckPartEntry
    {
        public Constants.DuckType duckType;
        public DuckPartConfig config;
    }

    [CreateAssetMenu(fileName = "DuckPartLibrary", menuName = "ShooterB/Duck Part Library")]
    public class DuckPartLibrary : ScriptableObject
    {
        [Header("Duck Part Entries")]
        public DuckPartEntry[] entries;

        public void SetConfig(Constants.DuckType type, DuckPartConfig config)
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].duckType == type)
                {
                    entries[i].config = config;
                    return;
                }
            }
        }

        public bool TryGetConfig(Constants.DuckType type, out DuckPartConfig config)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].duckType == type)
                    {
                        config = entries[i].config;
                        return true;
                    }
                }
            }
            config = default;
            return false;
        }
    }
}
