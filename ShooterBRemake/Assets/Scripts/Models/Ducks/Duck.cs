using UnityEngine;

namespace ShooterB
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Duck : MonoBehaviour
    {
        [Header("Duck Properties")]
        public Constants.DuckType duckType = Constants.DuckType.Type0;
        public int pointValue = 1;

        [Header("Movement")]
        public float speed = 10f;
        private Vector2 velocity;
        private Constants.MovementPattern currentPattern;
        private int patternChangeCounter = 0;
        private const int PATTERN_CHANGE_FRAMES = 20;

        [Header("Components")]
        private Rigidbody2D rb;
        private CircleCollider2D col;
        private SpriteRenderer spriteRenderer;
        private Animator animator;

        [Header("Boundaries")]
        private float screenTop;
        private float screenBottom;
        private float screenRight;
        private float screenLeft;

        private bool isDead = false;
        private Sprite aliveSprite;

        private const float DEAD_GRAVITY = 2f;
        private const float DEAD_CLEANUP_TIME = 2f;
        private const int DEATH_SPRITE_COUNT = 7;
        private const float DEATH_SPRITE_PPU = 32f;

        private static Sprite[] deathSprites;
        private static bool deathSpritesLoaded = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;

            LoadDeathSprites();
        }

        private static void LoadDeathSprites()
        {
            if (deathSpritesLoaded) return;

            Texture2D tex = Resources.Load<Texture2D>("allDeadDucks");
            if (tex == null)
            {
                Debug.LogError("[DUCK] allDeadDucks texture not found in Resources folder.");
                return;
            }

            deathSprites = new Sprite[DEATH_SPRITE_COUNT];
            int frameWidth = tex.width / DEATH_SPRITE_COUNT;
            int frameHeight = tex.height;

            for (int i = 0; i < DEATH_SPRITE_COUNT; i++)
            {
                Rect rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                deathSprites[i] = Sprite.Create(tex, rect, pivot, DEATH_SPRITE_PPU);
            }

            deathSpritesLoaded = true;
            Debug.Log($"[DUCK] Death sprites loaded: {DEATH_SPRITE_COUNT} frames ({frameWidth}x{frameHeight} each)");
        }

        public void Initialize(Constants.DuckType type, int difficulty, Vector2 startPosition, float boundTop, float boundBottom, float boundRight, float boundLeft)
        {
            duckType = type;
            pointValue = Constants.DuckPoints.GetPoints(type);
            speed = Constants.DuckSpeed.GetSpeed(difficulty);

            transform.position = new Vector3(startPosition.x, startPosition.y, -5);
            transform.localScale = Vector3.one * 1f;
            velocity = new Vector2(speed, 0);

            if (spriteRenderer != null)
            {
                if (aliveSprite != null)
                {
                    spriteRenderer.sprite = aliveSprite;
                }
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingLayerName = "Default";
                spriteRenderer.sortingOrder = 10;
                spriteRenderer.enabled = true;
            }

            screenTop = boundTop;
            screenBottom = boundBottom;
            screenRight = boundRight;
            screenLeft = boundLeft;

            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;

            if (col != null)
            {
                col.enabled = true;
            }

            if (animator != null)
            {
                animator.SetBool("IsDead", false);
                animator.SetBool("IsFlying", true);
            }

            SelectRandomPattern();
            isDead = false;
            gameObject.SetActive(true);
        }

        private void Start()
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null && aliveSprite == null)
            {
                aliveSprite = spriteRenderer.sprite;
            }
        }

        private void FixedUpdate()
        {
            if (isDead) return;

            patternChangeCounter++;
            if (patternChangeCounter >= PATTERN_CHANGE_FRAMES)
            {
                patternChangeCounter = 0;
                SelectRandomPattern();
            }

            ApplyMovement();
            EnforceBoundaries();

            rb.linearVelocity = velocity;
        }

        private void SelectRandomPattern()
        {
            int random = Random.Range(0, 11);

            if (random < 3)
                currentPattern = Constants.MovementPattern.GoTop;
            else if (random > 7)
                currentPattern = Constants.MovementPattern.GoBottom;
            else
                currentPattern = Constants.MovementPattern.GoStraight;
        }

        private void ApplyMovement()
        {
            switch (currentPattern)
            {
                case Constants.MovementPattern.GoStraight:
                    if (velocity.y > -0.5f && velocity.y < 0.5f)
                        velocity.y = 0;
                    else if (velocity.y >= 0.5f)
                        velocity.y -= 0.1f;
                    else if (velocity.y <= -0.5f)
                        velocity.y += 0.1f;
                    break;

                case Constants.MovementPattern.GoTop:
                    if (velocity.y >= -0.5f && velocity.y <= 0)
                        velocity.y += 0.05f;
                    else if (velocity.y > 0 && velocity.y <= 1f)
                        velocity.y += 0.1f;
                    else if (velocity.y > 1f)
                        velocity.y += 0.15f;
                    break;

                case Constants.MovementPattern.GoBottom:
                    if (velocity.y >= 0 && velocity.y <= 0.5f)
                        velocity.y -= 0.05f;
                    else if (velocity.y < 0 && velocity.y >= -1f)
                        velocity.y -= 0.1f;
                    else if (velocity.y < -1f)
                        velocity.y -= 0.15f;
                    break;
            }
        }

        private void EnforceBoundaries()
        {
            float posY = transform.position.y;
            float posX = transform.position.x;

            if (posY > screenTop)
            {
                transform.position = new Vector3(transform.position.x, screenTop, transform.position.z);
                currentPattern = Constants.MovementPattern.GoStraight;
                velocity.y = -0.5f;
            }
            else if (posY < screenBottom)
            {
                transform.position = new Vector3(transform.position.x, screenBottom, transform.position.z);
                currentPattern = Constants.MovementPattern.GoStraight;
                velocity.y = 0.5f;
            }

            if (posX > screenRight)
            {
                DuckPassedScreen();
            }
        }

        private void DuckPassedScreen()
        {
            GameManager.Instance.BirdPassed();
            ReturnToPool();
        }

        public void OnHit(Constants.WeaponType weaponType = Constants.WeaponType.Rifle)
        {
            if (isDead) return;

            isDead = true;
            GameManager.Instance.BirdKilled(duckType);

            // Store the alive sprite for reuse when this duck is recycled
            if (aliveSprite == null && spriteRenderer != null)
            {
                aliveSprite = spriteRenderer.sprite;
            }

            // Swap to weapon-specific death sprite
            Sprite deathSprite = GetDeathSprite(weaponType);
            if (deathSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = deathSprite;
            }

            // Keep current scale -- PPU handles sizing relative to the alive sprite

            // Disable collider so it cant be hit again
            if (col != null)
            {
                col.enabled = false;
            }

            // Stop animator so it doesnt override the death sprite
            if (animator != null)
            {
                animator.enabled = false;
            }

            // Enable gravity to make it fall
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = DEAD_GRAVITY;
            rb.linearVelocity = Vector2.zero;

            // Return to pool after falling off screen
            Invoke(nameof(ReturnToPool), DEAD_CLEANUP_TIME);

            Debug.Log($"Duck hit by {weaponType}! Type: {duckType}, Points: {pointValue}");
        }

        private Sprite GetDeathSprite(Constants.WeaponType weaponType)
        {
            if (deathSprites == null || deathSprites.Length == 0)
            {
                Debug.LogWarning("[DUCK] Death sprites not loaded.");
                return null;
            }

            // Frame index matches weapon enum order:
            // Rifle=0, Cabirne=1, Beretta=2, MrSulko=3, LaserGun=4, TeslaGun=5, PiranhaGun=6
            // Original mapping: Beretta=0, Cabirne=1, Rifle=2, LaserGun=3, PiranhaGun=4, MrSulko=5, TeslaGun=6
            int frameIndex;
            switch (weaponType)
            {
                case Constants.WeaponType.Beretta:    frameIndex = 0; break;
                case Constants.WeaponType.Cabirne:    frameIndex = 1; break;
                case Constants.WeaponType.Rifle:      frameIndex = 2; break;
                case Constants.WeaponType.LaserGun:   frameIndex = 3; break;
                case Constants.WeaponType.PiranhaGun: frameIndex = 4; break;
                case Constants.WeaponType.MrSulko:    frameIndex = 5; break;
                case Constants.WeaponType.TeslaGun:   frameIndex = 6; break;
                default:                              frameIndex = 2; break;
            }

            if (frameIndex >= 0 && frameIndex < deathSprites.Length)
            {
                return deathSprites[frameIndex];
            }

            return null;
        }

        private void ReturnToPool()
        {
            CancelInvoke(nameof(ReturnToPool));

            // Re-enable animator for next use
            if (animator != null)
            {
                animator.enabled = true;
            }

            if (transform.parent == null)
            {
                Debug.LogError("Duck has no parent! Cannot return to pool.");
                gameObject.SetActive(false);
                return;
            }

            DuckSpawner spawner = transform.parent.GetComponent<DuckSpawner>();
            if (spawner != null)
            {
                spawner.ReturnDuckToPool(gameObject);
            }
            else
            {
                Debug.LogWarning($"DuckSpawner not found on parent '{transform.parent.name}'. Deactivating duck.");
                gameObject.SetActive(false);
            }
        }

    }
}
