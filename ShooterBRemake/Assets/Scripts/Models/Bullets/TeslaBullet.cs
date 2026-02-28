using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public class TeslaBullet : Bullet
    {
        private const float LEGACY_TESLA_BULLET_HEIGHT_WORLD = 0.28f; // 28px at 100 PPU
        private const float LEGACY_TESLA_VISUAL_SCALE = 1.9f;

        [Header("Tesla Chain")]
        public float aoeRadius = 6.0f;
        public int chainCount = 2;
        private int remainingChains;
        private readonly HashSet<int> hitDuckIds = new HashSet<int>();

        protected override void Awake()
        {
            startRadius = 1.2f;
            secondRadius = 0.6f;
            effectiveRadius = 1.1f;
            baseSpeed = 60f;
            visualScaleMultiplier = ComputeNormalizedTeslaVisualScale();

            base.Awake();
        }

        public override void Initialize(Vector2 target, Constants.WeaponType weaponType = Constants.WeaponType.TeslaGun)
        {
            remainingChains = Mathf.Max(0, chainCount);
            hitDuckIds.Clear();
            base.Initialize(target, weaponType);

            // Tesla v2 sprite is oriented bottom-to-top, so it needs a different offset.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        public void ConfigureChain(int chainsRemaining, HashSet<int> alreadyHitDuckIds)
        {
            remainingChains = Mathf.Max(0, chainsRemaining);
            hitDuckIds.Clear();

            if (alreadyHitDuckIds == null)
                return;

            foreach (int duckId in alreadyHitDuckIds)
            {
                hitDuckIds.Add(duckId);
            }
        }

        protected override void CheckCollisions()
        {
            Duck primaryDuck = FindNearestUnhitDuck(transform.position, effectiveRadius, hitDuckIds);
            if (primaryDuck == null)
            {
                Dispose();
                return;
            }

            int primaryDuckId = primaryDuck.GetInstanceID();
            hitDuckIds.Add(primaryDuckId);

            Vector2 primaryHitPosition = primaryDuck.transform.position;
            primaryDuck.OnHit(firedByWeapon);
            Debug.Log($"[TESLA] Primary hit duck at {primaryHitPosition}");

            if (remainingChains > 0)
                SpawnChainBullet(primaryHitPosition, remainingChains - 1);

            Dispose();
        }

        private void SpawnChainBullet(Vector2 origin, int chainsLeftForNextBullet)
        {
            Duck nextDuck = FindNearestUnhitDuck(origin, aoeRadius, hitDuckIds);
            if (nextDuck == null)
                return;

            if (poolSourcePrefab == null)
                return;

            GameObject chainBulletObject = BulletPool.Get(poolSourcePrefab);
            if (chainBulletObject == null)
                return;

            chainBulletObject.transform.position = new Vector3(origin.x, origin.y, transform.position.z);

            TeslaBullet chainBullet = chainBulletObject.GetComponent<TeslaBullet>();
            if (chainBullet == null)
            {
                BulletPool.Return(poolSourcePrefab, chainBulletObject);
                return;
            }

            chainBullet.SetPoolSourcePrefab(poolSourcePrefab);
            chainBullet.Initialize(nextDuck.transform.position, firedByWeapon);
            chainBullet.ConfigureChain(chainsLeftForNextBullet, hitDuckIds);
        }

        private Duck FindNearestUnhitDuck(Vector2 origin, float searchRadius, HashSet<int> alreadyHitDuckIds)
        {
            Camera gameplayCamera = Camera.main;
            Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, searchRadius);
            Duck closest = null;
            float closestSqrDistance = float.MaxValue;

            foreach (Collider2D hit in nearby)
            {
                Duck duck = hit.GetComponent<Duck>();
                if (duck == null || alreadyHitDuckIds.Contains(duck.GetInstanceID()))
                    continue;
                if (gameplayCamera != null && !IsDuckVisibleOnScreen(gameplayCamera, duck.transform.position))
                    continue;

                float sqrDistance = ((Vector2)duck.transform.position - origin).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = duck;
                }
            }

            return closest;
        }

        private static bool IsDuckVisibleOnScreen(Camera cameraRef, Vector3 worldPosition)
        {
            Vector3 viewPoint = cameraRef.WorldToViewportPoint(worldPosition);
            if (viewPoint.z < 0f)
                return false;

            return viewPoint.x >= 0f && viewPoint.x <= 1f &&
                   viewPoint.y >= 0f && viewPoint.y <= 1f;
        }

        private float ComputeNormalizedTeslaVisualScale()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            Sprite sprite = renderer != null ? renderer.sprite : null;
            if (sprite == null || sprite.pixelsPerUnit <= 0f)
                return LEGACY_TESLA_VISUAL_SCALE;

            float spriteHeightAtScaleOne = sprite.rect.height / sprite.pixelsPerUnit;
            if (spriteHeightAtScaleOne <= 0f)
                return LEGACY_TESLA_VISUAL_SCALE;

            return LEGACY_TESLA_VISUAL_SCALE * (LEGACY_TESLA_BULLET_HEIGHT_WORLD / spriteHeightAtScaleOne);
        }
    }
}
