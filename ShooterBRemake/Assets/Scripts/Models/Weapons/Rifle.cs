using UnityEngine;

namespace ShooterB
{
    public class Rifle : Weapon
    {
        protected override void Start()
        {
            weaponName = "Rifle";
            weaponType = Constants.WeaponType.Rifle;
            maxBullets = 2;
            fireDelay = 0.3f;
            refillDelay = 0.8f;

            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/RifleBullet");
                if (bulletPrefab == null)
                {
                    Debug.LogWarning("[RIFLE] RifleBullet prefab not found in Resources/Prefabs/. Create it or assign manually.");
                }
            }

            base.Start();

            Debug.Log($"[RIFLE] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
