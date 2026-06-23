using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShooterB
{
    public class DuckTrophyManager : MonoBehaviour
    {
        public struct DuckTrophyState
        {
            public DuckTrophyEntry entry;
            public string displayName;
            public bool discovered;
            public int killCount;
        }

        public struct CityTrophyState
        {
            public DuckTrophyCity city;
            public int discoveredCount;
            public int totalCount;
            public bool rewardClaimed;
            public bool canClaimReward;
            public int rewardCoins;
        }

        private const string CatalogResourcePath = "DuckTrophyCatalog";
        private const string PrefsPrefix = "DuckTrophy";
        private const string SchemaVersionKey = "DuckTrophies_SchemaVersion";
        private const int CurrentSchemaVersion = 1;

        private static DuckTrophyManager instance;
        public static DuckTrophyManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("DuckTrophyManager");
                    instance = go.AddComponent<DuckTrophyManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public event Action<Constants.DuckType> OnDuckDiscovered;
        public event Action<Constants.DuckType, int> OnDuckKillCountChanged;
        public event Action<DuckTrophyCity> OnCityRewardClaimed;
        public event Action OnClaimableBadgeChanged;

        private readonly Dictionary<Constants.DuckType, bool> discoveredByDuck =
            new Dictionary<Constants.DuckType, bool>();
        private readonly Dictionary<Constants.DuckType, int> killsByDuck =
            new Dictionary<Constants.DuckType, int>();
        private readonly Dictionary<DuckTrophyCity, bool> rewardClaimedByCity =
            new Dictionary<DuckTrophyCity, bool>();

        private CampaignDuckSpawner subscribedCampaignSpawner;
        private int previousClaimableRewardCount;

        public DuckTrophyCatalog Catalog { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            Catalog = Resources.Load<DuckTrophyCatalog>(CatalogResourcePath);
            if (Catalog == null)
                GameLog.Warning($"[DuckTrophy] Missing Resources/{CatalogResourcePath} catalog.");

            EnsureSchemaVersion();
            LoadState();
            SubscribeToGameManager();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TrySubscribeCampaignSpawner();
            previousClaimableRewardCount = GetClaimableRewardCount();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnBirdKilled -= HandleBirdKilled;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribeCampaignSpawner();
        }

        public IReadOnlyList<DuckTrophyState> GetDuckStatesForCity(DuckTrophyCity city)
        {
            List<DuckTrophyState> result = new List<DuckTrophyState>();
            if (Catalog == null)
                return result;

            foreach (DuckTrophyEntry entry in Catalog.Entries)
            {
                if (entry.city != city)
                    continue;

                result.Add(BuildState(entry));
            }

            return result
                .OrderBy(state => state.entry.trophyClass)
                .ThenBy(state => state.entry.sortOrder)
                .ThenBy(state => state.displayName)
                .ToList();
        }

        public CityTrophyState GetCityState(DuckTrophyCity city)
        {
            IReadOnlyList<DuckTrophyState> states = GetDuckStatesForCity(city);
            int discoveredCount = 0;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].discovered)
                    discoveredCount++;
            }

            bool complete = states.Count > 0 && discoveredCount >= states.Count;
            bool rewardClaimed = IsCityRewardClaimed(city);
            return new CityTrophyState
            {
                city = city,
                discoveredCount = discoveredCount,
                totalCount = states.Count,
                rewardClaimed = rewardClaimed,
                canClaimReward = complete && !rewardClaimed,
                rewardCoins = DuckTrophyCatalog.CityCompletionRewardCoins
            };
        }

        public bool IsDiscovered(Constants.DuckType duckType)
        {
            return discoveredByDuck.TryGetValue(duckType, out bool discovered) && discovered;
        }

        public int GetKillCount(Constants.DuckType duckType)
        {
            return killsByDuck.TryGetValue(duckType, out int kills) ? Mathf.Max(0, kills) : 0;
        }

        public bool HasAnyClaimableReward()
        {
            for (int i = 0; i < DuckTrophyCatalog.CampaignCityOrder.Length; i++)
            {
                if (GetCityState(DuckTrophyCatalog.CampaignCityOrder[i]).canClaimReward)
                    return true;
            }

            return false;
        }

        public int GetClaimableRewardCount()
        {
            int count = 0;
            for (int i = 0; i < DuckTrophyCatalog.CampaignCityOrder.Length; i++)
            {
                if (GetCityState(DuckTrophyCatalog.CampaignCityOrder[i]).canClaimReward)
                    count++;
            }

            return count;
        }

        public bool TryClaimCityReward(DuckTrophyCity city)
        {
            CityTrophyState state = GetCityState(city);
            if (!state.canClaimReward)
                return false;

            rewardClaimedByCity[city] = true;
            PlayerPrefs.SetInt(GetCityRewardClaimedKey(city), 1);
            GameManager.Instance.AddCoins(state.rewardCoins);
            PlayerPrefs.Save();

            OnCityRewardClaimed?.Invoke(city);
            NotifyClaimableBadgeIfChanged();
            GameLog.Log($"[DuckTrophy] Claimed {city} collection reward (+{state.rewardCoins} coins).");
            return true;
        }

        public void RecordCampaignDuckSpawned(Constants.DuckType duckType)
        {
            if (!HasCatalogEntry(duckType) || IsDiscovered(duckType))
                return;

            discoveredByDuck[duckType] = true;
            PlayerPrefs.SetInt(GetDiscoveredKey(duckType), 1);
            PlayerPrefs.Save();

            OnDuckDiscovered?.Invoke(duckType);
            NotifyClaimableBadgeIfChanged();
            GameLog.Log($"[DuckTrophy] Discovered {duckType}.");
        }

        private DuckTrophyState BuildState(DuckTrophyEntry entry)
        {
            return new DuckTrophyState
            {
                entry = entry,
                displayName = Constants.GetDuckDisplayName(entry.duckType),
                discovered = IsDiscovered(entry.duckType),
                killCount = GetKillCount(entry.duckType)
            };
        }

        private void SubscribeToGameManager()
        {
            GameManager.Instance.OnBirdKilled -= HandleBirdKilled;
            GameManager.Instance.OnBirdKilled += HandleBirdKilled;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TrySubscribeCampaignSpawner();
        }

        private void TrySubscribeCampaignSpawner()
        {
            CampaignDuckSpawner spawner = FindObjectOfType<CampaignDuckSpawner>();
            if (spawner == subscribedCampaignSpawner)
                return;

            UnsubscribeCampaignSpawner();
            subscribedCampaignSpawner = spawner;
            if (subscribedCampaignSpawner != null)
                subscribedCampaignSpawner.OnCampaignDuckSpawned += HandleCampaignDuckSpawned;
        }

        private void UnsubscribeCampaignSpawner()
        {
            if (subscribedCampaignSpawner != null)
                subscribedCampaignSpawner.OnCampaignDuckSpawned -= HandleCampaignDuckSpawned;

            subscribedCampaignSpawner = null;
        }

        private void HandleCampaignDuckSpawned(Constants.DuckType duckType, StageConfig stage, CityConfig city)
        {
            RecordCampaignDuckSpawned(duckType);
        }

        private void HandleBirdKilled(Constants.DuckType duckType, Constants.WeaponType weaponType)
        {
            if (GameManager.Instance.CurrentGameMode != Constants.GameMode.Campaign || !HasCatalogEntry(duckType))
                return;

            int next = GetKillCount(duckType) + 1;
            killsByDuck[duckType] = next;
            PlayerPrefs.SetInt(GetKillsKey(duckType), next);
            PlayerPrefs.Save();
            OnDuckKillCountChanged?.Invoke(duckType, next);
        }

        private bool HasCatalogEntry(Constants.DuckType duckType)
        {
            if (Catalog == null)
                return false;

            foreach (DuckTrophyEntry entry in Catalog.Entries)
            {
                if (entry.duckType == duckType)
                    return true;
            }

            return false;
        }

        private bool IsCityRewardClaimed(DuckTrophyCity city)
        {
            return rewardClaimedByCity.TryGetValue(city, out bool claimed) && claimed;
        }

        private void NotifyClaimableBadgeIfChanged()
        {
            int current = GetClaimableRewardCount();
            if (current == previousClaimableRewardCount)
                return;

            previousClaimableRewardCount = current;
            OnClaimableBadgeChanged?.Invoke();
        }

        private void EnsureSchemaVersion()
        {
            int storedVersion = PlayerPrefs.GetInt(SchemaVersionKey, 0);
            if (storedVersion >= CurrentSchemaVersion)
                return;

            PlayerPrefs.SetInt(SchemaVersionKey, CurrentSchemaVersion);
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            discoveredByDuck.Clear();
            killsByDuck.Clear();
            rewardClaimedByCity.Clear();

            if (Catalog != null)
            {
                foreach (DuckTrophyEntry entry in Catalog.Entries)
                {
                    discoveredByDuck[entry.duckType] = PlayerPrefs.GetInt(GetDiscoveredKey(entry.duckType), 0) == 1;
                    killsByDuck[entry.duckType] = Mathf.Max(0, PlayerPrefs.GetInt(GetKillsKey(entry.duckType), 0));
                }
            }

            for (int i = 0; i < DuckTrophyCatalog.CampaignCityOrder.Length; i++)
            {
                DuckTrophyCity city = DuckTrophyCatalog.CampaignCityOrder[i];
                rewardClaimedByCity[city] = PlayerPrefs.GetInt(GetCityRewardClaimedKey(city), 0) == 1;
            }
        }

        private static string GetDiscoveredKey(Constants.DuckType duckType)
        {
            return $"{PrefsPrefix}_{duckType}_Discovered";
        }

        private static string GetKillsKey(Constants.DuckType duckType)
        {
            return $"{PrefsPrefix}_{duckType}_Kills";
        }

        private static string GetCityRewardClaimedKey(DuckTrophyCity city)
        {
            return $"{PrefsPrefix}_City_{DuckTrophyCatalog.GetCityKey(city)}_RewardClaimed";
        }
    }
}
