using UnityEngine;

namespace ShooterB
{
    public class Beretta : Weapon
    {
        protected override void Start()
        {
            if (bulletPrefab == null)
                GameLog.Error("[BERETTA] bulletPrefab is not assigned. Assign Beretta bullet prefab via ShooterController or weapon prefab.");

            base.Start();
        }
    }
}
