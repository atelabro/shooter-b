using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ShooterB
{
    public class CampaignDuckSpawner : MonoBehaviour, IDuckSpawner
    {
        [Header("Duck Prefab")]
        public GameObject duckPrefab;

        [Header("Duck Type Frames")]
        public DuckFrameLibrary duckFrameLibrary;

        [Header("Camera")]
        public Camera gameCamera;

        [Header("Spawn Settings")]
        public int poolSize = 20;

        private Queue<GameObject> duckPool;

        private float spawnX;
        private float minY;
        private float maxY;
        private float boundTop;
        private float boundBottom;
        private float boundRight;
        private float boundLeft;

        public event Action OnAllDucksResolved;
        public event Action<int, float, string> OnWaveStarting; // waveNumber (1-based), displayDuration, optional format override

        private bool isSpawning = false;
        private int activeDuckCount = 0;
        private int stageSpawnDifficulty;
        private float stageBaseSpeed;
        private int nextSpawnSortingOrder;
        private bool skipCurrentWaveRequested = false;
        private bool isWaveInProgress = false;

        private void Awake()
        {
            if (duckFrameLibrary == null)
            {
                GameLog.Error("[CampaignDuckSpawner] duckFrameLibrary is not assigned.");
            }

            InitializeDuckPool();
            CalculateSpawnBounds();
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
        }

        private void CalculateSpawnBounds()
        {
            Camera cam = gameCamera != null ? gameCamera : Camera.main;
            if (cam == null)
            {
                GameLog.Error("[CampaignDuckSpawner] Camera is null. Cannot calculate spawn bounds.");
                return;
            }

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
        }

        public void StartSpawning()
        {
            if (isSpawning)
                return;

            isSpawning = true;

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;
            StageSpawnConfig spawnConfig = stage != null ? stage.spawnConfig : null;

            if (spawnConfig == null)
            {
                GameLog.Warning("[CampaignDuckSpawner] No spawn config configured for active stage.");
                return;
            }

            bool hasWaves = spawnConfig.waves != null && spawnConfig.waves.Length > 0;
            bool hasSequence = spawnConfig.spawnSequence != null && spawnConfig.spawnSequence.Length > 0;

            if (!hasWaves && !hasSequence)
            {
                GameLog.Warning("[CampaignDuckSpawner] No spawn sequence or waves configured for active stage.");
                return;
            }

            stageSpawnDifficulty = GameManager.Instance.Difficulty;
            stageBaseSpeed = Constants.DuckSpeed.GetSpeed(stageSpawnDifficulty);
            nextSpawnSortingOrder = 1000;

            if (hasWaves)
            {
                StartCoroutine(SpawnWavesCoroutine(spawnConfig));
                GameLog.Log($"[CampaignDuckSpawner] Starting wave mode with {spawnConfig.waves.Length} waves.");
            }
            else
            {
                StartCoroutine(SpawnSequenceCoroutine(spawnConfig));
                GameLog.Log($"[CampaignDuckSpawner] Starting sequence with {spawnConfig.spawnSequence.Length} entries.");
            }
        }

        public void StopSpawning()
        {
            isSpawning = false;
            StopAllCoroutines();
        }

        public bool SkipCurrentWave()
        {
            if (!isSpawning || !isWaveInProgress || GameManager.Instance.IsGameOver)
                return false;

            skipCurrentWaveRequested = true;
            DespawnActiveWaveDucks();
            GameLog.Log("[CampaignDuckSpawner] Debug skip requested for current wave.");
            return true;
        }

        private IEnumerator SpawnSequenceCoroutine(StageSpawnConfig config)
        {
            yield return StartCoroutine(SpawnEntriesCoroutine(config.spawnSequence, config));
            StartCoroutine(WaitForAllDucksResolved());
        }

        private IEnumerator SpawnWavesCoroutine(StageSpawnConfig config)
        {
            for (int i = 0; i < config.waves.Length; i++)
            {
                if (!isSpawning || GameManager.Instance.IsGameOver)
                    yield break;

                WaveConfig wave = config.waves[i];
                int waveNumber = i + 1;
                skipCurrentWaveRequested = false;
                isWaveInProgress = true;

                if (wave.showAnnouncement)
                {
                    string labelFormat = ResolveWaveLabelFormat(wave);
                    OnWaveStarting?.Invoke(waveNumber, wave.announcementDuration, labelFormat);
                    yield return new WaitForSeconds(wave.announcementDuration);
                }

                yield return StartCoroutine(SpawnEntriesCoroutine(wave.spawnSequence, config));
                yield return StartCoroutine(WaitForWaveDucksResolved(waveNumber));
                isWaveInProgress = false;
            }

            StartCoroutine(WaitForAllDucksResolved());
        }

        private IEnumerator SpawnEntriesCoroutine(SpawnEntry[] entries, StageSpawnConfig config)
        {
            if (entries == null)
                yield break;

            foreach (SpawnEntry entry in entries)
            {
                if (!isSpawning || GameManager.Instance.IsGameOver || skipCurrentWaveRequested)
                    yield break;

                yield return new WaitForSeconds(entry.delay);

                if (!isSpawning || GameManager.Instance.IsGameOver || skipCurrentWaveRequested)
                    yield break;

                if (entry.patternRef != null && entry.patternRef.entries != null)
                {
                    foreach (PatternEntry patternEntry in entry.patternRef.entries)
                    {
                        if (!isSpawning || GameManager.Instance.IsGameOver || skipCurrentWaveRequested)
                            yield break;

                        yield return new WaitForSeconds(patternEntry.delay);

                        if (!isSpawning || GameManager.Instance.IsGameOver || skipCurrentWaveRequested)
                            yield break;

                        SpawnEntry resolved = new SpawnEntry
                        {
                            duckType = entry.duckType,
                            healthOverride = entry.healthOverride,
                            startLane = patternEntry.startLane,
                            pathProjection = patternEntry.pathProjection,
                            speedMultiplier = patternEntry.speedMultiplier,
                            sizeMultiplier = entry.sizeMultiplier,
                            delay = patternEntry.delay
                        };
                        SpawnDuck(resolved, config);
                    }
                }
                else
                {
                    SpawnDuck(entry, config);
                }
            }
        }

        private IEnumerator WaitForWaveDucksResolved(int waveNumber)
        {
            float timeout = 15f;
            float elapsed = 0f;

            while (activeDuckCount > 0 && isSpawning && !GameManager.Instance.IsGameOver)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;

                if (elapsed >= timeout)
                {
                    GameLog.Warning($"[CampaignDuckSpawner] Timed out waiting for wave {waveNumber} ducks to resolve.");
                    yield break;
                }
            }
        }

        private void DespawnActiveWaveDucks()
        {
            if (duckPool == null)
                return;

            List<GameObject> activeDucks = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child == null)
                    continue;

                GameObject duck = child.gameObject;
                if (duck.activeSelf)
                    activeDucks.Add(duck);
            }

            for (int i = 0; i < activeDucks.Count; i++)
                ReturnDuckToPool(activeDucks[i]);
        }

        private static string ResolveWaveLabelFormat(WaveConfig wave)
        {
            if (wave == null)
                return null;

            LocalizationManager.Language language = LocalizationManager.Instance.CurrentLanguage;
            string format = language == LocalizationManager.Language.Macedonian
                ? wave.macedonianWaveLabelFormat
                : wave.englishWaveLabelFormat;

            return string.IsNullOrWhiteSpace(format) ? null : format;
        }

        private IEnumerator WaitForAllDucksResolved()
        {
            float timeout = 15f;
            float elapsed = 0f;

            while (activeDuckCount > 0 && !GameManager.Instance.IsGameOver)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;

                if (elapsed >= timeout)
                {
                    GameLog.Warning("[CampaignDuckSpawner] Timed out waiting for ducks to resolve. Forcing stage complete.");
                    break;
                }
            }

            if (!GameManager.Instance.IsGameOver)
                OnAllDucksResolved?.Invoke();
        }

        private void SpawnDuck(SpawnEntry entry, StageSpawnConfig config)
        {
            GameObject duckObj = GetDuckFromPool();
            if (duckObj == null)
            {
                GameLog.Warning("[CampaignDuckSpawner] Duck pool exhausted.");
                return;
            }

            Duck duck = duckObj.GetComponent<Duck>();
            if (duck == null)
                return;

            Sprite[] frames = GetFramesForType(entry.duckType);
            Vector2 spawnPosition = GetSpawnPosition(entry.startLane, entry.pathProjection);

            duck.Initialize(
                entry.duckType,
                stageSpawnDifficulty,
                spawnPosition,
                boundTop, boundBottom, boundRight, boundLeft,
                frames,
                entry.healthOverride,
                entry.sizeMultiplier,
                entry.startLane,
                entry.pathProjection,
                config.weightGoStraight,
                config.weightGoTop,
                config.weightGoBottom
            );

            float entrySpeedMultiplier = entry.speedMultiplier > 0f ? entry.speedMultiplier : 1f;
            duck.speed = stageBaseSpeed * config.duckSpeedMultiplier * entrySpeedMultiplier;

            SpriteRenderer spriteRenderer = duck.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = nextSpawnSortingOrder;
                duck.RefreshHealthBarSorting();
                nextSpawnSortingOrder--;
            }

            GameManager.Instance.BirdCreated();
            activeDuckCount++;
        }

        public void ReturnDuckToPool(GameObject duck)
        {
            if (duck == null)
            {
                GameLog.Error("[CampaignDuckSpawner] Trying to return null duck to pool.");
                return;
            }

            if (duckPool == null)
            {
                duck.SetActive(false);
                return;
            }

            duck.SetActive(false);
            duckPool.Enqueue(duck);
            activeDuckCount = Mathf.Max(0, activeDuckCount - 1);
        }

        private GameObject GetDuckFromPool()
        {
            GameObject duck;

            if (duckPool.Count > 0)
                duck = duckPool.Dequeue();
            else
            {
                duck = Instantiate(duckPrefab);
                duck.transform.SetParent(transform);
            }

            duck.SetActive(true);
            return duck;
        }

        private Vector2 GetSpawnPosition(Constants.DuckStartLane startLane, Constants.DuckPathProjection pathProjection)
        {
            if (pathProjection == Constants.DuckPathProjection.Random)
            {
                float randomY = UnityEngine.Random.Range(minY, maxY);
                float randomXOffset = UnityEngine.Random.Range(0f, 1f);
                return new Vector2(spawnX - randomXOffset, randomY);
            }

            return new Vector2(spawnX, GetLaneCenterY(startLane));
        }

        private float GetLaneCenterY(Constants.DuckStartLane startLane)
        {
            int lane = (int)startLane;
            if (lane < 1 || lane > 9)
                lane = 5;

            float laneHeight = (maxY - minY) / 9f;
            return minY + laneHeight * (lane - 0.5f);
        }

        private Sprite[] GetFramesForType(Constants.DuckType type)
        {
            if (duckFrameLibrary == null)
            {
                GameLog.Error("[CampaignDuckSpawner] duckFrameLibrary is not assigned.");
                return null;
            }

            Sprite[] frames = duckFrameLibrary.GetFrames(type);
            if (frames == null || frames.Length == 0)
            {
                GameLog.Error($"[CampaignDuckSpawner] No frames found for duck type {type} in duckFrameLibrary.");
            }

            return frames;
        }

        private void OnDestroy()
        {
            StopSpawning();
        }
    }
}
