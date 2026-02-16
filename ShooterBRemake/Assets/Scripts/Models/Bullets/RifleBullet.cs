using UnityEngine;

namespace ShooterB
{
    public class RifleBullet : Bullet
    {
        protected override void Awake()
        {
            startRadius = 0.2f;
            secondRadius = 0.7f;
            effectiveRadius = 0.9f;
            baseSpeed = 50f;

            base.Awake();

            Debug.Log($"[RIFLEBULLET] Initialized - Start: {startRadius}, End: {secondRadius}, Effective: {effectiveRadius}, Speed: {baseSpeed}");
        }
    }
}
