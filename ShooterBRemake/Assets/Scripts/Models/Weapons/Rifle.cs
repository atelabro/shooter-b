using UnityEngine;

namespace ShooterB
{
    public class Rifle : Weapon
    {
        protected override void Start()
        {
            if (bulletPrefab == null)
            {
                GameLog.Error("[RIFLE] bulletPrefab is not assigned. Assign it via ShooterController in the Inspector.");
            }

            base.Start();
        }
    }
}
