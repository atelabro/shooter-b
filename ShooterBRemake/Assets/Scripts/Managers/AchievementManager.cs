using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public class AchievementManager : MonoBehaviour
    {
        public enum AchievementId
        {
            FieldPromotion,
            CityCleaner,
            ResistanceCracker,
            UprisingBreaker,
            NoFlyZone,
            ExterminatorGeneral,
            EliteControlI,
            EliteControlII,
            EliteControlIII,
            EliteControlIV,
            BossSlayerI,
            BossSlayerII,
            BossSlayerIII,
            ArcherCleanupI,
            ArcherCleanupII,
            PhalarxBreakerI,
            PhalarxBreakerII,
            RifleVeteran,
            CabirnePrecision,
            BerettaStorm,
            SulkoRampage,
            LaserSweep,
            TeslaChainLord,
            PiranhaMassacre,
            DoubleDownI,
            DoubleDownII,
            TripleThreatI,
            TripleThreatII,
            OverkillI,
            OverkillII
        }

        private enum ProgressSource
        {
            ComboKill,
            BirdKill
        }

        private enum DuckFilterMode
        {
            Any,
            Exact,
            EliteOnly
        }

        private struct AchievementDefinition
        {
            public AchievementId id;
            public ProgressSource progressSource;
            public Constants.MultiKillType? comboType;
            public DuckFilterMode duckFilterMode;
            public Constants.DuckType duckType;
            public Constants.WeaponType? weaponType;
            public int targetCount;
            public int coinReward;
            public string titleKey;
        }

        private const int CurrentSchemaVersion = 2;
        private const string SchemaVersionKey = "Achievement_SchemaVersion";

        private static readonly string[] LegacyAchievementIdsToClear =
        {
            "BirdBlenderI",
            "BirdBlenderII",
            "BossSlayer",
            "CommanderDown",
            "DuckHunterI",
            "DuckHunterII",
            "DuckHunterIII",
            "DuckHunterIV",
            "DuckHunter10",
            "EliteControlI",
            "EliteControlII",
            "ArcherCleanup",
            "LaserSweep",
            "OverkillI",
            "OverkillII",
            "PhalarxBreaker",
            "PiranhaDoubleTrouble",
            "PiranhaMassacre",
            "PiranhaDoubleKill50",
            "RifleVeteran",
            "SniperPrecision",
            "SulkoRampage",
            "TeslaChainLord",
            "TripleThreatI",
            "TripleThreatII",
            "BerettaSpray"
        };

        private static AchievementManager instance;
        public static AchievementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AchievementManager");
                    instance = go.AddComponent<AchievementManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        private readonly Dictionary<AchievementId, AchievementDefinition> definitions = new Dictionary<AchievementId, AchievementDefinition>();
        private readonly Dictionary<AchievementId, int> progress = new Dictionary<AchievementId, int>();
        private readonly HashSet<AchievementId> unlocked = new HashSet<AchievementId>();

        public event Action<AchievementId> OnAchievementUnlocked;
        public event Action<AchievementId> OnAchievementProgressChanged;

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
            LoadState();
            SubscribeToGameManager();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.OnComboKillDetailed -= HandleComboKillDetailed;
            GameManager.Instance.OnBirdKilled -= HandleBirdKilled;
        }

        public int GetProgress(AchievementId id)
        {
            return progress.TryGetValue(id, out int value) ? value : 0;
        }

        public int GetTarget(AchievementId id)
        {
            return definitions.TryGetValue(id, out AchievementDefinition definition) ? definition.targetCount : 0;
        }

        public bool IsUnlocked(AchievementId id)
        {
            return unlocked.Contains(id);
        }

        public bool GetIsUnlocked(AchievementId id)
        {
            return IsUnlocked(id);
        }

        public string GetTitle(AchievementId id)
        {
            if (!definitions.TryGetValue(id, out AchievementDefinition definition))
                return id.ToString();

            return LocalizationManager.Instance.Get(definition.titleKey, id.ToString());
        }

        public string GetDescription(AchievementId id)
        {
            if (!definitions.TryGetValue(id, out AchievementDefinition definition))
                return string.Empty;

            return definition.progressSource == ProgressSource.ComboKill
                ? BuildComboDescription(definition)
                : BuildBirdDescription(definition);
        }

        public int GetCoinReward(AchievementId id)
        {
            return definitions.TryGetValue(id, out AchievementDefinition definition) ? definition.coinReward : 0;
        }

        public float GetNormalizedProgress(AchievementId id)
        {
            int target = GetTarget(id);
            if (target <= 0)
                return 0f;

            return Mathf.Clamp01((float)GetProgress(id) / target);
        }

        private void SubscribeToGameManager()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.OnComboKillDetailed -= HandleComboKillDetailed;
            GameManager.Instance.OnComboKillDetailed += HandleComboKillDetailed;
            GameManager.Instance.OnBirdKilled -= HandleBirdKilled;
            GameManager.Instance.OnBirdKilled += HandleBirdKilled;
        }

        private void RegisterDefinitions()
        {
            definitions.Clear();

            AddBirdAchievement(AchievementId.FieldPromotion, 200, coinReward: 10);
            AddBirdAchievement(AchievementId.CityCleaner, 600, coinReward: 20);
            AddBirdAchievement(AchievementId.ResistanceCracker, 1200, coinReward: 35);
            AddBirdAchievement(AchievementId.UprisingBreaker, 2500, coinReward: 60);
            AddBirdAchievement(AchievementId.NoFlyZone, 4000, coinReward: 90);
            AddBirdAchievement(AchievementId.ExterminatorGeneral, 7000, coinReward: 130);

            AddBirdAchievement(AchievementId.EliteControlI, 150, duckFilterMode: DuckFilterMode.EliteOnly, coinReward: 20);
            AddBirdAchievement(AchievementId.EliteControlII, 400, duckFilterMode: DuckFilterMode.EliteOnly, coinReward: 45);
            AddBirdAchievement(AchievementId.EliteControlIII, 800, duckFilterMode: DuckFilterMode.EliteOnly, coinReward: 80);
            AddBirdAchievement(AchievementId.EliteControlIV, 1300, duckFilterMode: DuckFilterMode.EliteOnly, coinReward: 120);

            AddBirdAchievement(AchievementId.BossSlayerI, 60, duckType: Constants.DuckType.USA_BOSS_DUCK, coinReward: 15);
            AddBirdAchievement(AchievementId.BossSlayerII, 180, duckType: Constants.DuckType.USA_BOSS_DUCK, coinReward: 40);
            AddBirdAchievement(AchievementId.BossSlayerIII, 360, duckType: Constants.DuckType.USA_BOSS_DUCK, coinReward: 80);

            AddBirdAchievement(AchievementId.ArcherCleanupI, 180, duckType: Constants.DuckType.MK_ARCHER, coinReward: 18);
            AddBirdAchievement(AchievementId.ArcherCleanupII, 420, duckType: Constants.DuckType.MK_ARCHER, coinReward: 45);

            AddBirdAchievement(AchievementId.PhalarxBreakerI, 180, duckType: Constants.DuckType.MK_PHALARX, coinReward: 22);
            AddBirdAchievement(AchievementId.PhalarxBreakerII, 420, duckType: Constants.DuckType.MK_PHALARX, coinReward: 55);

            AddBirdAchievement(AchievementId.RifleVeteran, 1200, weaponType: Constants.WeaponType.Rifle, coinReward: 20);
            AddBirdAchievement(AchievementId.CabirnePrecision, 400, weaponType: Constants.WeaponType.Cabirne, coinReward: 18);
            AddBirdAchievement(AchievementId.BerettaStorm, 700, weaponType: Constants.WeaponType.Beretta, coinReward: 22);
            AddBirdAchievement(AchievementId.SulkoRampage, 550, weaponType: Constants.WeaponType.MrSulko, coinReward: 24);
            AddBirdAchievement(AchievementId.LaserSweep, 900, weaponType: Constants.WeaponType.LaserGun, coinReward: 30);
            AddBirdAchievement(AchievementId.TeslaChainLord, 700, weaponType: Constants.WeaponType.TeslaGun, coinReward: 32);
            AddBirdAchievement(AchievementId.PiranhaMassacre, 650, weaponType: Constants.WeaponType.PiranhaGun, coinReward: 30);

            AddComboAchievement(AchievementId.DoubleDownI, 150, Constants.MultiKillType.DoubleKill, coinReward: 15);
            AddComboAchievement(AchievementId.DoubleDownII, 450, Constants.MultiKillType.DoubleKill, coinReward: 40);
            AddComboAchievement(AchievementId.TripleThreatI, 120, Constants.MultiKillType.TripleKill, coinReward: 20);
            AddComboAchievement(AchievementId.TripleThreatII, 320, Constants.MultiKillType.TripleKill, coinReward: 55);
            AddComboAchievement(AchievementId.OverkillI, 60, Constants.MultiKillType.QuadraKill, coinReward: 25);
            AddComboAchievement(AchievementId.OverkillII, 180, Constants.MultiKillType.QuadraKill, coinReward: 70);
        }

        private void AddBirdAchievement(
            AchievementId id,
            int targetCount,
            Constants.DuckType? duckType = null,
            Constants.WeaponType? weaponType = null,
            DuckFilterMode duckFilterMode = DuckFilterMode.Any,
            int coinReward = 0)
        {
            definitions[id] = new AchievementDefinition
            {
                id = id,
                progressSource = ProgressSource.BirdKill,
                comboType = null,
                duckFilterMode = duckType.HasValue ? DuckFilterMode.Exact : duckFilterMode,
                duckType = duckType ?? default,
                weaponType = weaponType,
                targetCount = targetCount,
                coinReward = Mathf.Max(0, coinReward),
                titleKey = GetTitleKey(id)
            };
        }

        private void AddComboAchievement(
            AchievementId id,
            int targetCount,
            Constants.MultiKillType comboType,
            int coinReward = 0)
        {
            definitions[id] = new AchievementDefinition
            {
                id = id,
                progressSource = ProgressSource.ComboKill,
                comboType = comboType,
                duckFilterMode = DuckFilterMode.Any,
                duckType = default,
                weaponType = null,
                targetCount = targetCount,
                coinReward = Mathf.Max(0, coinReward),
                titleKey = GetTitleKey(id)
            };
        }

        private static string GetTitleKey(AchievementId id)
        {
            return $"achievement.{id}.title";
        }

        private static string BuildComboLabel(Constants.MultiKillType comboType)
        {
            switch (comboType)
            {
                case Constants.MultiKillType.DoubleKill:
                    return LocalizationManager.Instance.Get("achievement.combo_label.double", "Double Kills");
                case Constants.MultiKillType.TripleKill:
                    return LocalizationManager.Instance.Get("achievement.combo_label.triple", "Triple Kills");
                default:
                    return LocalizationManager.Instance.Get("achievement.combo_label.quadra", "Quadra Kills");
            }
        }

        private static string GetDuckNameKey(Constants.DuckType duckType)
        {
            switch (duckType)
            {
                case Constants.DuckType.MK_VOJVODA:
                    return "duck.name.mk_vojvoda";
                case Constants.DuckType.MK_ARCHER:
                    return "duck.name.mk_archer";
                case Constants.DuckType.MK_PHALARX:
                    return "duck.name.mk_phalarx";
                default:
                    return null;
            }
        }

        private static string GetWeaponNameKey(Constants.WeaponType weaponType)
        {
            switch (weaponType)
            {
                case Constants.WeaponType.Rifle:
                    return "weapon.name.rifle";
                case Constants.WeaponType.Cabirne:
                    return "weapon.name.cabirne";
                case Constants.WeaponType.Beretta:
                    return "weapon.name.beretta";
                case Constants.WeaponType.MrSulko:
                    return "weapon.name.mrsulko";
                case Constants.WeaponType.LaserGun:
                    return "weapon.name.laser";
                case Constants.WeaponType.TeslaGun:
                    return "weapon.name.tesla";
                case Constants.WeaponType.PiranhaGun:
                    return "weapon.name.piranha";
                default:
                    return null;
            }
        }

        private static string BuildComboDescription(AchievementDefinition definition)
        {
            Constants.MultiKillType comboType = definition.comboType ?? Constants.MultiKillType.DoubleKill;
            string comboLabel = BuildComboLabel(comboType);
            string format = LocalizationManager.Instance.Get("achievement.description.combo_exact", "Get {0} {1}.");
            return string.Format(format, definition.targetCount, comboLabel);
        }

        private static string BuildBirdDescription(AchievementDefinition definition)
        {
            if (definition.duckFilterMode == DuckFilterMode.Exact)
            {
                string duckNameKey = GetDuckNameKey(definition.duckType);
                string duckName = duckNameKey == null
                    ? Constants.GetDuckDisplayName(definition.duckType)
                    : LocalizationManager.Instance.Get(duckNameKey, Constants.GetDuckDisplayName(definition.duckType));
                string format = LocalizationManager.Instance.Get("achievement.description.bird_kill_exact_duck", "Kill {0} {1} ducks.");
                return string.Format(format, definition.targetCount, duckName);
            }

            if (definition.duckFilterMode == DuckFilterMode.EliteOnly)
            {
                string format = LocalizationManager.Instance.Get("achievement.description.bird_kill_elite", "Kill {0} elite ducks.");
                return string.Format(format, definition.targetCount);
            }

            if (definition.weaponType.HasValue)
            {
                string weaponKey = GetWeaponNameKey(definition.weaponType.Value);
                string weaponName = weaponKey == null
                    ? definition.weaponType.Value.ToString()
                    : LocalizationManager.Instance.Get(weaponKey, definition.weaponType.Value.ToString());
                string format = LocalizationManager.Instance.Get("achievement.description.bird_kill_with_weapon", "Kill {0} ducks with {1}.");
                return string.Format(format, definition.targetCount, weaponName);
            }

            string genericFormat = LocalizationManager.Instance.Get("achievement.description.bird_kill", "Kill {0} ducks.");
            return string.Format(genericFormat, definition.targetCount);
        }

        private void EnsureSchemaVersion()
        {
            int storedVersion = PlayerPrefs.GetInt(SchemaVersionKey, 0);
            if (storedVersion >= CurrentSchemaVersion)
                return;

            ResetAchievementState();
            PlayerPrefs.SetInt(SchemaVersionKey, CurrentSchemaVersion);
            PlayerPrefs.Save();
            Debug.Log($"[Achievement] Schema upgraded {storedVersion} -> {CurrentSchemaVersion}. Achievement progress reset.");
        }

        private void ResetAchievementState()
        {
            foreach (AchievementId id in Enum.GetValues(typeof(AchievementId)))
            {
                PlayerPrefs.DeleteKey(GetProgressKey(id));
                PlayerPrefs.DeleteKey(GetUnlockedKey(id));
            }

            for (int i = 0; i < LegacyAchievementIdsToClear.Length; i++)
            {
                string legacyId = LegacyAchievementIdsToClear[i];
                PlayerPrefs.DeleteKey($"Achievement_{legacyId}_Progress");
                PlayerPrefs.DeleteKey($"Achievement_{legacyId}_Unlocked");
            }

            progress.Clear();
            unlocked.Clear();
        }

        private void LoadState()
        {
            progress.Clear();
            unlocked.Clear();

            foreach (KeyValuePair<AchievementId, AchievementDefinition> entry in definitions)
            {
                AchievementId id = entry.Key;
                int value = PlayerPrefs.GetInt(GetProgressKey(id), 0);
                bool isUnlocked = PlayerPrefs.GetInt(GetUnlockedKey(id), 0) == 1;

                progress[id] = Mathf.Clamp(value, 0, entry.Value.targetCount);
                if (isUnlocked)
                    unlocked.Add(id);
            }
        }

        private void HandleComboKillDetailed(Constants.MultiKillType comboType, Constants.WeaponType weaponType, int bonusPoints, Vector3 position)
        {
            if (!IsEligibleCampaignProgress())
                return;

            foreach (KeyValuePair<AchievementId, AchievementDefinition> entry in definitions)
            {
                AchievementDefinition definition = entry.Value;
                if (definition.progressSource != ProgressSource.ComboKill)
                    continue;

                if (!definition.comboType.HasValue || definition.comboType.Value != comboType)
                    continue;

                IncrementProgress(definition.id, 1);
            }
        }

        private void HandleBirdKilled(Constants.DuckType duckType, Constants.WeaponType weaponType)
        {
            if (!IsEligibleCampaignProgress())
                return;

            foreach (KeyValuePair<AchievementId, AchievementDefinition> entry in definitions)
            {
                AchievementDefinition definition = entry.Value;
                if (definition.progressSource != ProgressSource.BirdKill)
                    continue;

                if (definition.duckFilterMode == DuckFilterMode.Exact && definition.duckType != duckType)
                    continue;

                if (definition.weaponType.HasValue && definition.weaponType.Value != weaponType)
                    continue;

                if (definition.duckFilterMode == DuckFilterMode.EliteOnly && !IsEliteDuck(duckType))
                    continue;

                IncrementProgress(definition.id, 1);
            }
        }

        private static bool IsEliteDuck(Constants.DuckType duckType)
        {
            return duckType == Constants.DuckType.MK_PHALARX ||
                   duckType == Constants.DuckType.MK_ARCHER ||
                   duckType == Constants.DuckType.MK_VOJVODA ||
                   duckType == Constants.DuckType.FRENCH_REVOLUTIONARY;
        }

        private static bool IsEligibleCampaignProgress()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentGameMode != Constants.GameMode.Campaign)
                return false;

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;
            return stage != null && stage.stageIndex >= 3;
        }

        private void IncrementProgress(AchievementId id, int amount)
        {
            if (!definitions.TryGetValue(id, out AchievementDefinition definition))
                return;

            if (unlocked.Contains(id))
                return;

            int current = GetProgress(id);
            int next = Mathf.Min(definition.targetCount, current + Mathf.Max(0, amount));
            if (next == current)
                return;

            progress[id] = next;
            PlayerPrefs.SetInt(GetProgressKey(id), next);
            OnAchievementProgressChanged?.Invoke(id);

            if (next >= definition.targetCount)
            {
                unlocked.Add(id);
                PlayerPrefs.SetInt(GetUnlockedKey(id), 1);
                if (definition.coinReward > 0)
                    GameManager.Instance.AddCoins(definition.coinReward);
                OnAchievementUnlocked?.Invoke(id);
                Debug.Log($"[Achievement] Unlocked: {GetTitle(id)} ({id}) reward: {definition.coinReward} coins");
            }

            PlayerPrefs.Save();
        }

        private static string GetProgressKey(AchievementId id)
        {
            return $"Achievement_{id}_Progress";
        }

        private static string GetUnlockedKey(AchievementId id)
        {
            return $"Achievement_{id}_Unlocked";
        }
    }
}
