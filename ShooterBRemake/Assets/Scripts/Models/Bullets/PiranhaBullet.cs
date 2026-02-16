using UnityEngine;

namespace ShooterB
{
    public class PiranhaBullet : Bullet
    {
        [Header("Piranha Variants")]
        public Sprite[] variantSprites;

        protected override void Awake()
        {
            startRadius = 1.6f;
            secondRadius = 1.0f;
            effectiveRadius = 0.9f;
            baseSpeed = 16.67f;

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
    }
}
