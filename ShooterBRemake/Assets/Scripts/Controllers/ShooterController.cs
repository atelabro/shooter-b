using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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
        private Rifle rifleWeapon;
        private Cabirne cabirneWeapon;

        [Header("Prefabs")]
        public GameObject rifleBulletPrefab;
        public Sprite defaultRifleIcon;

        [Header("Weapon Icons")]
        public List<WeaponIconEntry> weaponIcons = new List<WeaponIconEntry>();

        private void Awake()
        {
            EnsureWeaponsCreated();
            if (activeWeapon == null)
                activeWeapon = rifleWeapon;
        }

        private void Start()
        {
            EnsureWeaponsCreated();
            if (activeWeapon == null)
                activeWeapon = rifleWeapon;

            StartCoroutine(LogInitialization());
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame && rifleWeapon != null)
            {
                activeWeapon = rifleWeapon;
                ApplyOrUpdateIcon(activeWeapon);
                Debug.Log("[SHOOTER] Switched weapon to Rifle");
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame && cabirneWeapon != null)
            {
                activeWeapon = cabirneWeapon;
                ApplyOrUpdateIcon(activeWeapon);
                Debug.Log("[SHOOTER] Switched weapon to Cabirne");
            }
        }

        private void EnsureWeaponsCreated()
        {
            if (rifleWeapon == null)
                rifleWeapon = CreateWeaponInstance<Rifle>("Rifle", Constants.WeaponType.Rifle);

            if (cabirneWeapon == null)
                cabirneWeapon = CreateWeaponInstance<Cabirne>("Cabirne", Constants.WeaponType.Cabirne);
        }

        private T CreateWeaponInstance<T>(string objectName, Constants.WeaponType expectedWeaponType) where T : Weapon
        {
            GameObject weaponObj = new GameObject(objectName);
            weaponObj.transform.SetParent(transform);
            T weapon = weaponObj.AddComponent<T>();
            weapon.weaponType = expectedWeaponType;

            if (rifleBulletPrefab != null && weapon.bulletPrefab == null)
            {
                weapon.bulletPrefab = rifleBulletPrefab;
            }

            if (expectedWeaponType == Constants.WeaponType.Rifle && defaultRifleIcon != null && weapon.weaponIcon == null)
            {
                weapon.weaponIcon = defaultRifleIcon;
            }

            ApplyOrUpdateIcon(weapon);
            return weapon;
        }

        private void EnsureActiveWeapon()
        {
            if (activeWeapon == null)
            {
                Debug.LogError("[SHOOTER] Failed to initialize active weapon.");
            }
            else
            {
                ApplyOrUpdateIcon(activeWeapon);
            }
        }

        private void ApplyOrUpdateIcon(Weapon weapon)
        {
            if (weapon == null)
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
