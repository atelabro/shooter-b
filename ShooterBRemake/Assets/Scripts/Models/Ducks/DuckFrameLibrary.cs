using System;
using UnityEngine;

namespace ShooterB
{
    [Serializable]
    public struct DuckFrameSet
    {
        public Constants.DuckType duckType;
        public Sprite[] frames;
    }

    [CreateAssetMenu(fileName = "DuckFrameLibrary", menuName = "ShooterB/Duck Frame Library")]
    public class DuckFrameLibrary : ScriptableObject
    {
        [Header("Duck Type Frames")]
        public DuckFrameSet[] frameSets;

        public Sprite[] GetFrames(Constants.DuckType type)
        {
            Sprite[] fallbackType0 = null;

            if (frameSets == null || frameSets.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < frameSets.Length; i++)
            {
                if (frameSets[i].duckType == Constants.DuckType.Type0 &&
                    frameSets[i].frames != null &&
                    frameSets[i].frames.Length > 0)
                {
                    fallbackType0 = frameSets[i].frames;
                }
            }

            for (int i = 0; i < frameSets.Length; i++)
            {
                if (frameSets[i].duckType == type &&
                    frameSets[i].frames != null &&
                    frameSets[i].frames.Length > 0)
                {
                    return frameSets[i].frames;
                }
            }

            return fallbackType0;
        }

        public void SetFrames(Constants.DuckType type, Sprite[] frames)
        {
            if (frameSets == null)
            {
                frameSets = new DuckFrameSet[0];
            }

            for (int i = 0; i < frameSets.Length; i++)
            {
                if (frameSets[i].duckType == type)
                {
                    frameSets[i].frames = frames;
                    return;
                }
            }

            DuckFrameSet[] expanded = new DuckFrameSet[frameSets.Length + 1];
            for (int i = 0; i < frameSets.Length; i++)
            {
                expanded[i] = frameSets[i];
            }

            expanded[expanded.Length - 1] = new DuckFrameSet
            {
                duckType = type,
                frames = frames
            };

            frameSets = expanded;
        }
    }
}
