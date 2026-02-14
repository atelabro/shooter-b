using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterB
{
    public class ShooterController : MonoBehaviour
    {
        [Header("Weapons")]
        public Weapon activeWeapon;
        private Rifle rifleWeapon;
        private Cabirne cabirneWeapon;

        [Header("Prefabs")]
        public GameObject rifleBulletPrefab;
        public Sprite defaultRifleIcon;

        [Header("Weapon Prefabs")]
        public Weapon rifleWeaponPrefab;
        public Weapon cabirneWeaponPrefab;

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
                Debug.Log("[SHOOTER] Switched weapon to Rifle");
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame && cabirneWeapon != null)
            {
                activeWeapon = cabirneWeapon;
                Debug.Log("[SHOOTER] Switched weapon to Cabirne");
            }
        }

        private void EnsureWeaponsCreated()
        {
            if (rifleWeapon == null)
            {
                if (rifleWeaponPrefab != null)
                    rifleWeapon = CreateWeaponFromPrefab<Rifle>(rifleWeaponPrefab, "Rifle", Constants.WeaponType.Rifle);
                else
                    rifleWeapon = CreateWeaponInstance<Rifle>("Rifle", Constants.WeaponType.Rifle);
            }

            if (cabirneWeapon == null)
            {
                if (cabirneWeaponPrefab != null)
                    cabirneWeapon = CreateWeaponFromPrefab<Cabirne>(cabirneWeaponPrefab, "Cabirne", Constants.WeaponType.Cabirne);
                else
                    cabirneWeapon = CreateWeaponInstance<Cabirne>("Cabirne", Constants.WeaponType.Cabirne);
            }
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

            return weapon;
        }

        private T CreateWeaponFromPrefab<T>(Weapon prefab, string fallbackName, Constants.WeaponType expectedWeaponType) where T : Weapon
        {
            Weapon weaponInstance = Instantiate(prefab, transform);
            weaponInstance.name = fallbackName;

            T typedWeapon = weaponInstance as T;
            if (typedWeapon == null)
            {
                typedWeapon = weaponInstance.GetComponent<T>();
            }

            if (typedWeapon == null)
            {
                Debug.LogError($"[SHOOTER] Prefab '{prefab.name}' does not contain required component {typeof(T).Name}. Falling back to runtime-created weapon.");
                Destroy(weaponInstance.gameObject);
                return CreateWeaponInstance<T>(fallbackName, expectedWeaponType);
            }

            typedWeapon.weaponType = expectedWeaponType;
            return typedWeapon;
        }

        private void EnsureActiveWeapon()
        {
            if (activeWeapon == null)
            {
                Debug.LogError("[SHOOTER] Failed to initialize active weapon.");
            }
            else
            {
                if (activeWeapon.weaponType == Constants.WeaponType.Rifle && activeWeapon.weaponIcon == null && defaultRifleIcon != null)
                {
                    activeWeapon.weaponIcon = defaultRifleIcon;
                }
            }
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
