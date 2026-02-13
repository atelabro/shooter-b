using UnityEngine;

namespace ShooterB
{
    public class ShooterController : MonoBehaviour
    {
        [Header("Weapons")]
        public Weapon activeWeapon;

        [Header("Prefabs")]
        public GameObject rifleBulletPrefab;
        public Sprite defaultRifleIcon;

        private void Start()
        {
            if (activeWeapon == null)
            {
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
            }

            StartCoroutine(LogInitialization());
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
