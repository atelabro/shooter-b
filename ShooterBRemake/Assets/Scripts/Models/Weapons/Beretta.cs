using UnityEngine;

namespace ShooterB
{
    public class Beretta : Weapon
    {
        protected override void Start()
        {
            weaponName = "Beretta";
            weaponType = Constants.WeaponType.Beretta;
            fireMode = Constants.WeaponFireMode.HoldAutomatic;
            maxBullets = 14;
            fireDelay = 0.2f;
            refillDelay = 1.6f;

            if (bulletPrefab == null)
                Debug.LogError("[BERETTA] bulletPrefab is not assigned. Assign Beretta bullet prefab via ShooterController or weapon prefab.");

            base.Start();

            Debug.Log($"[BERETTA] Initialized - Bullets: {maxBullets}, Fire Delay: {fireDelay}s, Refill: {refillDelay}s");
        }
    }
}
