using System;
using UnityEngine;

namespace ShooterB
{
    public class HapticsSettingsManager : MonoBehaviour
    {
        private static HapticsSettingsManager instance;

        public static HapticsSettingsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("HapticsSettingsManager");
                    instance = go.AddComponent<HapticsSettingsManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        public event Action OnVibrationSettingsChanged;

        public bool VibrationEnabled { get; private set; } = true;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void SetVibrationEnabled(bool enabled)
        {
            if (VibrationEnabled == enabled)
                return;

            VibrationEnabled = enabled;
            Save();
            OnVibrationSettingsChanged?.Invoke();
        }

        private void Load()
        {
            VibrationEnabled = PlayerPrefs.GetInt(Constants.PREFS_VIBRATION_ENABLED, 1) == 1;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(Constants.PREFS_VIBRATION_ENABLED, VibrationEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
