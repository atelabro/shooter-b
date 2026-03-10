using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public class WeaponShootSoundManager : MonoBehaviour
    {
        private const float MinRepeatIntervalSeconds = 0.02f;

        private static WeaponShootSoundManager instance;
        public static WeaponShootSoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("WeaponShootSoundManager");
                    instance = go.AddComponent<WeaponShootSoundManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        private readonly Dictionary<Constants.WeaponType, AudioClip> clipsByWeaponType =
            new Dictionary<Constants.WeaponType, AudioClip>();
        private readonly Dictionary<Constants.WeaponType, float> lastPlayTimeByWeaponType =
            new Dictionary<Constants.WeaponType, float>();

        private AudioSource shootSfxSource;
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

        public void PlayShoot(Constants.WeaponType weaponType)
        {
            EnsureReady();
            if (shootSfxSource == null)
                return;

            if (lastPlayTimeByWeaponType.TryGetValue(weaponType, out float lastTime))
            {
                if (Time.unscaledTime - lastTime < MinRepeatIntervalSeconds)
                    return;
            }

            if (!clipsByWeaponType.TryGetValue(weaponType, out AudioClip clip) || clip == null)
                return;

            lastPlayTimeByWeaponType[weaponType] = Time.unscaledTime;
            shootSfxSource.PlayOneShot(clip);
        }

        private void EnsureReady()
        {
            if (shootSfxSource == null)
            {
                shootSfxSource = gameObject.GetComponent<AudioSource>();
                if (shootSfxSource == null)
                    shootSfxSource = gameObject.AddComponent<AudioSource>();

                shootSfxSource.playOnAwake = false;
                shootSfxSource.loop = false;
                shootSfxSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
            }

            if (clipsByWeaponType.Count > 0)
                return;

            // Keep legacy mapping parity from original BaseSoundController.shoot(...)
            clipsByWeaponType[Constants.WeaponType.MrSulko] = Resources.Load<AudioClip>("Audio/mrsulko");
            clipsByWeaponType[Constants.WeaponType.Rifle] = Resources.Load<AudioClip>("Audio/shotgun");
            clipsByWeaponType[Constants.WeaponType.Cabirne] = Resources.Load<AudioClip>("Audio/sniper");
            clipsByWeaponType[Constants.WeaponType.Beretta] = Resources.Load<AudioClip>("Audio/machine");
            clipsByWeaponType[Constants.WeaponType.LaserGun] = Resources.Load<AudioClip>("Audio/laser");
            clipsByWeaponType[Constants.WeaponType.TeslaGun] = Resources.Load<AudioClip>("Audio/tesla");
            clipsByWeaponType[Constants.WeaponType.PiranhaGun] = Resources.Load<AudioClip>("Audio/piranha");

            if (!hasLoggedMissingClip)
            {
                foreach (KeyValuePair<Constants.WeaponType, AudioClip> kv in clipsByWeaponType)
                {
                    if (kv.Value != null)
                        continue;

                    hasLoggedMissingClip = true;
                    GameLog.Warning($"[WeaponShootSoundManager] Missing shoot SFX for {kv.Key} in Resources/Audio.");
                    break;
                }
            }
        }

        private void HandleAudioSettingsChanged()
        {
            if (shootSfxSource == null)
                return;

            shootSfxSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
        }
    }
}
