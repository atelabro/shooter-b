using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ShooterB
{
    public class DuckSpawner : MonoBehaviour, IDuckSpawner
    {
        [Header("Duck Prefab")]
        public GameObject duckPrefab;

        [Header("Duck Type Frames")]
        public Sprite[] type0PrivateFrames;
        public Sprite[] type1RamboFrames;
        public Sprite[] type2KimonoFrames;
        public Sprite[] type3CheFrames;
        public Sprite[] type4SoldierFrames;
        public Sprite[] type5PhalarxFrames;

        [Header("Camera")]
        public Camera gameCamera;

        [Header("Spawn Settings")]
        public int poolSize = 20;
        private Queue<GameObject> duckPool;

        [Header("Spawn Timing")]
        private float nextSpawnTime;

        [Header("Debug")]
        [SerializeField] private bool enableSpawnDistributionDebug = false;
        [SerializeField] private int spawnDistributionLogInterval = 100;

        [Header("Screen Bounds")]
        private float spawnX;
        private float minY;
        private float maxY;
        private float boundTop;
        private float boundBottom;
        private float boundRight;
        private float boundLeft;

        private bool isSpawning = false;
        private int totalSpawnedCount = 0;
        private int[] spawnCountsByType;
        private int activeDuckCount = 0;

        private void Awake()
        {
            Debug.Log($"[DUCKSPAWNER] Awake called on {gameObject.name}");
            spawnCountsByType = new int[System.Enum.GetValues(typeof(Constants.DuckType)).Length];
            ValidateDuckTypeFrames();
            InitializeDuckPool();
            CalculateSpawnBounds();
        }

        private void ValidateDuckTypeFrames()
        {
            ValidateTypeFrames(type0PrivateFrames, Constants.DuckType.Type0, nameof(type0PrivateFrames));
            ValidateTypeFrames(type1RamboFrames, Constants.DuckType.Type1, nameof(type1RamboFrames));
            ValidateTypeFrames(type2KimonoFrames, Constants.DuckType.Type2, nameof(type2KimonoFrames));
            ValidateTypeFrames(type3CheFrames, Constants.DuckType.Type3, nameof(type3CheFrames));
            ValidateTypeFrames(type4SoldierFrames, Constants.DuckType.Type4, nameof(type4SoldierFrames));
            ValidateTypeFrames(type5PhalarxFrames, Constants.DuckType.Type5, nameof(type5PhalarxFrames));
        }

        private void ValidateTypeFrames(Sprite[] frames, Constants.DuckType type, string fieldName)
        {
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[DUCKSPAWNER] No frames set for {type} ({fieldName}). Falling back to Type0 frames.");
            }
        }

        [ContextMenu("Auto-Populate Duck Frames")]
        private void AutoPopulateDuckFrames()
        {
#if UNITY_EDITOR
            type0PrivateFrames = LoadFramesFromSheet("Assets/Sprites/smallAnimatedPrivate.png", "smallAnimatedPrivate_");
            type1RamboFrames = LoadFramesFromSheet("Assets/Sprites/smallAnimatedRambo.png", "smallAnimatedRambo_");
            type2KimonoFrames = LoadFramesFromSheet("Assets/Sprites/smallAnimatedKimono.png", "smallAnimatedKimono_");
            type3CheFrames = LoadFramesFromSheet("Assets/Sprites/smallAnimatedChe.png", "smallAnimatedChe_");
            type4SoldierFrames = LoadFramesFromSheet("Assets/Sprites/smallAnimatedSoldier.png", "smallAnimatedSoldier_");
            type5PhalarxFrames = LoadFramesFromSheet("Assets/Sprites/PhalarxMacedonianDuck/mkd01.png", "mkd01_");

            UnityEditor.EditorUtility.SetDirty(this);

            Debug.Log(
                $"[DUCKSPAWNER] Auto-populated duck frames: " +
                $"Type0={type0PrivateFrames.Length}, " +
                $"Type1={type1RamboFrames.Length}, " +
                $"Type2={type2KimonoFrames.Length}, " +
                $"Type3={type3CheFrames.Length}, " +
                $"Type4={type4SoldierFrames.Length}, " +
                $"Type5={type5PhalarxFrames.Length}"
            );
#else
            Debug.LogWarning("[DUCKSPAWNER] Auto-Populate Duck Frames is only available in Unity Editor.");
#endif
        }

#if UNITY_EDITOR
        private static Sprite[] LoadFramesFromSheet(string assetPath, string spriteNamePrefix)
        {
            UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            List<Sprite> frames = new List<Sprite>();

            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name.StartsWith(spriteNamePrefix, System.StringComparison.Ordinal))
                {
                    frames.Add(sprite);
                }
            }

            frames.Sort((a, b) => ExtractFrameIndex(a.name).CompareTo(ExtractFrameIndex(b.name)));
            return frames.ToArray();
        }

        private static int ExtractFrameIndex(string spriteName)
        {
            int underscoreIndex = spriteName.LastIndexOf('_');
            if (underscoreIndex < 0 || underscoreIndex >= spriteName.Length - 1)
            {
                return int.MaxValue;
            }

            string suffix = spriteName.Substring(underscoreIndex + 1);
            if (int.TryParse(suffix, out int index))
            {
                return index;
            }

            return int.MaxValue;
        }
#endif

        private void Start()
        {
            Debug.Log($"[DUCKSPAWNER] Start called on {gameObject.name}");
            StartSpawning();
        }

        private void InitializeDuckPool()
        {
            duckPool = new Queue<GameObject>();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject duck = Instantiate(duckPrefab);
                duck.SetActive(false);
                duck.transform.SetParent(transform);
                duckPool.Enqueue(duck);
            }

            Debug.Log($"Duck pool initialized with {poolSize} ducks");
        }

        private void CalculateSpawnBounds()
        {
            Camera cam = gameCamera != null ? gameCamera : Camera.main;
            if (cam != null)
            {
                // Calculate world bounds at the Z-plane where ducks fly (Z = -5)
                float distanceFromCamera = Mathf.Abs(cam.transform.position.z - (-5));

                Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, distanceFromCamera));
                Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, distanceFromCamera));

                spawnX = bottomLeft.x - 2f;
                minY = bottomLeft.y + (Constants.FOOTER_HEIGHT / 100f) + 1f;
                maxY = topRight.y - (Constants.HEADER_HEIGHT / 100f) - 1f;

                boundTop = topRight.y - (Constants.HEADER_HEIGHT / 100f);
                boundBottom = bottomLeft.y + (Constants.FOOTER_HEIGHT / 100f);
                boundRight = topRight.x + 2f;
                boundLeft = bottomLeft.x - 2f;

                Debug.Log($"Camera Z: {cam.transform.position.z}, Distance: {distanceFromCamera}");
                Debug.Log($"Spawn bounds - Bottom-Left: {bottomLeft}, Top-Right: {topRight}");
                Debug.Log($"Spawn bounds - SpawnX: {spawnX}, Y range: {minY} to {maxY}");
                Debug.Log($"Duck bounds - Top: {boundTop}, Bottom: {boundBottom}, Right: {boundRight}, Left: {boundLeft}");
            }
            else
            {
                Debug.LogError("Camera is null! Cannot calculate spawn bounds!");
            }
        }

        public void StartSpawning()
        {
            if (!isSpawning)
            {
                ResetSpawnDistributionCounters();
                isSpawning = true;
                nextSpawnTime = Time.time + GetInitialSpawnDelay();
                StartCoroutine(SpawnCoroutine());
                Debug.Log("Duck spawning started - Arcade random");
            }
        }

        public void StopSpawning()
        {
            isSpawning = false;
            StopAllCoroutines();
            Debug.Log("Duck spawning stopped");
        }

        private IEnumerator SpawnCoroutine()
        {
            while (isSpawning && !GameManager.Instance.IsGameOver)
            {
                if (Time.time >= nextSpawnTime)
                {
                    int ducksToSpawn = IsArcadeVeryHardEnabled() ? Random.Range(1, 3) : 1;
                    for (int i = 0; i < ducksToSpawn; i++)
                    {
                        SpawnDuck();
                    }

                    float spawnDelay = GetNextSpawnDelay();
                    nextSpawnTime = Time.time + spawnDelay;
                }

                if (IsArcadeVeryHardEnabled())
                {
                    int shortfall = Constants.SpawnTiming.ARCADE_VERY_HARD_MIN_ACTIVE_DUCKS - activeDuckCount;
                    if (shortfall > 0)
                    {
                        int ducksToSpawnNow = Mathf.Min(shortfall, Constants.SpawnTiming.ARCADE_VERY_HARD_SPAWN_BATCH);
                        for (int i = 0; i < ducksToSpawnNow; i++)
                        {
                            SpawnDuck();
                        }
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void SpawnDuck()
        {
            Constants.DuckType duckType = SelectDuckType();

            GameObject duckObj = GetDuckFromPool();
            if (duckObj == null)
            {
                Debug.LogWarning("Duck pool exhausted!");
                return;
            }

            RecordDuckSpawn(duckType);
            Vector2 spawnPosition = GetRandomSpawnPosition();

            Duck duck = duckObj.GetComponent<Duck>();
            if (duck != null)
            {
                Sprite[] duckFrames = GetFramesForType(duckType);
                duck.Initialize(duckType, GameManager.Instance.Difficulty, spawnPosition, boundTop, boundBottom, boundRight, boundLeft, duckFrames);
                GameManager.Instance.BirdCreated();
                activeDuckCount++;
            }
        }

        private Sprite[] GetFramesForType(Constants.DuckType type)
        {
            Sprite[] selectedFrames;
            switch (type)
            {
                case Constants.DuckType.Type0:
                    selectedFrames = type0PrivateFrames;
                    break;
                case Constants.DuckType.Type1:
                    selectedFrames = type1RamboFrames;
                    break;
                case Constants.DuckType.Type2:
                    selectedFrames = type2KimonoFrames;
                    break;
                case Constants.DuckType.Type3:
                    selectedFrames = type3CheFrames;
                    break;
                case Constants.DuckType.Type4:
                    selectedFrames = type4SoldierFrames;
                    break;
                case Constants.DuckType.Type5:
                    selectedFrames = type5PhalarxFrames;
                    break;
                default:
                    selectedFrames = type0PrivateFrames;
                    break;
            }

            if (selectedFrames == null || selectedFrames.Length == 0)
            {
                return type0PrivateFrames;
            }

            return selectedFrames;
        }

        private GameObject GetDuckFromPool()
        {
            GameObject duck;

            if (duckPool.Count > 0)
            {
                duck = duckPool.Dequeue();
            } else 
            {
                duck = Instantiate(duckPrefab);
                duck.transform.SetParent(transform);
            }

            duck.SetActive(true);

            return duck;
        }

        public void ReturnDuckToPool(GameObject duck)
        {
            if (duck == null)
            {
                Debug.LogError("Trying to return null duck to pool!");
                return;
            }

            if (duckPool == null)
            {
                Debug.LogError("Duck pool is null! Spawner may not be initialized yet.");
                duck.SetActive(false);
                return;
            }

            duck.SetActive(false);
            duckPool.Enqueue(duck);
            activeDuckCount = Mathf.Max(0, activeDuckCount - 1);
            Debug.Log($"Duck returned to pool. Pool size: {duckPool.Count}");
        }

        private Constants.DuckType SelectDuckType()
        {
            float r = Random.Range(0f, 1f);
            float t0 = Constants.DuckSpawnProbability.TYPE_0;
            float t1 = t0 + Constants.DuckSpawnProbability.TYPE_1;
            float t2 = t1 + Constants.DuckSpawnProbability.TYPE_2;
            float t3 = t2 + Constants.DuckSpawnProbability.TYPE_3;
            float t4 = t3 + Constants.DuckSpawnProbability.TYPE_4;

            if (r < t0) return Constants.DuckType.Type0;
            if (r < t1) return Constants.DuckType.Type1;
            if (r < t2) return Constants.DuckType.Type2;
            if (r < t3) return Constants.DuckType.Type3;
            if (r < t4) return Constants.DuckType.Type4;
            return Constants.DuckType.Type5;
        }

        private void ResetSpawnDistributionCounters()
        {
            totalSpawnedCount = 0;
            if (spawnCountsByType == null)
            {
                spawnCountsByType = new int[System.Enum.GetValues(typeof(Constants.DuckType)).Length];
                return;
            }

            for (int i = 0; i < spawnCountsByType.Length; i++)
            {
                spawnCountsByType[i] = 0;
            }
        }

        private void RecordDuckSpawn(Constants.DuckType duckType)
        {
            if (spawnCountsByType == null)
            {
                spawnCountsByType = new int[System.Enum.GetValues(typeof(Constants.DuckType)).Length];
            }

            int duckTypeIndex = (int)duckType;
            if (duckTypeIndex >= 0 && duckTypeIndex < spawnCountsByType.Length)
            {
                spawnCountsByType[duckTypeIndex]++;
            }

            totalSpawnedCount++;

            if (!enableSpawnDistributionDebug || spawnDistributionLogInterval <= 0 || totalSpawnedCount % spawnDistributionLogInterval != 0)
            {
                return;
            }

            LogSpawnDistribution();
        }

        private void LogSpawnDistribution()
        {
            if (totalSpawnedCount <= 0 || spawnCountsByType == null || spawnCountsByType.Length == 0)
            {
                return;
            }

            Debug.Log(
                $"[DUCKSPAWNER] Spawn distribution @ {totalSpawnedCount} spawns | " +
                $"Type0: {GetActualPercent(Constants.DuckType.Type0):F1}% (exp {Constants.DuckSpawnProbability.TYPE_0 * 100f:F1}%) | " +
                $"Type1: {GetActualPercent(Constants.DuckType.Type1):F1}% (exp {Constants.DuckSpawnProbability.TYPE_1 * 100f:F1}%) | " +
                $"Type2: {GetActualPercent(Constants.DuckType.Type2):F1}% (exp {Constants.DuckSpawnProbability.TYPE_2 * 100f:F1}%) | " +
                $"Type3: {GetActualPercent(Constants.DuckType.Type3):F1}% (exp {Constants.DuckSpawnProbability.TYPE_3 * 100f:F1}%) | " +
                $"Type4: {GetActualPercent(Constants.DuckType.Type4):F1}% (exp {Constants.DuckSpawnProbability.TYPE_4 * 100f:F1}%) | " +
                $"Type5: {GetActualPercent(Constants.DuckType.Type5):F1}% (exp {Constants.DuckSpawnProbability.TYPE_5 * 100f:F1}%)"
            );
        }

        private float GetActualPercent(Constants.DuckType duckType)
        {
            if (totalSpawnedCount <= 0 || spawnCountsByType == null)
            {
                return 0f;
            }

            int duckTypeIndex = (int)duckType;
            if (duckTypeIndex < 0 || duckTypeIndex >= spawnCountsByType.Length)
            {
                return 0f;
            }

            return (spawnCountsByType[duckTypeIndex] / (float)totalSpawnedCount) * 100f;
        }

        private Vector2 GetRandomSpawnPosition()
        {
            float randomY = Random.Range(minY, maxY);
            float randomXOffset = Random.Range(0f, 1f);
            return new Vector2(spawnX - randomXOffset, randomY);
        }

        private void OnDestroy()
        {
            StopSpawning();
        }

        private bool IsArcadeVeryHardEnabled()
        {
            return GameManager.Instance.CurrentGameMode == Constants.GameMode.Arcade && GameManager.Instance.ArcadeVeryHardMode;
        }

        private float GetNextSpawnDelay()
        {
            float delay = Constants.SpawnTiming.GetSpawnDelay(GameManager.Instance.Difficulty);
            if (IsArcadeVeryHardEnabled())
                return Mathf.Max(Constants.SpawnTiming.ARCADE_VERY_HARD_MIN_DELAY, delay * Constants.SpawnTiming.ARCADE_VERY_HARD_MULTIPLIER);

            if (GameManager.Instance.Difficulty >= 35)
                return Mathf.Max(0.1f, delay);

            return delay;
        }

        private float GetInitialSpawnDelay()
        {
            if (IsArcadeVeryHardEnabled())
                return Constants.SpawnTiming.ARCADE_VERY_HARD_INITIAL_DELAY;

            return 2f;
        }
    }
}
