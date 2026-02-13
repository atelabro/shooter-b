using UnityEngine;
using System.Collections.Generic;

namespace ShooterB
{
    public class ShooterController : MonoBehaviour
    {
        [System.Serializable]
        public class WeaponIconEntry
        {
            public Constants.WeaponType weaponType;
            public Sprite icon;
        }

        [Header("Weapons")]
        public Weapon activeWeapon;

        [Header("Prefabs")]
        public GameObject rifleBulletPrefab;
        public Sprite defaultRifleIcon;

        [Header("Weapon Icons")]
        public List<WeaponIconEntry> weaponIcons = new List<WeaponIconEntry>();

        private void Awake()
        {
            EnsureActiveWeapon();
        }

        private void Start()
        {
            EnsureActiveWeapon();

            StartCoroutine(LogInitialization());
        }

        private void EnsureActiveWeapon()
        {
            if (activeWeapon != null)
            {
                ApplyIconIfMissing(activeWeapon);
                return;
            }

            GameObject rifleObj = new GameObject("Rifle");
            rifleObj.transform.SetParent(transform);
            Rifle rifle = rifleObj.AddComponent<Rifle>();

            if (rifleBulletPrefab != null)
            {
                rifle.bulletPrefab = rifleBulletPrefab;
            }

            if (defaultRifleIcon != null && rifle.weaponIcon == null)
            {
                rifle.weaponIcon = defaultRifleIcon;
            }

            activeWeapon = rifle;
            ApplyIconIfMissing(activeWeapon);

            if (activeWeapon == null)
            {
                Debug.LogError("[SHOOTER] Failed to initialize active weapon.");
            }
        }

        private void ApplyIconIfMissing(Weapon weapon)
        {
            if (weapon == null || weapon.weaponIcon != null)
                return;

            Sprite mappedIcon = FindIconForWeaponType(weapon.weaponType);
            if (mappedIcon != null)
            {
                weapon.weaponIcon = mappedIcon;
            }
            else if (weapon.weaponType == Constants.WeaponType.Rifle && defaultRifleIcon != null)
            {
                weapon.weaponIcon = defaultRifleIcon;
            }
        }

        private Sprite FindIconForWeaponType(Constants.WeaponType weaponType)
        {
            for (int i = 0; i < weaponIcons.Count; i++)
            {
                WeaponIconEntry entry = weaponIcons[i];
                if (entry != null && entry.weaponType == weaponType && entry.icon != null)
                    return entry.icon;
            }

            return null;
        }

        private System.Collections.IEnumerator LogInitialization()
        {
            yield return null;
            Debug.Log($"[SHOOTER] ShooterController initialized with weapon: {activeWeapon?.weaponName}");
        }

        public void Shoot(Vector2 targetPosition)
        {
            if (activeWeapon == null)
            {
                Debug.LogWarning("[SHOOTER] No active weapon!");
                return;
            }

            bool shot = activeWeapon.Shoot(targetPosition);

            if (shot)
            {
                Debug.Log($"[SHOOTER] Shot fired at {targetPosition}. Ammo remaining: {activeWeapon.CurrentBullets}");
            }
            else
            {
                Debug.Log($"[SHOOTER] Cannot shoot - Ammo: {activeWeapon.CurrentBullets}, Refilling: {activeWeapon.IsRefilling}");
            }
        }

        public int GetCurrentAmmo()
        {
            return activeWeapon != null ? activeWeapon.CurrentBullets : 0;
        }

        public int GetMaxAmmo()
        {
            return activeWeapon != null ? activeWeapon.maxBullets : 0;
        }

        public bool IsRefilling()
        {
            return activeWeapon != null && activeWeapon.IsRefilling;
        }
    }
}
