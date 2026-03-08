using System;
using UnityEngine;

namespace ShooterB
{
    public class AudioSettingsManager : MonoBehaviour
    {
        private static AudioSettingsManager instance;
        public static AudioSettingsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioSettingsManager");
                    instance = go.AddComponent<AudioSettingsManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        public event Action OnAudioSettingsChanged;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

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

        public float GetEffectiveMusicVolume()
        {
            return Mathf.Clamp01(MasterVolume) * Mathf.Clamp01(MusicVolume);
        }

        public float GetEffectiveSfxVolume()
        {
            return Mathf.Clamp01(MasterVolume) * Mathf.Clamp01(SfxVolume);
        }

        public void SetMaster(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            SaveAndNotify();
        }

        public void SetMusic(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            SaveAndNotify();
        }

        public void SetSfx(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            SaveAndNotify();
        }

        private void Load()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Constants.PREFS_MASTER_VOLUME, 1f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Constants.PREFS_MUSIC_VOLUME, 1f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Constants.PREFS_SFX_VOLUME, 1f));
        }

        private void SaveAndNotify()
        {
            PlayerPrefs.SetFloat(Constants.PREFS_MASTER_VOLUME, MasterVolume);
            PlayerPrefs.SetFloat(Constants.PREFS_MUSIC_VOLUME, MusicVolume);
            PlayerPrefs.SetFloat(Constants.PREFS_SFX_VOLUME, SfxVolume);
            PlayerPrefs.Save();
            OnAudioSettingsChanged?.Invoke();
        }
    }
}
