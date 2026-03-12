using UnityEngine;

namespace ShooterB
{
    public class Rifle : Weapon
    {
        protected override void Start()
        {
            weaponName = "Rifle";
            weaponType = Constants.WeaponType.Rifle;
            fireMode = Constants.WeaponFireMode.SingleTap;
            maxBullets = 2;
            damage = 2;
            fireDelay = 0.3f;
            refillDelay = 0.8f;

            if (bulletPrefab == null)
            {
                GameLog.Error("[RIFLE] bulletPrefab is not assigned. Assign it via ShooterController in the Inspector.");
            }

            base.Start();

            GameLog.Log($"[RIFLE] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
