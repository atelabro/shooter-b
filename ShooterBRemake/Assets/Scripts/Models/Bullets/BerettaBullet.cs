using UnityEngine;

namespace ShooterB
{
    public class BerettaBullet : Bullet
    {
        protected override void Awake()
        {
            startRadius = 0.3f;
            secondRadius = 0.12f;
            effectiveRadius = 0.24f;
            baseSpeed = 50f;

            base.Awake();
        }
    }
}
