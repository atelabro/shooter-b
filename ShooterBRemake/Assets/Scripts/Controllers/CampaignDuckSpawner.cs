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

        private bool isSpawning = false;
        private int activeDuckCount = 0;

        private void Awake()
        {
            if (duckFrameLibrary == null)
            {
                Debug.LogError("[CampaignDuckSpawner] duckFrameLibrary is not assigned.");
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
                Debug.LogError("[CampaignDuckSpawner] Camera is null. Cannot calculate spawn bounds.");
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

            if (spawnConfig == null || spawnConfig.spawnSequence == null || spawnConfig.spawnSequence.Length == 0)
            {
                Debug.LogWarning("[CampaignDuckSpawner] No spawn sequence configured for active stage.");
                return;
            }

            StartCoroutine(SpawnSequenceCoroutine(spawnConfig));
            Debug.Log($"[CampaignDuckSpawner] Starting sequence with {spawnConfig.spawnSequence.Length} entries.");
        }

        public void StopSpawning()
        {
            isSpawning = false;
            StopAllCoroutines();
        }

        private IEnumerator SpawnSequenceCoroutine(StageSpawnConfig config)
        {
            foreach (SpawnEntry entry in config.spawnSequence)
            {
                if (!isSpawning || GameManager.Instance.IsGameOver)
                    yield break;

                yield return new WaitForSeconds(entry.delay);

                if (!isSpawning || GameManager.Instance.IsGameOver)
                    yield break;

                SpawnDuck(entry, config);
            }

            StartCoroutine(WaitForAllDucksResolved());
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
                    Debug.LogWarning("[CampaignDuckSpawner] Timed out waiting for ducks to resolve. Forcing stage complete.");
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
                Debug.LogWarning("[CampaignDuckSpawner] Duck pool exhausted.");
                return;
            }

            Duck duck = duckObj.GetComponent<Duck>();
            if (duck == null)
                return;

            Sprite[] frames = GetFramesForType(entry.duckType);
            Vector2 spawnPosition = GetRandomSpawnPosition();

            duck.Initialize(
                entry.duckType,
                GameManager.Instance.Difficulty,
                spawnPosition,
                boundTop, boundBottom, boundRight, boundLeft,
                frames,
                config.weightGoStraight,
                config.weightGoTop,
                config.weightGoBottom
            );

            if (config.duckSpeedMultiplier != 1f)
                duck.speed *= config.duckSpeedMultiplier;

            GameManager.Instance.BirdCreated();
            activeDuckCount++;
        }

        public void ReturnDuckToPool(GameObject duck)
        {
            if (duck == null)
            {
                Debug.LogError("[CampaignDuckSpawner] Trying to return null duck to pool.");
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

        private Vector2 GetRandomSpawnPosition()
        {
            float randomY = UnityEngine.Random.Range(minY, maxY);
            float randomXOffset = UnityEngine.Random.Range(0f, 1f);
            return new Vector2(spawnX - randomXOffset, randomY);
        }

        private Sprite[] GetFramesForType(Constants.DuckType type)
        {
            if (duckFrameLibrary == null)
            {
                Debug.LogError("[CampaignDuckSpawner] duckFrameLibrary is not assigned.");
                return null;
            }

            Sprite[] frames = duckFrameLibrary.GetFrames(type);
            if (frames == null || frames.Length == 0)
            {
                Debug.LogError($"[CampaignDuckSpawner] No frames found for duck type {type} in duckFrameLibrary.");
            }

            return frames;
        }

        private void OnDestroy()
        {
            StopSpawning();
        }
    }
}
