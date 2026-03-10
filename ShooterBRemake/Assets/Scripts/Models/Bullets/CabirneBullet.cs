using UnityEngine;

namespace ShooterB
{
    public class CabirneBullet : Bullet
    {
        protected override void Awake()
        {
            startRadius = 0.55f;
            secondRadius = 0.18f;
            effectiveRadius = 0.54f;
            baseSpeed = 45f;

            base.Awake();

            GameLog.Log($"[CABIRNEBULLET] Initialized - Start: {startRadius}, End: {secondRadius}, Effective: {effectiveRadius}, Speed: {baseSpeed}");
        }
    }
}
