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
            Kill15,
            Kill30,
            Kill50,
            Combo3,
            Combo6,
            TripleOrBetter2,
            Elite3,
            Elite6,
            Finish1,
            Finish2
        }

        private enum ObjectiveType
        {
            BirdKills,
            ComboAny,
            ComboTripleOrBetter,
            EliteKills,
            GameCompleted
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
        }

        private const string PrefsPrefix = "DailyAwards";
        private const string DayKey = PrefsPrefix + "_CurrentDay";
        private const string SelectedIdsKey = PrefsPrefix + "_SelectedObjectiveIds";
        private const string SetBonusGrantedKey = PrefsPrefix + "_SetBonusGranted";
        private const int DailyObjectiveCount = 3;
        private const int DailyObjectiveRewardCoins = 5;
        private const int DailySetBonusCoins = 20;

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

        private readonly Dictionary<DailyObjectiveId, DailyObjectiveDefinition> definitions =
            new Dictionary<DailyObjectiveId, DailyObjectiveDefinition>();

        private readonly List<DailyObjectiveId> selectedObjectiveIds = new List<DailyObjectiveId>(DailyObjectiveCount);
        private readonly int[] progressBySlot = new int[DailyObjectiveCount];
        private readonly bool[] completedBySlot = new bool[DailyObjectiveCount];
        private readonly bool[] rewardGrantedBySlot = new bool[DailyObjectiveCount];
        private bool setBonusGranted;
        private CampaignDuckSpawner subscribedCampaignSpawner;

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
            EnsureTodayInitialized();
            SubscribeToGameManager();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TrySubscribeCampaignSpawner();
        }

        private void OnDestroy()
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
                return;

            gameManager.OnBirdKilled -= HandleBirdKilled;
            gameManager.OnComboKillDetailed -= HandleComboKillDetailed;
            gameManager.OnGameOver -= HandleGameOver;

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
            PlayerPrefs.SetInt(SetBonusGrantedKey, 0);
            PlayerPrefs.Save();
            Debug.Log("[DailyAwards] Debug reset: today's objectives progress set to 0.");
        }

        private void SubscribeToGameManager()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.OnBirdKilled -= HandleBirdKilled;
            GameManager.Instance.OnBirdKilled += HandleBirdKilled;
            GameManager.Instance.OnComboKillDetailed -= HandleComboKillDetailed;
            GameManager.Instance.OnComboKillDetailed += HandleComboKillDetailed;
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        private void RegisterDefinitions()
        {
            definitions.Clear();
            AddDefinition(DailyObjectiveId.Kill15, ObjectiveType.BirdKills, 15);
            AddDefinition(DailyObjectiveId.Kill30, ObjectiveType.BirdKills, 30);
            AddDefinition(DailyObjectiveId.Kill50, ObjectiveType.BirdKills, 50);
            AddDefinition(DailyObjectiveId.Combo3, ObjectiveType.ComboAny, 3);
            AddDefinition(DailyObjectiveId.Combo6, ObjectiveType.ComboAny, 6);
            AddDefinition(DailyObjectiveId.TripleOrBetter2, ObjectiveType.ComboTripleOrBetter, 2);
            AddDefinition(DailyObjectiveId.Elite3, ObjectiveType.EliteKills, 3);
            AddDefinition(DailyObjectiveId.Elite6, ObjectiveType.EliteKills, 6);
            AddDefinition(DailyObjectiveId.Finish1, ObjectiveType.GameCompleted, 1);
            AddDefinition(DailyObjectiveId.Finish2, ObjectiveType.GameCompleted, 2);
        }

        private void AddDefinition(DailyObjectiveId id, ObjectiveType type, int target)
        {
            definitions[id] = new DailyObjectiveDefinition
            {
                id = id,
                type = type,
                target = Mathf.Max(1, target),
                titleKey = $"daily.{id}.title",
                coinReward = DailyObjectiveRewardCoins
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
                case ObjectiveType.GameCompleted:
                    formatKey = definition.target == 1
                        ? "daily.description.game_completed_singular"
                        : "daily.description.game_completed";
                    fallbackFormat = definition.target == 1 ? "Finish {0} game." : "Finish {0} games.";
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
                }
            }
        }

        private void HandleComboKillDetailed(Constants.MultiKillType comboType, Constants.WeaponType weaponType, int bonusPoints, Vector3 position)
        {
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

        private void HandleGameOver()
        {
            IncrementGameCompletedObjectives();
        }

        private void HandleCampaignStageResolved()
        {
            IncrementGameCompletedObjectives();
        }

        private void IncrementGameCompletedObjectives()
        {
            for (int slot = 0; slot < selectedObjectiveIds.Count; slot++)
            {
                if (completedBySlot[slot])
                    continue;

                DailyObjectiveDefinition def = definitions[selectedObjectiveIds[slot]];
                if (def.type == ObjectiveType.GameCompleted)
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
                Debug.Log($"[DailyAwards] Objective completed: {def.id} (+{def.coinReward} coins)");
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
            Debug.Log($"[DailyAwards] Daily set completed (+{DailySetBonusCoins} coins)");
        }

        private void GenerateNewDay(string dayToken)
        {
            selectedObjectiveIds.Clear();

            List<DailyObjectiveId> pool = definitions.Keys.ToList();
            Shuffle(pool, BuildStableSeed(dayToken));
            for (int i = 0; i < DailyObjectiveCount && i < pool.Count; i++)
                selectedObjectiveIds.Add(pool[i]);

            while (selectedObjectiveIds.Count < DailyObjectiveCount)
                selectedObjectiveIds.Add(DailyObjectiveId.Kill15);

            Array.Clear(progressBySlot, 0, progressBySlot.Length);
            Array.Clear(completedBySlot, 0, completedBySlot.Length);
            Array.Clear(rewardGrantedBySlot, 0, rewardGrantedBySlot.Length);
            setBonusGranted = false;

            PlayerPrefs.SetString(DayKey, dayToken);
            PlayerPrefs.SetString(SelectedIdsKey, string.Join(",", selectedObjectiveIds.Select(id => ((int)id).ToString()).ToArray()));
            PlayerPrefs.SetInt(SetBonusGrantedKey, 0);
            for (int slot = 0; slot < DailyObjectiveCount; slot++)
            {
                PlayerPrefs.SetInt(GetProgressKey(slot), 0);
                PlayerPrefs.SetInt(GetCompletedKey(slot), 0);
                PlayerPrefs.SetInt(GetRewardGrantedKey(slot), 0);
            }

            PlayerPrefs.Save();
            Debug.Log($"[DailyAwards] New day initialized: {dayToken}");
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
                   duckType == Constants.DuckType.FRENCH_REVOLUTIONARY;
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
