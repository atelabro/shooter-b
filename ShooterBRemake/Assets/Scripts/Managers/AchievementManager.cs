using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public class AchievementManager : MonoBehaviour
    {
        public enum AchievementId
        {
            BirdBlenderI,
            BirdBlenderII,
            BossSlayer,
            CommanderDown,
            DuckHunterI,
            DuckHunterII,
            DuckHunterIII,
            DuckHunterIV,
            EliteControlI,
            EliteControlII,
            ArcherCleanup,
            LaserSweep,
            OverkillI,
            OverkillII,
            PhalarxBreaker,
            DuckHunter10,
            PiranhaDoubleTrouble,
            PiranhaMassacre,
            PiranhaDoubleKill50,
            RifleVeteran,
            SniperPrecision,
            SulkoRampage,
            TeslaChainLord,
            TripleThreatI,
            TripleThreatII,
            BerettaSpray
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
            public Constants.MultiKillType? minimumComboType;
            public DuckFilterMode duckFilterMode;
            public Constants.DuckType duckType;
            public Constants.WeaponType? weaponType;
            public int targetCount;
            public int coinReward;
            public string titleKey;
        }

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
            LoadState();
            SubscribeToGameManager();
        }

        private void OnDestroy()
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnComboKillDetailed -= HandleComboKillDetailed;
                gameManager.OnBirdKilled -= HandleBirdKilled;
            }
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

            if (definition.progressSource == ProgressSource.ComboKill)
                return BuildComboDescription(definition);

            return BuildBirdDescription(definition);
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
            // Legacy id kept for save compatibility.
            AddComboAchievement(
                AchievementId.PiranhaDoubleKill50,
                50,
                Constants.MultiKillType.DoubleKill,
                Constants.WeaponType.PiranhaGun,
                coinReward: 12);

            AddComboAchievement(
                AchievementId.PiranhaDoubleTrouble,
                50,
                Constants.MultiKillType.DoubleKill,
                Constants.WeaponType.PiranhaGun,
                coinReward: 7);

            AddComboAchievement(
                AchievementId.PiranhaMassacre,
                10,
                Constants.MultiKillType.QuadraKill,
                Constants.WeaponType.PiranhaGun,
                coinReward: 20);

            AddComboAchievement(
                AchievementId.TeslaChainLord,
                30,
                null,
                Constants.WeaponType.TeslaGun,
                Constants.MultiKillType.TripleKill,
                coinReward: 30);

            AddComboAchievement(
                AchievementId.BerettaSpray,
                100,
                Constants.MultiKillType.DoubleKill,
                Constants.WeaponType.Beretta,
                coinReward: 10);

            AddComboAchievement(
                AchievementId.BirdBlenderI,
                100,
                Constants.MultiKillType.DoubleKill,
                coinReward: 3);

            AddComboAchievement(
                AchievementId.BirdBlenderII,
                250,
                Constants.MultiKillType.DoubleKill,
                coinReward: 15);

            AddComboAchievement(
                AchievementId.TripleThreatI,
                50,
                Constants.MultiKillType.TripleKill,
                coinReward: 5);

            AddComboAchievement(
                AchievementId.TripleThreatII,
                150,
                Constants.MultiKillType.TripleKill,
                coinReward: 12);

            AddComboAchievement(
                AchievementId.OverkillI,
                25,
                Constants.MultiKillType.QuadraKill,
                coinReward: 5);

            AddComboAchievement(
                AchievementId.OverkillII,
                75,
                Constants.MultiKillType.QuadraKill,
                coinReward: 20);

            AddBirdAchievement(
                AchievementId.DuckHunterI,
                10,
                coinReward: 3);
            AddBirdAchievement(
                AchievementId.DuckHunterII,
                100,
                coinReward: 5);
            AddBirdAchievement(
                AchievementId.DuckHunterIII,
                1000,
                coinReward: 10);
            AddBirdAchievement(
                AchievementId.DuckHunterIV,
                5000,
                coinReward: 50);

            AddBirdAchievement(
                AchievementId.SniperPrecision,
                200,
                weaponType: Constants.WeaponType.Cabirne,
                coinReward: 7);
            AddBirdAchievement(
                AchievementId.LaserSweep,
                500,
                weaponType: Constants.WeaponType.LaserGun,
                coinReward: 10);
            AddBirdAchievement(
                AchievementId.SulkoRampage,
                300,
                weaponType: Constants.WeaponType.MrSulko,
                coinReward: 12);
            AddBirdAchievement(
                AchievementId.RifleVeteran,
                1000,
                weaponType: Constants.WeaponType.Rifle,
                coinReward: 15);

            AddBirdAchievement(
                AchievementId.BossSlayer,
                50,
                duckType: Constants.DuckType.MK_VOJVODA,
                coinReward: 7);
            AddBirdAchievement(
                AchievementId.CommanderDown,
                150,
                duckType: Constants.DuckType.MK_VOJVODA,
                coinReward: 20);
            AddBirdAchievement(
                AchievementId.ArcherCleanup,
                200,
                duckType: Constants.DuckType.MK_ARCHER,
                coinReward: 10);
            AddBirdAchievement(
                AchievementId.PhalarxBreaker,
                200,
                duckType: Constants.DuckType.MK_PHALARX,
                coinReward: 30);

            AddBirdAchievement(
                AchievementId.EliteControlI,
                100,
                duckFilterMode: DuckFilterMode.EliteOnly,
                coinReward: 15);
            AddBirdAchievement(
                AchievementId.EliteControlII,
                300,
                duckFilterMode: DuckFilterMode.EliteOnly,
                coinReward: 50);
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
                minimumComboType = null,
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
            Constants.MultiKillType? comboType,
            Constants.WeaponType? weaponType = null,
            Constants.MultiKillType? minimumComboType = null,
            int coinReward = 0)
        {
            definitions[id] = new AchievementDefinition
            {
                id = id,
                progressSource = ProgressSource.ComboKill,
                comboType = comboType,
                minimumComboType = minimumComboType,
                duckFilterMode = DuckFilterMode.Any,
                duckType = default,
                weaponType = weaponType,
                targetCount = targetCount,
                coinReward = Mathf.Max(0, coinReward),
                titleKey = GetTitleKey(id)
            };
        }

        private static string GetTitleKey(AchievementId id)
        {
            return $"achievement.{id}.title";
        }

        private static string BuildComboLabel(Constants.MultiKillType comboType, bool isMinimum)
        {
            if (isMinimum && comboType == Constants.MultiKillType.TripleKill)
                return LocalizationManager.Instance.Get("achievement.combo_label.triple_or_better", "Triple+ Kills");

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
            Constants.MultiKillType comboType = definition.comboType ?? definition.minimumComboType ?? Constants.MultiKillType.DoubleKill;
            bool isMinimum = definition.minimumComboType.HasValue && !definition.comboType.HasValue;
            string comboLabel = BuildComboLabel(comboType, isMinimum);

            if (definition.weaponType.HasValue)
            {
                string weaponKey = GetWeaponNameKey(definition.weaponType.Value);
                string weaponName = weaponKey == null
                    ? definition.weaponType.Value.ToString()
                    : LocalizationManager.Instance.Get(weaponKey, definition.weaponType.Value.ToString());

                string formatWithWeapon = isMinimum
                    ? LocalizationManager.Instance.Get("achievement.description.combo_min_with_weapon", "Get {0} {1} with {2}.")
                    : LocalizationManager.Instance.Get("achievement.description.combo_exact_with_weapon", "Get {0} {1} with {2}.");
                return string.Format(formatWithWeapon, definition.targetCount, comboLabel, weaponName);
            }

            string format = isMinimum
                ? LocalizationManager.Instance.Get("achievement.description.combo_min", "Get {0} {1}.")
                : LocalizationManager.Instance.Get("achievement.description.combo_exact", "Get {0} {1}.");
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

        private void LoadState()
        {
            progress.Clear();
            unlocked.Clear();

            foreach (KeyValuePair<AchievementId, AchievementDefinition> entry in definitions)
            {
                AchievementId id = entry.Key;
                int value = PlayerPrefs.GetInt(GetProgressKey(id), 0);
                bool isUnlocked = PlayerPrefs.GetInt(GetUnlockedKey(id), 0) == 1;

                progress[id] = Mathf.Max(0, value);
                if (isUnlocked)
                    unlocked.Add(id);
            }

            MigrateLegacyAchievement(AchievementId.DuckHunter10, AchievementId.DuckHunterI);
        }

        private void MigrateLegacyAchievement(AchievementId oldId, AchievementId newId)
        {
            if (!definitions.ContainsKey(newId))
                return;

            int oldProgress = PlayerPrefs.GetInt(GetProgressKey(oldId), 0);
            bool oldUnlocked = PlayerPrefs.GetInt(GetUnlockedKey(oldId), 0) == 1;

            if (oldProgress <= 0 && !oldUnlocked)
                return;

            int target = GetTarget(newId);
            int mergedProgress = Mathf.Clamp(Mathf.Max(GetProgress(newId), oldProgress), 0, target);
            progress[newId] = mergedProgress;
            PlayerPrefs.SetInt(GetProgressKey(newId), mergedProgress);

            if (oldUnlocked || mergedProgress >= target)
            {
                unlocked.Add(newId);
                PlayerPrefs.SetInt(GetUnlockedKey(newId), 1);
            }

            PlayerPrefs.Save();
        }

        private void HandleComboKillDetailed(Constants.MultiKillType comboType, Constants.WeaponType weaponType, int bonusPoints, Vector3 position)
        {
            foreach (KeyValuePair<AchievementId, AchievementDefinition> entry in definitions)
            {
                AchievementDefinition definition = entry.Value;
                if (definition.progressSource != ProgressSource.ComboKill)
                    continue;

                if (definition.comboType.HasValue && definition.comboType.Value != comboType)
                    continue;

                if (definition.minimumComboType.HasValue &&
                    GetComboRank(comboType) < GetComboRank(definition.minimumComboType.Value))
                    continue;

                if (definition.weaponType.HasValue && definition.weaponType.Value != weaponType)
                    continue;

                IncrementProgress(definition.id, 1);
            }
        }

        private void HandleBirdKilled(Constants.DuckType duckType, Constants.WeaponType weaponType)
        {
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

        private static int GetComboRank(Constants.MultiKillType comboType)
        {
            switch (comboType)
            {
                case Constants.MultiKillType.DoubleKill:
                    return 2;
                case Constants.MultiKillType.TripleKill:
                    return 3;
                default:
                    return 4;
            }
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
