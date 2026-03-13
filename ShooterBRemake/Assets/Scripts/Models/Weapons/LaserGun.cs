using UnityEngine;

namespace ShooterB
{
    public class LaserGun : Weapon
    {
        protected override void Start()
        {
            if (bulletPrefab == null)
                GameLog.Error("[LASER] bulletPrefab is not assigned. Assign Laser bullet prefab via ShooterController or weapon prefab.");

            base.Start();
        }
    }
}
