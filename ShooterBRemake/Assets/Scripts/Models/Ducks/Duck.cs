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
        private Constants.DuckStartLane currentStartLane;
        private Constants.DuckPathProjection currentPathProjection;
        private Vector2 bezierP0, bezierP1, bezierP2, bezierP3;
        private float pathProgress;
        private int patternChangeCounter = 0;
        private const int PATTERN_CHANGE_FRAMES = 20;

        [Header("Visual Normalization")]
        [SerializeField] private float targetAliveHeightWorld = 1.8f;
        [SerializeField] private float type0SizeMultiplier = 1f;
        [SerializeField] private float type1SizeMultiplier = 1f;
        [SerializeField] private float type2SizeMultiplier = 1f;
        [SerializeField] private float type4SizeMultiplier = 1f;
        [SerializeField] private float type5SizeMultiplier = 1f;
        [SerializeField] private float mkArcherSizeMultiplier = 1f;
        [SerializeField] private float mkVojvodaSizeMultiplier = 1f;
        [SerializeField] private float frenchRevolutionarySizeMultiplier = 1f;
        [SerializeField] private float frenchNapoleonSizeMultiplier = 1f;
        [SerializeField] private float frenchArtistSizeMultiplier = 0.88f;
        [SerializeField] private float britishRedcoatSizeMultiplier = 1f;
        [SerializeField] private float britishPoliceSizeMultiplier = 1f;
        [SerializeField] private float britishPunkSizeMultiplier = 1f;

        [Header("Hitbox Normalization")]
        [SerializeField] private float targetHitRadiusWorld = 0.45f;
        [SerializeField] private float deathSpriteScaleMultiplier = 1.25f;
        [SerializeField] private float deathSpriteDelay = 0.08f;

        [Header("Components")]
        private Rigidbody2D rb;
        private CircleCollider2D col;
        private SpriteRenderer spriteRenderer;
        private Animator animator;

        [Header("Hit Puff")]
        [SerializeField] private SpriteRenderer hitPuffRenderer;
        [SerializeField] private Vector2 hitPuffOffsetNormalized = new Vector2(0f, 0.05f);
        [SerializeField] private float hitPuffWidthMultiplier = 2.5f;
        [SerializeField] private float hitPuffHeightMultiplier = 2.5f;
        [SerializeField] private int hitPuffSortingOffset = 1;

        [Header("Debug")]
        [SerializeField] private bool keepDeadDuckForDebug;
        [SerializeField] private bool freezeDeadDuckForDebug = true;

        [Header("Boundaries")]
        private float screenTop;
        private float screenBottom;
        private float screenRight;
        private float screenLeft;

        private float movementWeightGoStraight = 0.4f;
        private float movementWeightGoTop = 0.3f;
        private float movementWeightGoBottom = 0.3f;

        private bool isDead = false;
        private Sprite aliveSprite;
        private Sprite[] aliveFrames;
        private int aliveFrameIndex;
        private float aliveFrameTimer;
        private const float ALIVE_ANIMATION_FPS = 12f;

        private const float DEAD_GRAVITY = 2f;
        private const float DEAD_CLEANUP_TIME = 2f;
        private Constants.WeaponType pendingDeathWeaponType = Constants.WeaponType.Rifle;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public void Initialize(
            Constants.DuckType type, int difficulty, Vector2 startPosition,
            float boundTop, float boundBottom, float boundRight, float boundLeft,
            Sprite[] typeAliveFrames,
            Constants.DuckStartLane startLane = Constants.DuckStartLane.Lane5,
            Constants.DuckPathProjection pathProjection = Constants.DuckPathProjection.Random,
            float goStraightWeight = 0.4f, float goTopWeight = 0.3f, float goBottomWeight = 0.3f)
        {
            duckType = type;
            pointValue = Constants.DuckPoints.GetPoints(type);
            speed = Constants.DuckSpeed.GetSpeed(difficulty);
            movementWeightGoStraight = goStraightWeight;
            movementWeightGoTop = goTopWeight;
            movementWeightGoBottom = goBottomWeight;
            if (GameManager.Instance.CurrentGameMode == Constants.GameMode.Arcade && GameManager.Instance.ArcadeVeryHardMode)
                speed *= Constants.DuckSpeed.ARCADE_VERY_HARD_MULTIPLIER;
            aliveFrames = typeAliveFrames;
            aliveFrameIndex = 0;
            aliveFrameTimer = 0f;

            if (spriteRenderer != null)
            {
                if (aliveFrames != null && aliveFrames.Length > 0)
                {
                    spriteRenderer.sprite = aliveFrames[0];
                    aliveSprite = aliveFrames[0];
                }
                else if (aliveSprite != null)
                {
                    spriteRenderer.sprite = aliveSprite;
                }
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingLayerName = "Default";
                spriteRenderer.sortingOrder = 10;
                spriteRenderer.enabled = true;
            }

            ApplyNormalizedScale();
            ApplyNormalizedHitbox();

            screenTop = boundTop;
            screenBottom = boundBottom;
            screenRight = boundRight;
            screenLeft = boundLeft;
            currentStartLane = SanitizeLane(startLane);
            currentPathProjection = pathProjection;
            pathProgress = 0f;
            patternChangeCounter = 0;

            if (currentPathProjection == Constants.DuckPathProjection.Straight)
            {
                startPosition = new Vector2(screenLeft, GetLaneCenterY(currentStartLane));
            }
            else if (IsBezierPath(currentPathProjection))
            {
                ConfigureBezierControlPoints(currentPathProjection);
                startPosition = bezierP0;
            }
            else if (IsDiagonalPath(currentPathProjection))
            {
                float safeHeight = screenTop - screenBottom;
                float margin = safeHeight * 0.05f;
                if (currentPathProjection == Constants.DuckPathProjection.DiagonalRise)
                    startPosition = new Vector2(screenLeft, screenBottom + margin);
                else
                    startPosition = new Vector2(screenLeft, screenTop - margin);
            }
            else if (IsSinWavePath(currentPathProjection))
            {
                startPosition = new Vector2(screenLeft, GetSinWaveCenterY());
            }
            else if (IsZigZagPath(currentPathProjection))
            {
                startPosition = new Vector2(screenLeft, GetZigZagStartY());
            }
            else if (IsBounce(currentPathProjection))
            {
                startPosition = new Vector2(screenLeft, GetLaneCenterY(currentStartLane));
            }
            else if (IsDiagonalVPath(currentPathProjection))
            {
                float safeHeight = screenTop - screenBottom;
                float margin = safeHeight * 0.05f;
                startPosition = currentPathProjection == Constants.DuckPathProjection.DiagonalV
                    ? new Vector2(screenLeft, screenBottom + margin)
                    : new Vector2(screenLeft, screenTop - margin);
            }

            if (spriteRenderer != null)
                spriteRenderer.flipX = false;

            transform.position = new Vector3(startPosition.x, startPosition.y, -5);
            velocity = new Vector2(speed, 0f);

            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;

            if (col != null)
            {
                col.enabled = true;
            }

            if (animator != null)
            {
                animator.enabled = false;
            }

            ResetHitPuff();

            if (currentPathProjection == Constants.DuckPathProjection.Random)
            {
                SelectRandomPattern();
            }
            isDead = false;
            gameObject.SetActive(true);
        }

        private void ApplyNormalizedScale()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                transform.localScale = Vector3.one;
                return;
            }

            float spriteHeightAtScaleOne = spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit;
            if (spriteHeightAtScaleOne <= 0f)
            {
                transform.localScale = Vector3.one * GetTypeSizeMultiplier(duckType);
                return;
            }

            float normalizedScale = (targetAliveHeightWorld / spriteHeightAtScaleOne) * GetTypeSizeMultiplier(duckType);
            transform.localScale = Vector3.one * normalizedScale;
        }

        private float GetTypeSizeMultiplier(Constants.DuckType type)
        {
            switch (type)
            {
                case Constants.DuckType.Type0: return type0SizeMultiplier;
                case Constants.DuckType.Type1: return type1SizeMultiplier;
                case Constants.DuckType.Type2: return type2SizeMultiplier;
                case Constants.DuckType.Type4: return type4SizeMultiplier;
                case Constants.DuckType.MK_PHALARX: return type5SizeMultiplier;
                case Constants.DuckType.MK_ARCHER: return mkArcherSizeMultiplier;
                case Constants.DuckType.MK_VOJVODA: return mkVojvodaSizeMultiplier;
                case Constants.DuckType.FRENCH_REVOLUTIONARY: return frenchRevolutionarySizeMultiplier;
                case Constants.DuckType.FRENCH_NAPOLEON: return frenchNapoleonSizeMultiplier;
                case Constants.DuckType.FRENCH_ARTIST: return frenchArtistSizeMultiplier;
                case Constants.DuckType.FRENCH_MUSKETEER: return frenchNapoleonSizeMultiplier;
                case Constants.DuckType.BRITISH_REDCOAT: return britishRedcoatSizeMultiplier;
                case Constants.DuckType.BRITISH_POLICE: return britishPoliceSizeMultiplier;
                case Constants.DuckType.BRITISH_PUNK: return britishPunkSizeMultiplier;
                case Constants.DuckType.USA_POLICE: return britishPoliceSizeMultiplier;
                case Constants.DuckType.USA_WORKER: return britishPoliceSizeMultiplier;
                case Constants.DuckType.USA_BUSINESS: return britishPoliceSizeMultiplier;
                case Constants.DuckType.JAPANESE_SAMURAI: return britishPunkSizeMultiplier;
                case Constants.DuckType.JAPANESE_STRAW_DUCK: return britishPunkSizeMultiplier;
                case Constants.DuckType.JAPANESE_KIMONO_DUCK: return britishPunkSizeMultiplier;
                default: return 1f;
            }
        }

        private void ApplyNormalizedHitbox()
        {
            if (col == null)
            {
                return;
            }

            float uniformScale = Mathf.Abs(transform.localScale.x);
            if (uniformScale < 0.0001f)
            {
                col.radius = targetHitRadiusWorld;
                return;
            }

            col.radius = targetHitRadiusWorld / uniformScale;
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

            AnimateAliveSprite();

            if (currentPathProjection == Constants.DuckPathProjection.Random)
            {
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
            else if (currentPathProjection == Constants.DuckPathProjection.Straight)
            {
                velocity.x = speed;
                velocity.y = 0f;
                rb.linearVelocity = velocity;

                if (transform.position.x > screenRight)
                    DuckPassedScreen();
            }
            else if (IsDiagonalPath(currentPathProjection))
            {
                AdvanceDiagonalPath();
            }
            else if (IsSinWavePath(currentPathProjection))
            {
                AdvanceSinWavePath();
            }
            else if (IsZigZagPath(currentPathProjection))
            {
                AdvanceZigZagPath();
            }
            else if (IsBounce(currentPathProjection))
            {
                AdvanceBouncePath();
            }
            else if (IsDiagonalVPath(currentPathProjection))
            {
                AdvanceDiagonalVPath();
            }
            else
            {
                AdvanceBezierPath();
            }
        }

        private Constants.DuckStartLane SanitizeLane(Constants.DuckStartLane lane)
        {
            int value = (int)lane;
            if (value < 1 || value > 9)
                return Constants.DuckStartLane.Lane5;
            return lane;
        }

        private float GetLaneCenterY(Constants.DuckStartLane lane)
        {
            int laneIndex = Mathf.Clamp((int)lane, 1, 9) - 1;
            float laneHeight = (screenTop - screenBottom) / 9f;
            return screenBottom + laneHeight * (laneIndex + 0.5f);
        }

        private bool IsBezierPath(Constants.DuckPathProjection p)
            => p == Constants.DuckPathProjection.BezierMountain || p == Constants.DuckPathProjection.BezierValley;

        private bool IsDiagonalPath(Constants.DuckPathProjection p)
            => p == Constants.DuckPathProjection.DiagonalRise || p == Constants.DuckPathProjection.DiagonalFall;

        private bool IsSinWavePath(Constants.DuckPathProjection p)
            => p == Constants.DuckPathProjection.SinWave ||
               p == Constants.DuckPathProjection.SinWaveBig ||
               p == Constants.DuckPathProjection.SinWaveStartDown;

        private bool IsZigZagPath(Constants.DuckPathProjection p)
            => p == Constants.DuckPathProjection.ZigZagTopFirst || p == Constants.DuckPathProjection.ZigZagBottomFirst;

        private bool IsBounce(Constants.DuckPathProjection p)
            => p == Constants.DuckPathProjection.BounceMid;

        private bool IsDiagonalVPath(Constants.DuckPathProjection p)
            => p == Constants.DuckPathProjection.DiagonalV || p == Constants.DuckPathProjection.DiagonalInverseV;

        private float GetSinWaveCenterY()
        {
            return GetLaneCenterY(currentStartLane);
        }

        private float GetZigZagStartY()
        {
            return GetLaneCenterY(currentStartLane);
        }

        private bool IsZigZagTopFirst()
            => currentPathProjection == Constants.DuckPathProjection.ZigZagTopFirst;

        private void ConfigureBezierControlPoints(Constants.DuckPathProjection projection)
        {
            float w = screenRight - screenLeft;
            float safeHeight = screenTop - screenBottom;
            float margin = safeHeight * 0.05f;
            float topMargin = safeHeight * 0.02f;

            if (projection == Constants.DuckPathProjection.BezierMountain)
            {
                bezierP0 = new Vector2(screenLeft, screenBottom + margin);
                bezierP1 = new Vector2(screenLeft + w * 0.35f, screenTop - topMargin);
                bezierP2 = new Vector2(screenRight - w * 0.35f, screenTop - topMargin);
                bezierP3 = new Vector2(screenRight, screenBottom + margin);
            }
            else
            {
                bezierP0 = new Vector2(screenLeft, screenTop - margin);
                bezierP1 = new Vector2(screenLeft + w * 0.35f, screenBottom + margin);
                bezierP2 = new Vector2(screenRight - w * 0.35f, screenBottom + margin);
                bezierP3 = new Vector2(screenRight, screenTop - margin);
            }
        }

        private void AdvanceBezierPath()
        {
            float screenWidth = screenRight - screenLeft;
            if (screenWidth <= 0f)
            {
                DuckPassedScreen();
                return;
            }

            pathProgress += (speed * Time.fixedDeltaTime) / screenWidth;
            if (pathProgress >= 1f)
            {
                DuckPassedScreen();
                return;
            }

            Vector2 pos = CubicBezier(bezierP0, bezierP1, bezierP2, bezierP3, pathProgress);
            rb.MovePosition(new Vector2(pos.x, pos.y));
        }

        private void AdvanceDiagonalPath()
        {
            float screenWidth = screenRight - screenLeft;
            if (screenWidth <= 0f)
            {
                DuckPassedScreen();
                return;
            }

            pathProgress += (speed * Time.fixedDeltaTime) / screenWidth;
            if (pathProgress >= 1f)
            {
                DuckPassedScreen();
                return;
            }

            float safeHeight = screenTop - screenBottom;
            float margin = safeHeight * 0.05f;
            float x = Mathf.Lerp(screenLeft, screenRight, pathProgress);
            float y = currentPathProjection == Constants.DuckPathProjection.DiagonalRise
                ? Mathf.Lerp(screenBottom + margin, screenTop - margin, pathProgress)
                : Mathf.Lerp(screenTop - margin, screenBottom + margin, pathProgress);

            rb.MovePosition(new Vector2(x, y));
        }

        private void AdvanceSinWavePath()
        {
            float screenWidth = screenRight - screenLeft;
            if (screenWidth <= 0f)
            {
                DuckPassedScreen();
                return;
            }

            pathProgress += (speed * Time.fixedDeltaTime) / screenWidth;
            if (pathProgress >= 1f)
            {
                DuckPassedScreen();
                return;
            }

            float safeHeight = screenTop - screenBottom;
            float margin = safeHeight * 0.05f;
            float midY = GetSinWaveCenterY();
            float amplitude = currentPathProjection == Constants.DuckPathProjection.SinWaveBig
                ? (safeHeight * 0.5f) - margin
                : safeHeight * 0.18f;
            float maxAmplitude = Mathf.Max(0f, (safeHeight * 0.5f) - margin);
            amplitude = Mathf.Min(amplitude, maxAmplitude);
            float waves = 1.5f;
            float directionSign = currentPathProjection == Constants.DuckPathProjection.SinWaveStartDown ? -1f : 1f;

            float x = Mathf.Lerp(screenLeft, screenRight, pathProgress);
            float y = midY + (directionSign * Mathf.Sin(pathProgress * Mathf.PI * 2f * waves) * amplitude);
            rb.MovePosition(new Vector2(x, y));
        }

        private void AdvanceZigZagPath()
        {
            float screenWidth = screenRight - screenLeft;
            if (screenWidth <= 0f)
            {
                DuckPassedScreen();
                return;
            }

            pathProgress += (speed * Time.fixedDeltaTime) / screenWidth;
            if (pathProgress >= 1f)
            {
                DuckPassedScreen();
                return;
            }

            float safeHeight = screenTop - screenBottom;
            float margin = safeHeight * 0.05f;
            float topY = screenTop - margin;
            float bottomY = screenBottom + margin;
            float startY = GetZigZagStartY();
            bool topFirst = IsZigZagTopFirst();

            float y1 = topFirst ? topY : bottomY;
            float y2 = topFirst ? bottomY : topY;
            float y3 = y1;
            float y4 = y2;

            float y;
            if (pathProgress < 0.25f)
                y = Mathf.Lerp(startY, y1, pathProgress / 0.25f);
            else if (pathProgress < 0.50f)
                y = Mathf.Lerp(y1, y2, (pathProgress - 0.25f) / 0.25f);
            else if (pathProgress < 0.75f)
                y = Mathf.Lerp(y2, y3, (pathProgress - 0.50f) / 0.25f);
            else
                y = Mathf.Lerp(y3, y4, (pathProgress - 0.75f) / 0.25f);

            float x = Mathf.Lerp(screenLeft, screenRight, pathProgress);
            rb.MovePosition(new Vector2(x, y));
        }

        private void AdvanceBouncePath()
        {
            float screenWidth = screenRight - screenLeft;
            if (screenWidth <= 0f)
            {
                DuckPassedScreen();
                return;
            }

            const float bouncePoint = 0.55f;
            pathProgress += (speed * Time.fixedDeltaTime) / screenWidth;
            if (pathProgress >= 1f)
            {
                DuckPassedScreen();
                return;
            }

            float x;
            if (pathProgress < bouncePoint)
            {
                x = Mathf.Lerp(screenLeft, screenLeft + screenWidth * bouncePoint, pathProgress / bouncePoint);
                if (spriteRenderer != null) spriteRenderer.flipX = false;
            }
            else
            {
                x = Mathf.Lerp(screenLeft + screenWidth * bouncePoint, screenLeft, (pathProgress - bouncePoint) / (1f - bouncePoint));
                if (spriteRenderer != null) spriteRenderer.flipX = true;
            }

            rb.MovePosition(new Vector2(x, GetLaneCenterY(currentStartLane)));
        }

        private void AdvanceDiagonalVPath()
        {
            float screenWidth = screenRight - screenLeft;
            if (screenWidth <= 0f)
            {
                DuckPassedScreen();
                return;
            }

            pathProgress += (speed * Time.fixedDeltaTime) / screenWidth;
            if (pathProgress >= 1f)
            {
                DuckPassedScreen();
                return;
            }

            float safeHeight = screenTop - screenBottom;
            float margin = safeHeight * 0.05f;
            float bottomY = screenBottom + margin;
            float topY = screenTop - margin;
            float x = Mathf.Lerp(screenLeft, screenRight, pathProgress);
            float y;

            if (currentPathProjection == Constants.DuckPathProjection.DiagonalV)
            {
                y = pathProgress < 0.5f
                    ? Mathf.Lerp(bottomY, topY, pathProgress / 0.5f)
                    : Mathf.Lerp(topY, bottomY, (pathProgress - 0.5f) / 0.5f);
            }
            else
            {
                y = pathProgress < 0.5f
                    ? Mathf.Lerp(topY, bottomY, pathProgress / 0.5f)
                    : Mathf.Lerp(bottomY, topY, (pathProgress - 0.5f) / 0.5f);
            }

            rb.MovePosition(new Vector2(x, y));
        }

        private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        private void AnimateAliveSprite()
        {
            if (aliveFrames == null || aliveFrames.Length <= 1 || spriteRenderer == null)
            {
                return;
            }

            aliveFrameTimer += Time.fixedDeltaTime;
            float frameDuration = 1f / ALIVE_ANIMATION_FPS;
            if (aliveFrameTimer < frameDuration)
            {
                return;
            }

            aliveFrameTimer -= frameDuration;
            aliveFrameIndex = (aliveFrameIndex + 1) % aliveFrames.Length;
            spriteRenderer.sprite = aliveFrames[aliveFrameIndex];
        }

        private void SelectRandomPattern()
        {
            float total = movementWeightGoStraight + movementWeightGoTop + movementWeightGoBottom;
            float random = Random.Range(0f, total);

            if (random < movementWeightGoTop)
                currentPattern = Constants.MovementPattern.GoTop;
            else if (random < movementWeightGoTop + movementWeightGoBottom)
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
            pendingDeathWeaponType = weaponType;
            GameManager.Instance.BirdKilled(duckType, weaponType);

            if (spriteRenderer != null)
                spriteRenderer.flipX = false;

            // Store the alive sprite for reuse when this duck is recycled
            if (aliveSprite == null && spriteRenderer != null)
            {
                aliveSprite = spriteRenderer.sprite;
            }

            // Stop alive animation source to ensure nothing overwrites the death sprite after hit.
            aliveFrames = null;

            if (animator != null)
            {
                animator.enabled = false;
            }

            // Disable collider so it cant be hit again
            if (col != null)
            {
                col.enabled = false;
            }

            // Animator is disabled; death sprite is controlled directly.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;

            ShowHitPuff();
            Invoke(nameof(ApplyDeathState), deathSpriteDelay);

            GameLog.Log($"Duck hit by {weaponType}! Type: {duckType}, Points: {pointValue}");
        }

        private Sprite GetDeathSprite(Constants.WeaponType weaponType)
        {
            Sprite registeredDeathSprite = Weapon.GetRegisteredDeathSprite(weaponType);
            if (registeredDeathSprite != null)
            {
                return registeredDeathSprite;
            }

            GameLog.Warning($"[DUCK] No registered death sprite for weapon {weaponType}.");
            return null;
        }

        private void ReturnToPool()
        {
            CancelInvoke(nameof(ApplyDeathState));
            CancelInvoke(nameof(ReturnToPool));

            // Animator remains disabled; alive animation is code-driven.
            ResetHitPuff();

            if (transform.parent == null)
            {
                GameLog.Error("Duck has no parent! Cannot return to pool.");
                gameObject.SetActive(false);
                return;
            }

            IDuckSpawner spawner = transform.parent.GetComponent<IDuckSpawner>();
            if (spawner != null)
            {
                spawner.ReturnDuckToPool(gameObject);
            }
            else
            {
                GameLog.Warning($"IDuckSpawner not found on parent '{transform.parent.name}'. Deactivating duck.");
                gameObject.SetActive(false);
            }
        }

        private void ApplyDeathState()
        {
            if (spriteRenderer != null)
            {
                Sprite deathSprite = GetDeathSprite(pendingDeathWeaponType);
                if (deathSprite != null)
                {
                    spriteRenderer.sprite = deathSprite;
                }

                spriteRenderer.enabled = true;
                spriteRenderer.color = Color.white;
            }
            else
            {
                GameLog.Warning($"[DUCK] SpriteRenderer missing on hit for type {duckType}.");
            }

            if (keepDeadDuckForDebug)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;

                if (!freezeDeadDuckForDebug)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.gravityScale = DEAD_GRAVITY;
                }

                return;
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = DEAD_GRAVITY;
            rb.linearVelocity = Vector2.zero;
            Invoke(nameof(ReturnToPool), DEAD_CLEANUP_TIME);
        }

        private void ResetHitPuff()
        {
            if (hitPuffRenderer != null)
            {
                hitPuffRenderer.enabled = false;
            }
        }

        private void ShowHitPuff()
        {
            FitHitPuffToDuck();

            if (hitPuffRenderer != null)
            {
                if (spriteRenderer != null)
                {
                    hitPuffRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                    hitPuffRenderer.sortingOrder = spriteRenderer.sortingOrder + hitPuffSortingOffset;
                }

                hitPuffRenderer.enabled = true;
            }
        }

        private void FitHitPuffToDuck()
        {
            if (hitPuffRenderer == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Bounds duckBounds = spriteRenderer.bounds;
            Vector3 duckSize = duckBounds.size;
            if (duckSize.x <= 0f || duckSize.y <= 0f)
            {
                return;
            }

            Transform puffTransform = hitPuffRenderer.transform;
            Vector3 targetWorldPosition = duckBounds.center + new Vector3(
                duckSize.x * hitPuffOffsetNormalized.x,
                duckSize.y * hitPuffOffsetNormalized.y,
                0f);

            if (puffTransform.parent != null)
            {
                puffTransform.localPosition = puffTransform.parent.InverseTransformPoint(targetWorldPosition);
            }
            else
            {
                puffTransform.position = targetWorldPosition;
            }

            Sprite puffSprite = hitPuffRenderer.sprite;
            if (puffSprite == null)
            {
                return;
            }

            Vector2 puffSpriteSize = puffSprite.bounds.size;
            if (puffSpriteSize.x <= 0f || puffSpriteSize.y <= 0f)
            {
                return;
            }

            float targetWidth = duckSize.x * hitPuffWidthMultiplier;
            float targetHeight = duckSize.y * hitPuffHeightMultiplier;
            Vector3 parentLossyScale = puffTransform.parent != null ? puffTransform.parent.lossyScale : Vector3.one;
            float parentScaleX = Mathf.Abs(parentLossyScale.x);
            float parentScaleY = Mathf.Abs(parentLossyScale.y);
            if (parentScaleX < 0.0001f)
                parentScaleX = 1f;
            if (parentScaleY < 0.0001f)
                parentScaleY = 1f;

            puffTransform.localScale = new Vector3(
                targetWidth / (puffSpriteSize.x * parentScaleX),
                targetHeight / (puffSpriteSize.y * parentScaleY),
                1f);
        }

    }
}
