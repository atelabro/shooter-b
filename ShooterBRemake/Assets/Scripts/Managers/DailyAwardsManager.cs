using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShooterB
{
    public class DailyAwardsManager : MonoBehaviour
    {
        public enum DailyObjectiveId
        {
            Kill40,
            Kill80,
            Kill120,
            Kill160,
            Combo8,
            Combo16,
            Combo24,
            TripleOrBetter4,
            TripleOrBetter8,
            TripleOrBetter12,
            Elite8,
            Elite15,
            Boss5,
            Boss8,
            Complete2,
            Complete4,
            Complete5
        }

        private enum ObjectiveType
        {
            BirdKills,
            ComboAny,
            ComboTripleOrBetter,
            EliteKills,
            BossKills,
            StageCompleted
        }

        public struct DailyObjectiveState
        {
            public int slotIndex;
            public DailyObjectiveId objectiveId;
            public string title;
            public string description;
            public int progress;
            public int target;
            public bool isCompleted;
            public bool rewardGranted;
            public int coinReward;

            public float NormalizedProgress => target <= 0 ? 0f : Mathf.Clamp01((float)progress / target);
        }

        private struct DailyObjectiveDefinition
        {
            public DailyObjectiveId id;
            public ObjectiveType type;
            public int target;
            public string titleKey;
            public int coinReward;
            public int minStageIndex;
        }

        private const string PrefsPrefix = "DailyAwards";
        private const string DayKey = PrefsPrefix + "_CurrentDay";
        private const string SelectedIdsKey = PrefsPrefix + "_SelectedObjectiveIds";
        private const string SetBonusGrantedKey = PrefsPrefix + "_SetBonusGranted";
        private const string AdWatchBonusGrantedKey = PrefsPrefix + "_AdWatchBonusGranted";
        private const string SchemaVersionKey = PrefsPrefix + "_SchemaVersion";
        private const int CurrentSchemaVersion = 3;
        private const int DailyObjectiveCount = 3;
        private const int DailySetBonusCoins = 30;
        private const int DailyAdWatchBonusCoins = 10;

        private static DailyAwardsManager instance;
        public static DailyAwardsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("DailyAwardsManager");
                    instance = go.AddComponent<DailyAwardsManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public event Action<int> OnDailyObjectiveProgressChanged;
        public event Action<int> OnDailyObjectiveCompleted;
        public event Action OnDailySetCompleted;
        public event Action OnDailyAdWatchBonusClaimed;

        private readonly Dictionary<DailyObjectiveId, DailyObjectiveDefinition> definitions =
            new Dictionary<DailyObjectiveId, DailyObjectiveDefinition>();

        private readonly List<DailyObjectiveId> selectedObjectiveIds = new List<DailyObjectiveId>(DailyObjectiveCount);
        private readonly int[] progressBySlot = new int[DailyObjectiveCount];
        private readonly bool[] completedBySlot = new bool[DailyObjectiveCount];
        private readonly bool[] rewardGrantedBySlot = new bool[DailyObjectiveCount];
        private bool setBonusGranted;
        private bool adWatchBonusGranted;
        private CampaignDuckSpawner subscribedCampaignSpawner;
        public RewardedAdResult LastAdWatchBonusAttemptResult { get; private set; } = RewardedAdResult.Unavailable;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterDefinitions();
            EnsureSchemaVersion();
            EnsureTodayInitialized();
            SubscribeToGameManager();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TrySubscribeCampaignSpawner();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBirdKilled -= HandleBirdKilled;
                GameManager.Instance.OnComboKillDetailed -= HandleComboKillDetailed;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribeCampaignSpawner();
        }

        public void EnsureTodayInitialized()
        {
            string todayToken = GetTodayToken();
            string storedToken = PlayerPrefs.GetString(DayKey, string.Empty);

            if (storedToken != todayToken || !TryLoadSelectedObjectives())
            {
                GenerateNewDay(todayToken);
                return;
            }

            LoadSlotState();
        }

        public IReadOnlyList<DailyObjectiveState> GetTodayObjectives()
        {
            EnsureTodayInitialized();

            List<DailyObjectiveState> result = new List<DailyObjectiveState>(DailyObjectiveCount);
            for (int slot = 0; slot < selectedObjectiveIds.Count; slot++)
            {
                DailyObjectiveId id = selectedObjectiveIds[slot];
                if (!definitions.TryGetValue(id, out DailyObjectiveDefinition def))
                    continue;

                result.Add(new DailyObjectiveState
                {
                    slotIndex = slot,
                    objectiveId = id,
                    title = LocalizationManager.Instance.Get(def.titleKey, id.ToString()),
                    description = GetLocalizedDescription(def),
                    progress = progressBySlot[slot],
                    target = def.target,
                    isCompleted = completedBySlot[slot],
                    rewardGranted = rewardGrantedBySlot[slot],
                    coinReward = def.coinReward
                });
            }

            return result;
        }

        public int GetCompletedTodayCount()
        {
            int completedCount = 0;
            for (int i = 0; i < selectedObjectiveIds.Count; i++)
            {
                if (completedBySlot[i])
                    completedCount++;
            }

            return completedCount;
        }

        public int GetUnfinishedTodayCount()
        {
            IReadOnlyList<DailyObjectiveState> objectives = GetTodayObjectives();
            int unfinishedCount = 0;
            for (int i = 0; i < objectives.Count; i++)
            {
                if (!objectives[i].isCompleted)
                    unfinishedCount++;
            }

            return unfinishedCount;
        }

        public bool IsTodaySetComplete()
        {
            return GetCompletedTodayCount() >= DailyObjectiveCount;
        }

        public int GetDailySetBonusCoins()
        {
            return DailySetBonusCoins;
        }

        public bool IsDailySetBonusGranted()
        {
            return setBonusGranted;
        }

        public int GetDailyAdWatchBonusCoins()
        {
            return DailyAdWatchBonusCoins;
        }

        public bool IsDailyAdWatchBonusGranted()
        {
            return adWatchBonusGranted;
        }

        public bool CanClaimDailyAdWatchBonus()
        {
            return IsTodaySetComplete() && !adWatchBonusGranted;
        }

        public bool TryClaimDailyAdWatchBonus(string source, Action<RewardedAdResult> onCompleted)
        {
            EnsureTodayInitialized();

            if (!CanClaimDailyAdWatchBonus())
                return false;

            RewardedAdService.Instance.ShowRewardedAd(
                RewardedAdPlacement.DailyAwardsSetBonus,
                source,
                result =>
                {
                    LastAdWatchBonusAttemptResult = result;
                    if (result != RewardedAdResult.Completed)
                    {
                        GameLog.Log($"[DailyAwards] Ad watch bonus not granted from {source}. Result={result}");
                        onCompleted?.Invoke(result);
                        return;
                    }

                    adWatchBonusGranted = true;
                    PlayerPrefs.SetInt(AdWatchBonusGrantedKey, 1);
                    GameManager.Instance.AddCoins(DailyAdWatchBonusCoins);
                    PlayerPrefs.Save();
                    OnDailyAdWatchBonusClaimed?.Invoke();
                    GameLog.Log($"[DailyAwards] Ad watch bonus claimed from {source} (+{DailyAdWatchBonusCoins} coins)");
                    onCompleted?.Invoke(result);
                });

            return true;
        }

        public void DebugResetTodayProgress()
        {
            EnsureTodayInitialized();

            for (int slot = 0; slot < selectedObjectiveIds.Count; slot++)
            {
                progressBySlot[slot] = 0;
                completedBySlot[slot] = false;
                rewardGrantedBySlot[slot] = false;
                PlayerPrefs.SetInt(GetProgressKey(slot), 0);
                PlayerPrefs.SetInt(GetCompletedKey(slot), 0);
                PlayerPrefs.SetInt(GetRewardGrantedKey(slot), 0);
                OnDailyObjectiveProgressChanged?.Invoke(slot);
            }

            setBonusGranted = false;
            adWatchBonusGranted = false;
            PlayerPrefs.SetInt(SetBonusGrantedKey, 0);
            PlayerPrefs.SetInt(AdWatchBonusGrantedKey, 0);
            PlayerPrefs.Save();
            GameLog.Log("[DailyAwards] Debug reset: today's objectives progress set to 0.");
        }

        private void SubscribeToGameManager()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.OnBirdKilled -= HandleBirdKilled;
            GameManager.Instance.OnBirdKilled += HandleBirdKilled;
            GameManager.Instance.OnComboKillDetailed -= HandleComboKillDetailed;
            GameManager.Instance.OnComboKillDetailed += HandleComboKillDetailed;
        }

        private void RegisterDefinitions()
        {
            definitions.Clear();
            AddDefinition(DailyObjectiveId.Kill40, ObjectiveType.BirdKills, 40, 8);
            AddDefinition(DailyObjectiveId.Kill80, ObjectiveType.BirdKills, 80, 10);
            AddDefinition(DailyObjectiveId.Kill120, ObjectiveType.BirdKills, 120, 12, minStageIndex: 12);
            AddDefinition(DailyObjectiveId.Kill160, ObjectiveType.BirdKills, 160, 16, minStageIndex: 24);
            AddDefinition(DailyObjectiveId.Combo8, ObjectiveType.ComboAny, 8, 8);
            AddDefinition(DailyObjectiveId.Combo16, ObjectiveType.ComboAny, 16, 12, minStageIndex: 10);
            AddDefinition(DailyObjectiveId.Combo24, ObjectiveType.ComboAny, 24, 16, minStageIndex: 24);
            AddDefinition(DailyObjectiveId.TripleOrBetter4, ObjectiveType.ComboTripleOrBetter, 4, 10);
            AddDefinition(DailyObjectiveId.TripleOrBetter8, ObjectiveType.ComboTripleOrBetter, 8, 14, minStageIndex: 12);
            AddDefinition(DailyObjectiveId.TripleOrBetter12, ObjectiveType.ComboTripleOrBetter, 12, 18, minStageIndex: 24);
            AddDefinition(DailyObjectiveId.Elite8, ObjectiveType.EliteKills, 8, 10);
            AddDefinition(DailyObjectiveId.Elite15, ObjectiveType.EliteKills, 15, 14, minStageIndex: 12);
            AddDefinition(DailyObjectiveId.Boss5, ObjectiveType.BossKills, 5, 16, minStageIndex: 12);
            AddDefinition(DailyObjectiveId.Boss8, ObjectiveType.BossKills, 8, 20, minStageIndex: 24);
            AddDefinition(DailyObjectiveId.Complete2, ObjectiveType.StageCompleted, 2, 10);
            AddDefinition(DailyObjectiveId.Complete4, ObjectiveType.StageCompleted, 4, 16, minStageIndex: 12);
            AddDefinition(DailyObjectiveId.Complete5, ObjectiveType.StageCompleted, 5, 20, minStageIndex: 24);
        }

        private void AddDefinition(DailyObjectiveId id, ObjectiveType type, int target, int coinReward, int minStageIndex = 3)
        {
            definitions[id] = new DailyObjectiveDefinition
            {
                id = id,
                type = type,
                target = Mathf.Max(1, target),
                titleKey = $"daily.{id}.title",
                coinReward = Mathf.Max(0, coinReward),
                minStageIndex = Mathf.Max(3, minStageIndex)
            };
        }

        private static string GetLocalizedDescription(DailyObjectiveDefinition definition)
        {
            string formatKey;
            string fallbackFormat;

            switch (definition.type)
            {
                case ObjectiveType.BirdKills:
                    formatKey = "daily.description.bird_kills";
                    fallbackFormat = "Kill {0} ducks.";
                    break;
                case ObjectiveType.ComboAny:
                    formatKey = "daily.description.combo_any";
                    fallbackFormat = "Trigger {0} combos.";
                    break;
                case ObjectiveType.ComboTripleOrBetter:
                    formatKey = "daily.description.combo_triple_or_better";
                    fallbackFormat = "Get {0} triple-or-better combos.";
                    break;
                case ObjectiveType.EliteKills:
                    formatKey = "daily.description.elite_kills";
                    fallbackFormat = "Kill {0} elite ducks.";
                    break;
                case ObjectiveType.BossKills:
                    formatKey = "daily.description.boss_kills";
                    fallbackFormat = "Kill {0} boss ducks.";
                    break;
                case ObjectiveType.StageCompleted:
                    formatKey = definition.target == 1
                        ? "daily.description.stage_completed_singular"
                        : "daily.description.stage_completed";
                    fallbackFormat = definition.target == 1
                        ? "Complete {0} campaign stage."
                        : "Complete {0} campaign stages.";
                    break;
                default:
                    formatKey = "daily.description.bird_kills";
                    fallbackFormat = "Kill {0} ducks.";
                    break;
            }

            string format = LocalizationManager.Instance.Get(formatKey, fallbackFormat);
            return string.Format(format, definition.target);
        }

        private void HandleBirdKilled(Constants.DuckType duckType, Constants.WeaponType weaponType)
        {
            if (!IsEligibleCampaignProgress())
                return;

            for (int slot = 0; slot < selectedObjectiveIds.Count; slot++)
            {
                if (completedBySlot[slot])
                    continue;

                DailyObjectiveDefinition def = definitions[selectedObjectiveIds[slot]];
                switch (def.type)
                {
                    case ObjectiveType.BirdKills:
                        IncrementSlot(slot, 1);
                        break;
                    case ObjectiveType.EliteKills:
                        if (IsEliteDuck(duckType))
                            IncrementSlot(slot, 1);
                        break;
                    case ObjectiveType.BossKills:
                        if (duckType == Constants.DuckType.MK_SAMUIL_BOSS_DUCK ||
                            duckType == Constants.DuckType.USA_BOSS_DUCK ||
                            duckType == Constants.DuckType.FRENCH_MUSKETEER_BOSS_DUCK ||
                            duckType == Constants.DuckType.JAPANESE_SAMURAI_BOSS_DUCK ||
                            duckType == Constants.DuckType.BRITISH_SHERLOCK_BOSS_DUCK)
                            IncrementSlot(slot, 1);
                        break;
                }
            }
        }

        private void HandleComboKillDetailed(Constants.MultiKillType comboType, Constants.WeaponType weaponType, int bonusPoints, Vector3 position)
        {
            if (!IsEligibleCampaignProgress())
                return;

            for (int slot = 0; slot < selectedObjectiveIds.Count; slot++)
            {
                if (completedBySlot[slot])
                    continue;

                DailyObjectiveDefinition def = definitions[selectedObjectiveIds[slot]];
                switch (def.type)
                {
                    case ObjectiveType.ComboAny:
                        IncrementSlot(slot, 1);
                        break;
                    case ObjectiveType.ComboTripleOrBetter:
                        if (comboType == Constants.MultiKillType.TripleKill || comboType == Constants.MultiKillType.QuadraKill)
                            IncrementSlot(slot, 1);
                        break;
                }
            }
        }

        private void HandleCampaignStageResolved()
        {
            if (!IsEligibleCampaignProgress())
                return;

            for (int slot = 0; slot < selectedObjectiveIds.Count; slot++)
            {
                if (completedBySlot[slot])
                    continue;

                DailyObjectiveDefinition def = definitions[selectedObjectiveIds[slot]];
                if (def.type == ObjectiveType.StageCompleted)
                    IncrementSlot(slot, 1);
            }
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
                subscribedCampaignSpawner.OnAllDucksResolved += HandleCampaignStageResolved;
        }

        private void UnsubscribeCampaignSpawner()
        {
            if (subscribedCampaignSpawner != null)
                subscribedCampaignSpawner.OnAllDucksResolved -= HandleCampaignStageResolved;

            subscribedCampaignSpawner = null;
        }

        private void IncrementSlot(int slot, int amount)
        {
            if (slot < 0 || slot >= selectedObjectiveIds.Count)
                return;

            DailyObjectiveDefinition def = definitions[selectedObjectiveIds[slot]];
            int current = progressBySlot[slot];
            int next = Mathf.Min(def.target, current + Mathf.Max(0, amount));
            if (next == current)
                return;

            progressBySlot[slot] = next;
            PlayerPrefs.SetInt(GetProgressKey(slot), next);
            OnDailyObjectiveProgressChanged?.Invoke(slot);

            if (next >= def.target && !completedBySlot[slot])
            {
                completedBySlot[slot] = true;
                PlayerPrefs.SetInt(GetCompletedKey(slot), 1);

                if (!rewardGrantedBySlot[slot] && def.coinReward > 0)
                {
                    rewardGrantedBySlot[slot] = true;
                    PlayerPrefs.SetInt(GetRewardGrantedKey(slot), 1);
                    GameManager.Instance.AddCoins(def.coinReward);
                }

                OnDailyObjectiveCompleted?.Invoke(slot);
                GameLog.Log($"[DailyAwards] Objective completed: {def.id} (+{def.coinReward} coins)");
                TryGrantSetBonus();
            }

            PlayerPrefs.Save();
        }

        private void TryGrantSetBonus()
        {
            if (setBonusGranted || !IsTodaySetComplete())
                return;

            setBonusGranted = true;
            PlayerPrefs.SetInt(SetBonusGrantedKey, 1);
            GameManager.Instance.AddCoins(DailySetBonusCoins);
            OnDailySetCompleted?.Invoke();
            PlayerPrefs.Save();
            GameLog.Log($"[DailyAwards] Daily set completed (+{DailySetBonusCoins} coins)");
        }

        private void EnsureSchemaVersion()
        {
            int storedVersion = PlayerPrefs.GetInt(SchemaVersionKey, 0);
            if (storedVersion >= CurrentSchemaVersion)
                return;

            ResetDailyState();
            PlayerPrefs.SetInt(SchemaVersionKey, CurrentSchemaVersion);
            PlayerPrefs.Save();
            GameLog.Log($"[DailyAwards] Schema upgraded {storedVersion} -> {CurrentSchemaVersion}. Daily progress reset.");
        }

        private void ResetDailyState()
        {
            PlayerPrefs.DeleteKey(DayKey);
            PlayerPrefs.DeleteKey(SelectedIdsKey);
            PlayerPrefs.DeleteKey(SetBonusGrantedKey);
            PlayerPrefs.DeleteKey(AdWatchBonusGrantedKey);

            for (int slot = 0; slot < DailyObjectiveCount; slot++)
            {
                PlayerPrefs.DeleteKey(GetProgressKey(slot));
                PlayerPrefs.DeleteKey(GetCompletedKey(slot));
                PlayerPrefs.DeleteKey(GetRewardGrantedKey(slot));
            }

            selectedObjectiveIds.Clear();
            Array.Clear(progressBySlot, 0, progressBySlot.Length);
            Array.Clear(completedBySlot, 0, completedBySlot.Length);
            Array.Clear(rewardGrantedBySlot, 0, rewardGrantedBySlot.Length);
            setBonusGranted = false;
            adWatchBonusGranted = false;
        }

        private void GenerateNewDay(string dayToken)
        {
            selectedObjectiveIds.Clear();

            List<DailyObjectiveId> pool = GetAvailableObjectivePool();
            Shuffle(pool, BuildStableSeed(dayToken));
            for (int i = 0; i < DailyObjectiveCount && i < pool.Count; i++)
                selectedObjectiveIds.Add(pool[i]);

            while (selectedObjectiveIds.Count < DailyObjectiveCount)
                selectedObjectiveIds.Add(DailyObjectiveId.Kill40);

            Array.Clear(progressBySlot, 0, progressBySlot.Length);
            Array.Clear(completedBySlot, 0, completedBySlot.Length);
            Array.Clear(rewardGrantedBySlot, 0, rewardGrantedBySlot.Length);
            setBonusGranted = false;
            adWatchBonusGranted = false;

            PlayerPrefs.SetString(DayKey, dayToken);
            PlayerPrefs.SetString(SelectedIdsKey, string.Join(",", selectedObjectiveIds.Select(id => ((int)id).ToString()).ToArray()));
            PlayerPrefs.SetInt(SetBonusGrantedKey, 0);
            PlayerPrefs.SetInt(AdWatchBonusGrantedKey, 0);
            for (int slot = 0; slot < DailyObjectiveCount; slot++)
            {
                PlayerPrefs.SetInt(GetProgressKey(slot), 0);
                PlayerPrefs.SetInt(GetCompletedKey(slot), 0);
                PlayerPrefs.SetInt(GetRewardGrantedKey(slot), 0);
            }

            PlayerPrefs.Save();
            GameLog.Log($"[DailyAwards] New day initialized: {dayToken}");
        }

        private List<DailyObjectiveId> GetAvailableObjectivePool()
        {
            int highestUnlockedStageIndex = GetHighestUnlockedStageIndex();
            List<DailyObjectiveId> pool = definitions
                .Where(entry => highestUnlockedStageIndex >= entry.Value.minStageIndex)
                .Select(entry => entry.Key)
                .ToList();

            if (pool.Count >= DailyObjectiveCount)
                return pool;

            return definitions.Keys.ToList();
        }

        private static int GetHighestUnlockedStageIndex()
        {
            CampaignProgressManager progress = CampaignProgressManager.Instance;
            CityConfig[] cities = progress.CampaignCities;
            if (cities == null || cities.Length == 0)
                return GetHighestUnlockedStageIndexFromPrefs();

            int highestUnlockedStageIndex = 3;
            for (int cityIndex = 0; cityIndex < cities.Length; cityIndex++)
            {
                CityConfig city = cities[cityIndex];
                if (city == null || city.stages == null)
                    continue;

                for (int stageIndex = 0; stageIndex < city.stages.Length; stageIndex++)
                {
                    StageConfig stage = city.stages[stageIndex];
                    if (stage == null)
                        continue;

                    if (progress.IsStageUnlocked(stage, city, cities))
                        highestUnlockedStageIndex = Mathf.Max(highestUnlockedStageIndex, stage.stageIndex);
                }
            }

            return highestUnlockedStageIndex;
        }

        private static int GetHighestUnlockedStageIndexFromPrefs()
        {
            int highestUnlockedStageIndex = 3;
            for (int stageIndex = 3; stageIndex <= 128; stageIndex++)
            {
                if (PlayerPrefs.GetInt($"Campaign_Stage_{stageIndex}_Stars", 0) >= 1)
                    highestUnlockedStageIndex = stageIndex;
            }

            return highestUnlockedStageIndex;
        }

        private void LoadSlotState()
        {
            for (int slot = 0; slot < DailyObjectiveCount; slot++)
            {
                DailyObjectiveDefinition def = definitions[selectedObjectiveIds[slot]];
                int progress = Mathf.Max(0, PlayerPrefs.GetInt(GetProgressKey(slot), 0));
                progressBySlot[slot] = Mathf.Clamp(progress, 0, def.target);
                completedBySlot[slot] = PlayerPrefs.GetInt(GetCompletedKey(slot), 0) == 1 || progressBySlot[slot] >= def.target;
                rewardGrantedBySlot[slot] = PlayerPrefs.GetInt(GetRewardGrantedKey(slot), 0) == 1;
            }

            setBonusGranted = PlayerPrefs.GetInt(SetBonusGrantedKey, 0) == 1;
            adWatchBonusGranted = PlayerPrefs.GetInt(AdWatchBonusGrantedKey, 0) == 1;
        }

        private bool TryLoadSelectedObjectives()
        {
            string selectedIdsCsv = PlayerPrefs.GetString(SelectedIdsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(selectedIdsCsv))
                return false;

            string[] parts = selectedIdsCsv.Split(',');
            if (parts.Length != DailyObjectiveCount)
                return false;

            selectedObjectiveIds.Clear();
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out int rawId))
                    return false;

                DailyObjectiveId id = (DailyObjectiveId)rawId;
                if (!definitions.ContainsKey(id))
                    return false;

                selectedObjectiveIds.Add(id);
            }

            return true;
        }

        private static bool IsEliteDuck(Constants.DuckType duckType)
        {
            return duckType == Constants.DuckType.MK_PHALARX ||
                   duckType == Constants.DuckType.MK_ARCHER ||
                   duckType == Constants.DuckType.MK_VOJVODA ||
                   duckType == Constants.DuckType.FRENCH_REVOLUTIONARY ||
                   duckType == Constants.DuckType.FRENCH_MUSKETEER ||
                   duckType == Constants.DuckType.USA_ADMIRAL_ELITE;
        }

        private static bool IsEligibleCampaignProgress()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentGameMode != Constants.GameMode.Campaign)
                return false;

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;
            return stage != null && stage.stageIndex >= 3;
        }

        private static string GetTodayToken()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        private static int BuildStableSeed(string token)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < token.Length; i++)
                    hash = (hash * 31) + token[i];

                return hash;
            }
        }

        private static void Shuffle<T>(IList<T> list, int seed)
        {
            System.Random random = new System.Random(seed);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private static string GetProgressKey(int slot)
        {
            return $"{PrefsPrefix}_Slot{slot}_Progress";
        }

        private static string GetCompletedKey(int slot)
        {
            return $"{PrefsPrefix}_Slot{slot}_Completed";
        }

        private static string GetRewardGrantedKey(int slot)
        {
            return $"{PrefsPrefix}_Slot{slot}_RewardGranted";
        }
    }
}
