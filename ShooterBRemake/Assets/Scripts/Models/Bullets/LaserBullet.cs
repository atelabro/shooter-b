using UnityEngine;

namespace ShooterB
{
    public class LaserBullet : Bullet
    {
        private const float LEGACY_LASER_BULLET_HEIGHT_WORLD = 0.28f; // 28px at 100 PPU
        private const float LEGACY_LASER_VISUAL_SCALE = 1.9f;

        protected override void Awake()
        {
            startRadius = 1.2f;
            secondRadius = 0.6f;
            effectiveRadius = 1.485f;
            baseSpeed = 50f;
            visualScaleMultiplier = ComputeNormalizedLaserVisualScale();

            base.Awake();
        }

        public override void Initialize(Vector2 target, Constants.WeaponType weaponType = Constants.WeaponType.LaserGun)
        {
            base.Initialize(target, weaponType);
        }

        private float ComputeNormalizedLaserVisualScale()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            Sprite sprite = renderer != null ? renderer.sprite : null;
            if (sprite == null || sprite.pixelsPerUnit <= 0f)
                return LEGACY_LASER_VISUAL_SCALE;

            float spriteHeightAtScaleOne = sprite.rect.height / sprite.pixelsPerUnit;
            if (spriteHeightAtScaleOne <= 0f)
                return LEGACY_LASER_VISUAL_SCALE;

            return LEGACY_LASER_VISUAL_SCALE * (LEGACY_LASER_BULLET_HEIGHT_WORLD / spriteHeightAtScaleOne);
        }
    }
}
