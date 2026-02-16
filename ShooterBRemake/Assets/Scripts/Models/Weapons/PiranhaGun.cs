using UnityEngine;

namespace ShooterB
{
    public class PiranhaGun : Weapon
    {
        protected override void Start()
        {
            weaponName = "PiranhaGun";
            weaponType = Constants.WeaponType.PiranhaGun;
            maxBullets = 3;
            fireDelay = 0.9f;
            refillDelay = 1.4f;

            if (bulletPrefab == null)
                Debug.LogError("[PIRANHA] bulletPrefab is not assigned. Assign Piranha bullet prefab via ShooterController or weapon prefab.");

            base.Start();

            Debug.Log($"[PIRANHA] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
