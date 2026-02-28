using UnityEngine;
using System.Collections.Generic;

namespace ShooterB
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Bullet : MonoBehaviour
    {
        [Header("Bullet Properties")]
        public float startRadius = 0.6f;
        public float secondRadius = 0.2f;
        public float effectiveRadius = 0.45f;
        public float baseSpeed = 35f;
        public float visualScaleMultiplier = 1f;

        protected Rigidbody2D rb;
        protected CircleCollider2D col;
        protected SpriteRenderer spriteRenderer;

        protected Constants.WeaponType firedByWeapon;
        protected Vector2 targetPosition;
        protected Vector2 direction;
        protected float initialDistance;
        protected float currentDistance;
        protected bool bangTriggered = false;
        protected bool isActive = false;
        protected GameObject poolSourcePrefab;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
            col.isTrigger = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = Constants.SORTING_LAYER_BULLETS;
            }
        }

        public virtual void Initialize(Vector2 target, Constants.WeaponType weaponType = Constants.WeaponType.Rifle)
        {
            firedByWeapon = weaponType;
            targetPosition = target;
            direction = (targetPosition - (Vector2)transform.position).normalized;
            initialDistance = Vector2.Distance(transform.position, targetPosition);
            currentDistance = initialDistance;
            bangTriggered = false;
            isActive = true;

            float visualScale = Mathf.Max(0.01f, visualScaleMultiplier);
            transform.localScale = Vector3.one * startRadius * 2f * visualScale;

            // Rotate sprite to face travel direction.
            // The sprite points from bottom-right to top-left (135 degrees),
            // so we subtract that offset to align with the actual direction.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 135f);

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            Debug.Log($"[BULLET] Initialized at {transform.position}, target: {targetPosition}, direction: {direction}, distance: {initialDistance}");
        }

        protected virtual void FixedUpdate()
        {
            if (!isActive)
            {
                return;
            }

            currentDistance = Vector2.Distance(transform.position, targetPosition);

            if (currentDistance < 0.5f && !bangTriggered)
            {
                transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
                TriggerBang();
            }

            if (bangTriggered)
            {
                CheckCollisions();
            }
            else
            {
                MoveBullet();
                UpdateSize();
            }
        }

        protected virtual void MoveBullet()
        {
            float frameDistance = baseSpeed * Time.fixedDeltaTime;

            if (currentDistance <= frameDistance)
            {
                transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                Vector2 velocity = direction * baseSpeed;
                rb.linearVelocity = velocity;
            }
        }

        protected virtual void UpdateSize()
        {
            if (initialDistance <= 0) return;

            float progress = 1f - (currentDistance / initialDistance);
            float currentRadius = Mathf.Lerp(startRadius, secondRadius, progress);
            float visualScale = Mathf.Max(0.01f, visualScaleMultiplier);
            transform.localScale = Vector3.one * currentRadius * 2f * visualScale;

            if (col != null)
            {
                col.radius = currentRadius / 2f;
            }
        }

        protected virtual void TriggerBang()
        {
            bangTriggered = true;
            rb.linearVelocity = Vector2.zero;

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            Debug.Log($"[BULLET] Bang triggered at {transform.position}");
        }

        protected virtual void CheckCollisions()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, effectiveRadius);
            HashSet<Duck> uniqueDucksHit = new HashSet<Duck>();

            foreach (Collider2D hit in hits)
            {
                Duck duck = hit.GetComponent<Duck>();
                if (duck != null)
                {
                    uniqueDucksHit.Add(duck);
                    duck.OnHit(firedByWeapon);
                    Debug.Log($"[BULLET] Hit duck at {hit.transform.position}");
                }
            }

            TriggerComboIfNeeded(uniqueDucksHit);

            Dispose();
        }

        private void TriggerComboIfNeeded(HashSet<Duck> uniqueDucksHit)
        {
            int killsFromSingleImpact = uniqueDucksHit != null ? uniqueDucksHit.Count : 0;
            if (killsFromSingleImpact < 2 || GameManager.Instance == null)
                return;

            Vector3 comboWorldPosition = GetComboWorldPosition(uniqueDucksHit);

            if (killsFromSingleImpact == 2)
            {
                GameManager.Instance.AddComboPoints(Constants.MultiKillType.DoubleKill, comboWorldPosition);
            }
            else if (killsFromSingleImpact == 3)
            {
                GameManager.Instance.AddComboPoints(Constants.MultiKillType.TripleKill, comboWorldPosition);
            }
            else
            {
                GameManager.Instance.AddComboPoints(Constants.MultiKillType.QuadraKill, comboWorldPosition);
            }
        }

        private Vector3 GetComboWorldPosition(HashSet<Duck> uniqueDucksHit)
        {
            if (uniqueDucksHit == null || uniqueDucksHit.Count == 0)
                return transform.position;

            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (Duck duck in uniqueDucksHit)
            {
                if (duck == null)
                    continue;

                sum += duck.transform.position;
                count++;
            }

            if (count == 0)
                return transform.position;

            return sum / count;
        }

        protected virtual void Dispose()
        {
            isActive = false;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            BulletPool.Return(poolSourcePrefab, gameObject);
        }

        public virtual void SetPoolSourcePrefab(GameObject sourcePrefab)
        {
            poolSourcePrefab = sourcePrefab;
        }

        private void OnDrawGizmos()
        {
            if (isActive)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, effectiveRadius);

                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }
    }
}
