using UnityEngine;

namespace ShooterB
{
    public class PiranhaBullet : Bullet
    {
        [Header("Piranha Variants")]
        public Sprite[] variantSprites;

        public override void Initialize(Vector2 target, Constants.WeaponType weaponType = Constants.WeaponType.PiranhaGun, int bulletDamage = 1)
        {
            ApplyRandomVariantSprite();
            base.Initialize(target, weaponType, bulletDamage);
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
