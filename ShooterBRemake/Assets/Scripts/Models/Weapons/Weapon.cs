using UnityEngine;
using System.Collections;

namespace ShooterB
{
    public abstract class Weapon : MonoBehaviour
    {
        [Header("Weapon Properties")]
        public string weaponName;
        public Constants.WeaponType weaponType;
        public int maxBullets;
        public float fireDelay;
        public float refillDelay;
        public Sprite weaponIcon;

        [Header("Bullet")]
        public GameObject bulletPrefab;

        protected int currentBullets;
        protected bool isRefilling = false;
        protected float lastFireTime = 0f;

        public int CurrentBullets => currentBullets;
        public bool IsRefilling => isRefilling;
        public bool CanShoot => currentBullets > 0 && !isRefilling && Time.time >= lastFireTime + fireDelay;

        protected virtual void Start()
        {
            currentBullets = maxBullets;
        }

        public virtual bool Shoot(Vector2 targetPosition)
        {
            if (!CanShoot)
                return false;

            if (bulletPrefab == null)
            {
                Debug.LogError($"[WEAPON] {weaponName} bulletPrefab is NULL! Cannot shoot.");
                return false;
            }

            currentBullets--;
            lastFireTime = Time.time;

            SpawnBullet(targetPosition);

            if (currentBullets <= 0)
            {
                StartRefill();
            }

            return true;
        }

        protected virtual void SpawnBullet(Vector2 targetPosition)
        {
            Debug.Log($"[WEAPON] SpawnBullet called - bulletPrefab is null? {bulletPrefab == null}");

            if (bulletPrefab == null)
            {
                Debug.LogError($"[WEAPON] {weaponName} bulletPrefab is NULL! Cannot spawn bullet. Make sure it's assigned in Inspector.");
                return;
            }

            Vector2 gunPosition = GetGunPosition();
            Debug.Log($"[WEAPON] Gun position calculated: {gunPosition}");

            GameObject bulletObj = Instantiate(bulletPrefab);
            bulletObj.transform.position = new Vector3(gunPosition.x, gunPosition.y, -5);
            Debug.Log($"[WEAPON] Bullet GameObject instantiated: {bulletObj.name}");

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            Debug.Log($"[WEAPON] Bullet component found? {bullet != null}");

            if (bullet != null)
            {
                bullet.Initialize(targetPosition, weaponType);
                Debug.Log($"[WEAPON] Bullet spawned at {gunPosition}, traveling to {targetPosition}");
            }
            else
            {
                Debug.LogError($"[WEAPON] No Bullet component on {bulletObj.name}! Check prefab has RifleBullet script.");
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
                isRefilling = true;
                StartCoroutine(RefillCoroutine());
            }
        }

        protected virtual IEnumerator RefillCoroutine()
        {
            yield return new WaitForSeconds(refillDelay);
            currentBullets = maxBullets;
            isRefilling = false;
            Debug.Log($"[WEAPON] {weaponName} refilled to {maxBullets} bullets");
        }

        public virtual void ResetAmmo()
        {
            currentBullets = maxBullets;
            isRefilling = false;
            StopAllCoroutines();
        }

        public virtual Sprite GetAmmoHudSprite()
        {
            if (bulletPrefab == null)
                return null;

            SpriteRenderer renderer = bulletPrefab.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }
    }
}
