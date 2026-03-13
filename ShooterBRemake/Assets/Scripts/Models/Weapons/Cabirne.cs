using UnityEngine;

namespace ShooterB
{
    public class Cabirne : Weapon
    {
        protected override void Start()
        {
            if (bulletPrefab == null)
                GameLog.Error("[CABIRNE] bulletPrefab is not assigned. Assign Cabirne bullet prefab via ShooterController or weapon prefab.");

            base.Start();
        }
    }
}
