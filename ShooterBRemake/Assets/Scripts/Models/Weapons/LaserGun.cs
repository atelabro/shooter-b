using UnityEngine;

namespace ShooterB
{
    public class LaserGun : Weapon
    {
        protected override void Start()
        {
            weaponName = "LaserGun";
            weaponType = Constants.WeaponType.LaserGun;
            fireMode = Constants.WeaponFireMode.SingleTap;
            damage = 3;

            // Legacy parity from ShooterBgame LaserGun.java defaults.
            maxBullets = 11;
            fireDelay = 0.26f;
            refillDelay = 0.83f;

            if (bulletPrefab == null)
                GameLog.Error("[LASER] bulletPrefab is not assigned. Assign Laser bullet prefab via ShooterController or weapon prefab.");

            base.Start();

            GameLog.Log($"[LASER] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
