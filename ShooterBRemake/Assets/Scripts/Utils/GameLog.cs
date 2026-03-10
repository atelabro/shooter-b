using System.Diagnostics;
using UnityEngine;

namespace ShooterB
{
    public static class GameLog
    {
        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        [Conditional("DEBUG")]
        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}
