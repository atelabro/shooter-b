using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ShooterB
{
    public class DuckSpawner : MonoBehaviour
    {
        [Header("Duck Prefab")]
        public GameObject duckPrefab;

        [Header("Camera")]
        public Camera gameCamera;

        [Header("Spawn Settings")]
        public int poolSize = 20;
        private Queue<GameObject> duckPool;

        [Header("Spawn Timing")]
        private float nextSpawnTime;

        [Header("Screen Bounds")]
        private float spawnX;
        private float minY;
        private float maxY;
        private float boundTop;
        private float boundBottom;
        private float boundRight;
        private float boundLeft;

        private bool isSpawning = false;

        private void Awake()
        {
            Debug.Log($"[DUCKSPAWNER] Awake called on {gameObject.name}");
            InitializeDuckPool();
            CalculateSpawnBounds();
        }

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
                isSpawning = true;
                nextSpawnTime = Time.time + 2f;
                StartCoroutine(SpawnCoroutine());
                Debug.Log("Duck spawning started");
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
                    SpawnDuck();

                    float spawnDelay = Constants.SpawnTiming.GetSpawnDelay(GameManager.Instance.Difficulty);
                    nextSpawnTime = Time.time + spawnDelay;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void SpawnDuck()
        {
            GameObject duckObj = GetDuckFromPool();
            if (duckObj == null)
            {
                Debug.LogWarning("Duck pool exhausted!");
                return;
            }

            Constants.DuckType duckType = SelectDuckType();
            Vector2 spawnPosition = GetRandomSpawnPosition();

            Duck duck = duckObj.GetComponent<Duck>();
            if (duck != null)
            {
                duck.Initialize(duckType, GameManager.Instance.Difficulty, spawnPosition, boundTop, boundBottom, boundRight, boundLeft);
                GameManager.Instance.BirdCreated();
            }
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
            Debug.Log($"Duck returned to pool. Pool size: {duckPool.Count}");
        }

        private Constants.DuckType SelectDuckType()
        {
            float random = Random.Range(0f, 1f);

            if (random < Constants.DuckSpawnProbability.TYPE_0)
                return Constants.DuckType.Type0;
            else if (random < Constants.DuckSpawnProbability.TYPE_0 + Constants.DuckSpawnProbability.TYPE_1)
                return Constants.DuckType.Type1;
            else if (random < Constants.DuckSpawnProbability.TYPE_0 + Constants.DuckSpawnProbability.TYPE_1 + Constants.DuckSpawnProbability.TYPE_2)
                return Constants.DuckType.Type2;
            else if (random < 0.96f)
                return Constants.DuckType.Type3;
            else
                return Constants.DuckType.Type4;
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
    }
}
