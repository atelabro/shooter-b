using UnityEngine;

namespace ShooterB
{
    public class PiranhaGun : Weapon
    {
        protected override void Start()
        {
            weaponName = "PiranhaGun";
            weaponType = Constants.WeaponType.PiranhaGun;
            fireMode = Constants.WeaponFireMode.SingleTap;
            maxBullets = 3;
            damage = 2;
            fireDelay = 0.9f;
            refillDelay = 1.4f;

            if (bulletPrefab == null)
                GameLog.Error("[PIRANHA] bulletPrefab is not assigned. Assign Piranha bullet prefab via ShooterController or weapon prefab.");

            base.Start();

            GameLog.Log($"[PIRANHA] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
