using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public class WeaponReloadSoundManager : MonoBehaviour
    {
        private const float MinRepeatIntervalSeconds = 0.05f;

        private static WeaponReloadSoundManager instance;
        public static WeaponReloadSoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("WeaponReloadSoundManager");
                    instance = go.AddComponent<WeaponReloadSoundManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        private readonly Dictionary<Constants.WeaponType, AudioClip> clipsByWeaponType =
            new Dictionary<Constants.WeaponType, AudioClip>();
        private readonly Dictionary<Constants.WeaponType, float> lastPlayTimeByWeaponType =
            new Dictionary<Constants.WeaponType, float>();

        private AudioSource reloadSfxSource;
        private bool hasLoggedMissingClip;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            AudioSettingsManager.Instance.OnAudioSettingsChanged += HandleAudioSettingsChanged;
            EnsureReady();
        }

        private void OnDestroy()
        {
            if (AudioSettingsManager.HasInstance)
                AudioSettingsManager.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
        }

        public void PlayReload(Constants.WeaponType weaponType)
        {
            EnsureReady();
            if (reloadSfxSource == null)
                return;

            if (lastPlayTimeByWeaponType.TryGetValue(weaponType, out float lastTime))
            {
                if (Time.unscaledTime - lastTime < MinRepeatIntervalSeconds)
                    return;
            }

            if (!clipsByWeaponType.TryGetValue(weaponType, out AudioClip clip) || clip == null)
                return;

            lastPlayTimeByWeaponType[weaponType] = Time.unscaledTime;
            reloadSfxSource.PlayOneShot(clip);
        }

        private void EnsureReady()
        {
            if (reloadSfxSource == null)
            {
                reloadSfxSource = gameObject.GetComponent<AudioSource>();
                if (reloadSfxSource == null)
                    reloadSfxSource = gameObject.AddComponent<AudioSource>();

                reloadSfxSource.playOnAwake = false;
                reloadSfxSource.loop = false;
                reloadSfxSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
            }

            if (clipsByWeaponType.Count > 0)
                return;

            // Keep legacy mapping parity from original BaseSoundController.refill(...)
            clipsByWeaponType[Constants.WeaponType.MrSulko] = Resources.Load<AudioClip>("Audio/mrsulkoReload");
            clipsByWeaponType[Constants.WeaponType.Rifle] = Resources.Load<AudioClip>("Audio/shotgunreload");
            clipsByWeaponType[Constants.WeaponType.Cabirne] = Resources.Load<AudioClip>("Audio/sniperReload");
            clipsByWeaponType[Constants.WeaponType.Beretta] = Resources.Load<AudioClip>("Audio/sniperReload");
            clipsByWeaponType[Constants.WeaponType.LaserGun] = Resources.Load<AudioClip>("Audio/laserReload");
            clipsByWeaponType[Constants.WeaponType.TeslaGun] = Resources.Load<AudioClip>("Audio/teslaReload");
            clipsByWeaponType[Constants.WeaponType.PiranhaGun] = Resources.Load<AudioClip>("Audio/piranhaReload");

            if (!hasLoggedMissingClip)
            {
                foreach (KeyValuePair<Constants.WeaponType, AudioClip> kv in clipsByWeaponType)
                {
                    if (kv.Value != null)
                        continue;

                    hasLoggedMissingClip = true;
                    GameLog.Warning($"[WeaponReloadSoundManager] Missing reload SFX for {kv.Key} in Resources/Audio.");
                    break;
                }
            }
        }

        private void HandleAudioSettingsChanged()
        {
            if (reloadSfxSource == null)
                return;

            reloadSfxSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
        }
    }
}
