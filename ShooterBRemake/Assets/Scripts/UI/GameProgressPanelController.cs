using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class GameProgressPanelController : MonoBehaviour
    {
        [Header("UI")]
        public TextMeshProUGUI remainingWavesText;
        public TextMeshProUGUI remainingDucksText;
        public string wavesFormat = "Waves: {0}";
        public string ducksFormat = "Ducks: {0}";
        [Header("Text Style")]
        public bool applyOutline = true;
        public Color outlineColor = new Color32(27, 37, 51, 255);
        [Range(0f, 1f)] public float outlineWidth = 0.15f;

        private CampaignDuckSpawner campaignDuckSpawner;
        private bool hasInitializedFromStage;
        private int totalWaves;
        private int remainingWaves;
        private int totalDucks;
        private int remainingDucks;

        private void Start()
        {
            ApplyTextStyleOverrides();
            InitializeFromActiveStage();
            SubscribeToEvents();
            RefreshUI();
        }

        private void Update()
        {
            if (!hasInitializedFromStage)
            {
                InitializeFromActiveStage();
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBirdKilled += HandleBirdKilled;
                GameManager.Instance.OnBirdPassed += HandleBirdPassed;
            }

            campaignDuckSpawner = FindObjectOfType<CampaignDuckSpawner>();
            if (campaignDuckSpawner != null)
            {
                campaignDuckSpawner.OnWaveStarting += HandleWaveStarting;
                campaignDuckSpawner.OnAllDucksResolved += HandleAllDucksResolved;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBirdKilled -= HandleBirdKilled;
                GameManager.Instance.OnBirdPassed -= HandleBirdPassed;
            }

            if (campaignDuckSpawner != null)
            {
                campaignDuckSpawner.OnWaveStarting -= HandleWaveStarting;
                campaignDuckSpawner.OnAllDucksResolved -= HandleAllDucksResolved;
            }
        }

        private void InitializeFromActiveStage()
        {
            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;
            StageSpawnConfig spawnConfig = stage != null ? stage.spawnConfig : null;
            if (spawnConfig == null)
                return;

            totalWaves = CalculateTotalWaves(spawnConfig);
            totalDucks = CalculateTotalDucks(spawnConfig);
            remainingWaves = totalWaves;
            remainingDucks = totalDucks;
            hasInitializedFromStage = true;
        }

        private static int CalculateTotalWaves(StageSpawnConfig spawnConfig)
        {
            if (spawnConfig == null)
                return 0;

            if (spawnConfig.waves != null && spawnConfig.waves.Length > 0)
                return spawnConfig.waves.Length;

            return 1;
        }

        private static int CalculateTotalDucks(StageSpawnConfig spawnConfig)
        {
            if (spawnConfig == null)
                return 0;

            if (spawnConfig.waves != null && spawnConfig.waves.Length > 0)
            {
                int count = 0;
                for (int i = 0; i < spawnConfig.waves.Length; i++)
                    count += CalculateEntriesDuckCount(spawnConfig.waves[i].spawnSequence);
                return count;
            }

            return CalculateEntriesDuckCount(spawnConfig.spawnSequence);
        }

        private static int CalculateEntriesDuckCount(SpawnEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                SpawnEntry entry = entries[i];
                if (entry.patternRef != null && entry.patternRef.entries != null)
                    count += entry.patternRef.entries.Length;
                else
                    count += 1;
            }

            return count;
        }

        private void HandleWaveStarting(int waveNumber, float duration)
        {
            if (totalWaves <= 0)
                return;

            remainingWaves = Mathf.Clamp(totalWaves - waveNumber + 1, 0, totalWaves);
            RefreshUI();
        }

        private void HandleBirdKilled(Constants.DuckType duckType, Constants.WeaponType weaponType)
        {
            DecrementDucks();
        }

        private void HandleBirdPassed()
        {
            DecrementDucks();
        }

        private void DecrementDucks()
        {
            if (remainingDucks <= 0)
                return;

            remainingDucks--;
            RefreshUI();
        }

        private void HandleAllDucksResolved()
        {
            remainingWaves = 0;
            remainingDucks = 0;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (remainingWavesText != null)
                remainingWavesText.text = string.Format(wavesFormat, Mathf.Max(0, remainingWaves));

            if (remainingDucksText != null)
                remainingDucksText.text = string.Format(ducksFormat, Mathf.Max(0, remainingDucks));
        }

        private void ApplyTextStyleOverrides()
        {
            if (!applyOutline)
                return;

            ApplyOutlineToText(remainingWavesText);
            ApplyOutlineToText(remainingDucksText);
        }

        private void ApplyOutlineToText(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            Material instanceMaterial = text.fontMaterial;
            if (instanceMaterial == null)
                return;

            if (instanceMaterial.HasProperty("_OutlineColor"))
                instanceMaterial.SetColor("_OutlineColor", outlineColor);

            if (instanceMaterial.HasProperty("_OutlineWidth"))
                instanceMaterial.SetFloat("_OutlineWidth", Mathf.Clamp01(outlineWidth));

            text.fontMaterial = instanceMaterial;
        }
    }
}
