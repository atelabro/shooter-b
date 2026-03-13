using UnityEngine;

namespace ShooterB
{
    public class MrSulko : Weapon
    {
        protected override void Start()
        {
            if (bulletPrefab == null)
                GameLog.Error("[MRSULKO] bulletPrefab is not assigned. Assign MrSulko bullet prefab via ShooterController or weapon prefab.");

            base.Start();
        }
    }
}
