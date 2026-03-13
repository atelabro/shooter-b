using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ShooterB
{
    public abstract class Weapon : MonoBehaviour
    {
        [Header("Weapon Properties")]
        public string weaponName;
        public Constants.WeaponType weaponType;
        public Constants.WeaponFireMode fireMode = Constants.WeaponFireMode.SingleTap;
        public int maxBullets;
        [Min(1)] public int damage = 1;
        [Min(0)] public int startModalAmmoBonus = 2;
        public float fireDelay;
        public float refillDelay;
        public Sprite weaponIcon;
        public Sprite ammoHudSprite;

        [Header("Bullet")]
        public GameObject bulletPrefab;

        [Header("Death Sprite")]
        public Sprite deathSprite;

        protected int currentBullets;
        protected bool isRefilling = false;
        protected float lastFireTime = 0f;

        private static readonly Dictionary<Constants.WeaponType, Sprite> deathSpritesByWeaponType =
            new Dictionary<Constants.WeaponType, Sprite>();

        public int CurrentBullets => currentBullets;
        public bool IsRefilling => isRefilling;
        public bool CanShoot => currentBullets > 0 && !isRefilling && Time.time >= lastFireTime + fireDelay;
        public bool CanAutoFire => fireMode == Constants.WeaponFireMode.HoldAutomatic;

        protected virtual void Start()
        {
            ValidateConfiguredValues();
            currentBullets = maxBullets;
            RegisterConfiguredDeathSprite();
            GameLog.Log($"[WEAPON] {weaponName} initialized from prefab config - Bullets: {maxBullets}, Damage: {damage}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }

        protected virtual void ValidateConfiguredValues()
        {
            if (string.IsNullOrWhiteSpace(weaponName))
            {
                GameLog.Warning($"[WEAPON] {GetType().Name} has no weaponName configured on its prefab.");
            }

            if (maxBullets <= 0)
            {
                GameLog.Warning($"[WEAPON] {weaponName} has invalid maxBullets ({maxBullets}). Clamping to 1.");
                maxBullets = 1;
            }

            if (damage <= 0)
            {
                GameLog.Warning($"[WEAPON] {weaponName} has invalid damage ({damage}). Clamping to 1.");
                damage = 1;
            }

            if (fireDelay < 0f)
            {
                GameLog.Warning($"[WEAPON] {weaponName} has invalid fireDelay ({fireDelay}). Clamping to 0.");
                fireDelay = 0f;
            }

            if (refillDelay < 0f)
            {
                GameLog.Warning($"[WEAPON] {weaponName} has invalid refillDelay ({refillDelay}). Clamping to 0.");
                refillDelay = 0f;
            }
        }

        public void RegisterConfiguredDeathSprite()
        {
            RegisterDeathSprite(weaponType, deathSprite);
        }

        public static void RegisterDeathSprite(Constants.WeaponType weaponType, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            deathSpritesByWeaponType[weaponType] = sprite;
        }

        public static Sprite GetRegisteredDeathSprite(Constants.WeaponType weaponType)
        {
            deathSpritesByWeaponType.TryGetValue(weaponType, out Sprite sprite);
            return sprite;
        }

        public virtual bool Shoot(Vector2 targetPosition)
        {
            if (!CanShoot)
                return false;

            if (bulletPrefab == null)
            {
                GameLog.Error($"[WEAPON] {weaponName} bulletPrefab is NULL! Cannot shoot.");
                return false;
            }

            currentBullets--;
            lastFireTime = Time.time;

            WeaponShootSoundManager.Instance.PlayShoot(weaponType);
            SpawnBullet(targetPosition);

            if (currentBullets <= 0)
            {
                StartRefill();
            }

            return true;
        }

        protected virtual void SpawnBullet(Vector2 targetPosition)
        {
            GameLog.Log($"[WEAPON] SpawnBullet called - bulletPrefab is null? {bulletPrefab == null}");

            if (bulletPrefab == null)
            {
                GameLog.Error($"[WEAPON] {weaponName} bulletPrefab is NULL! Cannot spawn bullet. Make sure it's assigned in Inspector.");
                return;
            }

            Vector2 gunPosition = GetGunPosition();
            GameLog.Log($"[WEAPON] Gun position calculated: {gunPosition}");

            GameObject bulletObj = BulletPool.Get(bulletPrefab);
            bulletObj.transform.position = new Vector3(gunPosition.x, gunPosition.y, -5);
            GameLog.Log($"[WEAPON] Bullet GameObject instantiated: {bulletObj.name}");

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            GameLog.Log($"[WEAPON] Bullet component found? {bullet != null}");

            if (bullet != null)
            {
                bullet.SetPoolSourcePrefab(bulletPrefab);
                bullet.Initialize(targetPosition, weaponType, damage);
                GameLog.Log($"[WEAPON] Bullet spawned at {gunPosition}, traveling to {targetPosition}");
            }
            else
            {
                GameLog.Error($"[WEAPON] No Bullet component on {bulletObj.name}! Check prefab has RifleBullet script.");
            }
        }

        protected virtual Vector2 GetGunPosition()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                float distanceFromCamera = Mathf.Abs(cam.transform.position.z - (-5));
                Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(1, 0, distanceFromCamera));
                return new Vector2(bottomRight.x - 1f, bottomRight.y + 1f);
            }

            return new Vector2(8, -4);
        }

        protected virtual void StartRefill()
        {
            if (!isRefilling)
            {
                WeaponReloadSoundManager.Instance.PlayReload(weaponType);
                isRefilling = true;
                StartCoroutine(RefillCoroutine());
            }
        }

        protected virtual IEnumerator RefillCoroutine()
        {
            yield return new WaitForSeconds(refillDelay);
            currentBullets = maxBullets;
            isRefilling = false;
            GameLog.Log($"[WEAPON] {weaponName} refilled to {maxBullets} bullets");
        }

        public virtual void ResetAmmo()
        {
            currentBullets = maxBullets;
            isRefilling = false;
            StopAllCoroutines();
        }

        public virtual Sprite GetAmmoHudSprite()
        {
            if (ammoHudSprite != null)
                return ammoHudSprite;

            if (bulletPrefab == null)
                return null;

            SpriteRenderer renderer = bulletPrefab.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }
    }
}
