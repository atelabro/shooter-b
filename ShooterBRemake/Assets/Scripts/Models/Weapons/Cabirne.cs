using UnityEngine;

namespace ShooterB
{
    public class Cabirne : Weapon
    {
        protected override void Start()
        {
            weaponName = "Cabirne";
            weaponType = Constants.WeaponType.Cabirne;
            maxBullets = 7;
            fireDelay = 0.2f;
            refillDelay = 0.6f;

            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/RifleBullet");
                if (bulletPrefab == null)
                {
                    Debug.LogWarning("[CABIRNE] RifleBullet prefab not found in Resources/Prefabs/. Assign bulletPrefab manually.");
                }
            }

            base.Start();

            Debug.Log($"[CABIRNE] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
