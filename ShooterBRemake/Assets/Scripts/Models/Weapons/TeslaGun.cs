using UnityEngine;

namespace ShooterB
{
    public class TeslaGun : Weapon
    {
        protected override void Start()
        {
            if (bulletPrefab == null)
                GameLog.Error("[TESLA] bulletPrefab is not assigned. Assign Tesla bullet prefab via ShooterController or weapon prefab.");

            base.Start();
        }
    }
}
