using UnityEngine;

namespace ShooterB
{
    public class TeslaGun : Weapon
    {
        protected override void Start()
        {
            weaponName = "TeslaGun";
            weaponType = Constants.WeaponType.TeslaGun;
            maxBullets = 5;
            fireDelay = 0.1f;
            refillDelay = 0.4f;

            if (bulletPrefab == null)
                Debug.LogError("[TESLA] bulletPrefab is not assigned. Assign Tesla bullet prefab via ShooterController or weapon prefab.");

            base.Start();

            Debug.Log($"[TESLA] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
