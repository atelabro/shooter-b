using UnityEngine;

namespace ShooterB
{
    public class TeslaGun : Weapon
    {
        protected override void Start()
        {
            weaponName = "TeslaGun";
            weaponType = Constants.WeaponType.TeslaGun;
            fireMode = Constants.WeaponFireMode.SingleTap;
            maxBullets = 5;
            damage = 1;
            fireDelay = 0.1f;
            refillDelay = 0.4f;

            if (bulletPrefab == null)
                GameLog.Error("[TESLA] bulletPrefab is not assigned. Assign Tesla bullet prefab via ShooterController or weapon prefab.");

            base.Start();

            GameLog.Log($"[TESLA] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
