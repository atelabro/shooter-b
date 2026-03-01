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
        private PiranhaGun piranhaWeapon;
        private TeslaGun teslaWeapon;
        private MrSulko mrSulkoWeapon;

        [Header("Prefabs")]
        public GameObject rifleBulletPrefab;
        public GameObject cabirneBulletPrefab;
        public GameObject piranhaBulletPrefab;
        public GameObject teslaBulletPrefab;
        public GameObject mrSulkoBulletPrefab;
        public Sprite defaultRifleIcon;
        public Sprite defaultPiranhaIcon;
        public Sprite defaultTeslaIcon;
        public Sprite defaultMrSulkoIcon;

        [Header("Weapon Prefabs")]
        public Weapon rifleWeaponPrefab;
        public Weapon cabirneWeaponPrefab;
        public Weapon piranhaWeaponPrefab;
        public Weapon teslaWeaponPrefab;
        public Weapon mrSulkoWeaponPrefab;

        private bool isFireHeld;
        private Vector2 heldTargetPosition;

        private void Awake()
        {
            EnsureWeaponsCreated();
            ApplyConfiguredStartingWeapon();
        }

        private void Start()
        {
            EnsureWeaponsCreated();
            RegisterWeaponDeathSprites();
            ApplyConfiguredStartingWeapon();

            EnsureActiveWeapon();

            StartCoroutine(LogInitialization());
        }

        private void Update()
        {
            UpdateAutomaticFire();

            if (Keyboard.current == null)
                return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame && piranhaWeapon != null)
            {
                SetActiveWeapon(piranhaWeapon, "PiranhaGun");
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame && cabirneWeapon != null)
            {
                SetActiveWeapon(cabirneWeapon, "Cabirne");
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame && rifleWeapon != null)
            {
                SetActiveWeapon(rifleWeapon, "Rifle");
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame && teslaWeapon != null)
            {
                SetActiveWeapon(teslaWeapon, "TeslaGun");
            }

            if (Keyboard.current.digit5Key.wasPressedThisFrame && mrSulkoWeapon != null)
            {
                SetActiveWeapon(mrSulkoWeapon, "MrSulko");
            }
        }

        private void EnsureWeaponsCreated()
        {
            if (rifleWeapon == null)
            {
                if (rifleWeaponPrefab != null)
                    rifleWeapon = CreateWeaponFromPrefab<Rifle>(rifleWeaponPrefab, "Rifle", Constants.WeaponType.Rifle, rifleBulletPrefab, defaultRifleIcon);
                else
                    rifleWeapon = CreateWeaponInstance<Rifle>("Rifle", Constants.WeaponType.Rifle, rifleBulletPrefab, defaultRifleIcon);
            }

            if (cabirneWeapon == null)
            {
                if (cabirneWeaponPrefab != null)
                    cabirneWeapon = CreateWeaponFromPrefab<Cabirne>(cabirneWeaponPrefab, "Cabirne", Constants.WeaponType.Cabirne, cabirneBulletPrefab, null);
                else
                    cabirneWeapon = CreateWeaponInstance<Cabirne>("Cabirne", Constants.WeaponType.Cabirne, cabirneBulletPrefab, null);
            }

            if (piranhaWeapon == null)
            {
                if (piranhaWeaponPrefab != null)
                    piranhaWeapon = CreateWeaponFromPrefab<PiranhaGun>(piranhaWeaponPrefab, "PiranhaGun", Constants.WeaponType.PiranhaGun, piranhaBulletPrefab, defaultPiranhaIcon);
                else
                    piranhaWeapon = CreateWeaponInstance<PiranhaGun>("PiranhaGun", Constants.WeaponType.PiranhaGun, piranhaBulletPrefab, defaultPiranhaIcon);
            }

            if (teslaWeapon == null)
            {
                if (teslaWeaponPrefab != null)
                    teslaWeapon = CreateWeaponFromPrefab<TeslaGun>(teslaWeaponPrefab, "TeslaGun", Constants.WeaponType.TeslaGun, teslaBulletPrefab, defaultTeslaIcon);
                else
                    teslaWeapon = CreateWeaponInstance<TeslaGun>("TeslaGun", Constants.WeaponType.TeslaGun, teslaBulletPrefab, defaultTeslaIcon);
            }

            if (mrSulkoWeapon == null)
            {
                GameObject resolvedMrSulkoBulletPrefab = ResolveMrSulkoBulletPrefab();
                Sprite resolvedMrSulkoIcon = defaultMrSulkoIcon != null ? defaultMrSulkoIcon : defaultRifleIcon;

                if (mrSulkoWeaponPrefab != null)
                    mrSulkoWeapon = CreateWeaponFromPrefab<MrSulko>(mrSulkoWeaponPrefab, "MrSulko", Constants.WeaponType.MrSulko, resolvedMrSulkoBulletPrefab, resolvedMrSulkoIcon);
                else
                    mrSulkoWeapon = CreateWeaponInstance<MrSulko>("MrSulko", Constants.WeaponType.MrSulko, resolvedMrSulkoBulletPrefab, resolvedMrSulkoIcon);
            }

            RegisterWeaponDeathSprites();
        }

        private T CreateWeaponInstance<T>(string objectName, Constants.WeaponType expectedWeaponType, GameObject defaultBulletPrefab, Sprite defaultIcon) where T : Weapon
        {
            GameObject weaponObj = new GameObject(objectName);
            weaponObj.transform.SetParent(transform);
            T weapon = weaponObj.AddComponent<T>();
            weapon.weaponType = expectedWeaponType;

            if (defaultBulletPrefab != null && weapon.bulletPrefab == null)
                weapon.bulletPrefab = defaultBulletPrefab;

            if (defaultIcon != null && weapon.weaponIcon == null)
                weapon.weaponIcon = defaultIcon;

            return weapon;
        }

        private T CreateWeaponFromPrefab<T>(Weapon prefab, string fallbackName, Constants.WeaponType expectedWeaponType, GameObject defaultBulletPrefab, Sprite defaultIcon) where T : Weapon
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
                return CreateWeaponInstance<T>(fallbackName, expectedWeaponType, defaultBulletPrefab, defaultIcon);
            }

            typedWeapon.weaponType = expectedWeaponType;
            if (typedWeapon.bulletPrefab == null && defaultBulletPrefab != null)
                typedWeapon.bulletPrefab = defaultBulletPrefab;
            if (typedWeapon.weaponIcon == null && defaultIcon != null)
                typedWeapon.weaponIcon = defaultIcon;

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

                if (activeWeapon.weaponType == Constants.WeaponType.MrSulko && activeWeapon.weaponIcon == null && defaultMrSulkoIcon != null)
                {
                    activeWeapon.weaponIcon = defaultMrSulkoIcon;
                }
            }
        }

        private void RegisterWeaponDeathSprites()
        {
            if (rifleWeapon != null)
                rifleWeapon.RegisterConfiguredDeathSprite();

            if (cabirneWeapon != null)
                cabirneWeapon.RegisterConfiguredDeathSprite();

            if (piranhaWeapon != null)
                piranhaWeapon.RegisterConfiguredDeathSprite();

            if (teslaWeapon != null)
                teslaWeapon.RegisterConfiguredDeathSprite();

            if (mrSulkoWeapon != null)
                mrSulkoWeapon.RegisterConfiguredDeathSprite();
        }

        private System.Collections.IEnumerator LogInitialization()
        {
            yield return null;
            Debug.Log($"[SHOOTER] ShooterController initialized with weapon: {activeWeapon?.weaponName}");
        }

        public void Shoot(Vector2 targetPosition)
        {
            TryShoot(targetPosition, true);
        }

        public void BeginFire(Vector2 targetPosition)
        {
            isFireHeld = true;
            heldTargetPosition = targetPosition;

            if (activeWeapon == null)
            {
                Debug.LogWarning("[SHOOTER] No active weapon!");
                return;
            }

            TryShoot(targetPosition, true);
        }

        public void UpdateFire(Vector2 targetPosition)
        {
            if (!isFireHeld)
                return;

            heldTargetPosition = targetPosition;
        }

        public void EndFire()
        {
            isFireHeld = false;
        }

        private void UpdateAutomaticFire()
        {
            if (!isFireHeld || activeWeapon == null || !activeWeapon.CanAutoFire)
                return;

            TryShoot(heldTargetPosition, false);
        }

        private void TryShoot(Vector2 targetPosition, bool logFailure)
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
                if (logFailure)
                    Debug.Log($"[SHOOTER] Cannot shoot - Ammo: {activeWeapon.CurrentBullets}, Refilling: {activeWeapon.IsRefilling}");
            }
        }

        private void SetActiveWeapon(Weapon weapon, string weaponName)
        {
            activeWeapon = weapon;
            Debug.Log($"[SHOOTER] Switched weapon to {weaponName}");
        }

        public bool SetActiveWeaponByType(Constants.WeaponType weaponType)
        {
            Weapon weapon = GetWeaponByType(weaponType);
            if (weapon == null)
            {
                Debug.LogWarning($"[SHOOTER] Requested weapon '{weaponType}' is unavailable in this scene setup.");
                return false;
            }

            SetActiveWeapon(weapon, weapon.weaponName);
            return true;
        }

        private Weapon GetWeaponByType(Constants.WeaponType weaponType)
        {
            switch (weaponType)
            {
                case Constants.WeaponType.Rifle:
                    return rifleWeapon;
                case Constants.WeaponType.Cabirne:
                    return cabirneWeapon;
                case Constants.WeaponType.PiranhaGun:
                    return piranhaWeapon;
                case Constants.WeaponType.TeslaGun:
                    return teslaWeapon;
                case Constants.WeaponType.MrSulko:
                    return mrSulkoWeapon;
                default:
                    return null;
            }
        }

        private void ApplyConfiguredStartingWeapon()
        {
            if (GameManager.Instance == null)
            {
                if (activeWeapon == null)
                    activeWeapon = rifleWeapon;
                return;
            }

            if (!SetActiveWeaponByType(GameManager.Instance.SelectedWeaponType))
                activeWeapon = rifleWeapon;
        }

        private GameObject ResolveMrSulkoBulletPrefab()
        {
            if (mrSulkoBulletPrefab != null)
                return mrSulkoBulletPrefab;

            if (cabirneBulletPrefab != null)
            {
                Debug.LogWarning("[SHOOTER] mrSulkoBulletPrefab is not assigned. Falling back to Cabirne bullet prefab.");
                return cabirneBulletPrefab;
            }

            if (rifleBulletPrefab != null)
            {
                Debug.LogWarning("[SHOOTER] mrSulkoBulletPrefab is not assigned. Falling back to Rifle bullet prefab.");
                return rifleBulletPrefab;
            }

            Debug.LogWarning("[SHOOTER] mrSulkoBulletPrefab is not assigned and no fallback bullet prefab is available.");
            return null;
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

        public Sprite GetActiveWeaponAmmoSprite()
        {
            return activeWeapon != null ? activeWeapon.GetAmmoHudSprite() : null;
        }

        public Constants.WeaponType? GetActiveWeaponType()
        {
            return activeWeapon != null ? activeWeapon.weaponType : (Constants.WeaponType?)null;
        }
    }
}
