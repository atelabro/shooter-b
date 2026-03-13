using UnityEngine;

namespace ShooterB
{
    public class PiranhaGun : Weapon
    {
        protected override void Start()
        {
            if (bulletPrefab == null)
                GameLog.Error("[PIRANHA] bulletPrefab is not assigned. Assign Piranha bullet prefab via ShooterController or weapon prefab.");

            base.Start();
        }
    }
}
