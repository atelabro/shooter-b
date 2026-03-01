using UnityEngine;

namespace ShooterB
{
    public class PiranhaBullet : Bullet
    {
        private const float LEGACY_PIRANHA_BULLET_HEIGHT_WORLD = 0.28f; // 28px at 100 PPU
        private const float LEGACY_PIRANHA_VISUAL_SCALE = 1.9f;

        [Header("Piranha Variants")]
        public Sprite[] variantSprites;

        protected override void Awake()
        {
            startRadius = 1.2f;
            secondRadius = 0.6f;
            effectiveRadius = 1.1f;
            baseSpeed = 16.67f;
            visualScaleMultiplier = ComputeNormalizedPiranhaVisualScale();

            base.Awake();
        }

        public override void Initialize(Vector2 target, Constants.WeaponType weaponType = Constants.WeaponType.PiranhaGun)
        {
            ApplyRandomVariantSprite();
            base.Initialize(target, weaponType);
        }

        private void ApplyRandomVariantSprite()
        {
            if (spriteRenderer == null || variantSprites == null || variantSprites.Length == 0)
                return;

            int index = Random.Range(0, variantSprites.Length);
            Sprite selected = variantSprites[index];
            if (selected != null)
                spriteRenderer.sprite = selected;
        }

        private float ComputeNormalizedPiranhaVisualScale()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            Sprite sprite = renderer != null ? renderer.sprite : null;
            if (sprite == null || sprite.pixelsPerUnit <= 0f)
                return LEGACY_PIRANHA_VISUAL_SCALE;

            float spriteHeightAtScaleOne = sprite.rect.height / sprite.pixelsPerUnit;
            if (spriteHeightAtScaleOne <= 0f)
                return LEGACY_PIRANHA_VISUAL_SCALE;

            return LEGACY_PIRANHA_VISUAL_SCALE * (LEGACY_PIRANHA_BULLET_HEIGHT_WORLD / spriteHeightAtScaleOne);
        }
    }
}
