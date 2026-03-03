using UnityEngine;

namespace ShooterB
{
    public class MrSulkoBullet : Bullet
    {
        protected override void Awake()
        {
            startRadius = 0.55f;
            secondRadius = 0.2f;
            effectiveRadius = 0.54f;
            baseSpeed = 58f;

            base.Awake();
        }
    }
}
