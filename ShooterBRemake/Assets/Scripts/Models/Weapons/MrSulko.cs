using UnityEngine;

namespace ShooterB
{
    public class MrSulko : Weapon
    {
        protected override void Start()
        {
            weaponName = "MrSulko";
            weaponType = Constants.WeaponType.MrSulko;
            fireMode = Constants.WeaponFireMode.HoldAutomatic;
            maxBullets = 14;
            fireDelay = 0.1f;
            refillDelay = 0.72f;

            if (bulletPrefab == null)
                GameLog.Error("[MRSULKO] bulletPrefab is not assigned. Assign MrSulko bullet prefab via ShooterController or weapon prefab.");

            base.Start();

            GameLog.Log($"[MRSULKO] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
