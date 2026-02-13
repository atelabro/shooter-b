using UnityEngine;

namespace ShooterB
{
    public class RifleBullet : Bullet
    {
        protected override void Awake()
        {
            startRadius = 0.6f;
            secondRadius = 0.2f;
            effectiveRadius = 0.45f;
            baseSpeed = 50f;

            base.Awake();

            Debug.Log($"[RIFLEBULLET] Initialized - Start: {startRadius}, End: {secondRadius}, Effective: {effectiveRadius}, Speed: {baseSpeed}");
        }
    }
}
