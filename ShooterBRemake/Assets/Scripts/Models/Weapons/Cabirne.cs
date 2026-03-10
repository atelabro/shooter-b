using UnityEngine;

namespace ShooterB
{
    public class Cabirne : Weapon
    {
        protected override void Start()
        {
            weaponName = "Cabirne";
            weaponType = Constants.WeaponType.Cabirne;
            fireMode = Constants.WeaponFireMode.SingleTap;
            maxBullets = 7;
            fireDelay = 0.2f;
            refillDelay = 0.6f;

            if (bulletPrefab == null)
                GameLog.Error("[CABIRNE] bulletPrefab is not assigned. Assign Cabirne bullet prefab via ShooterController or weapon prefab.");

            base.Start();

            GameLog.Log($"[CABIRNE] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
