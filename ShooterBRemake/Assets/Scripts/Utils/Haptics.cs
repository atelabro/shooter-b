using UnityEngine;

namespace ShooterB
{
    public static class Haptics
    {
        public static void Reload()
        {
            if (!HapticsSettingsManager.Instance.VibrationEnabled)
            {
                GameLog.Log("[Haptics] Reload vibration skipped because vibration is disabled.");
                return;
            }

            GameLog.Log("[Haptics] Reload vibration triggered.");

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
