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
            public string title;
            public string description;
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
            return definitions.TryGetValue(id, out AchievementDefinition definition) ? definition.title : id.ToString();
        }

        public string GetDescription(AchievementId id)
        {
            return definitions.TryGetValue(id, out AchievementDefinition definition) ? definition.description : string.Empty;
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
                "Predator School",
                "Get 50 Double Kills using Piranha Gun.",
                50,
                Constants.MultiKillType.DoubleKill,
                Constants.WeaponType.PiranhaGun,
                coinReward: 12);

            AddComboAchievement(
                AchievementId.PiranhaDoubleTrouble,
                "Piranha Double Trouble",
                "Get 50 Double Kills with Piranha Gun.",
                50,
                Constants.MultiKillType.DoubleKill,
                Constants.WeaponType.PiranhaGun,
                coinReward: 7);

            AddComboAchievement(
                AchievementId.PiranhaMassacre,
                "Piranha Massacre",
                "Get 10 Quadra Kills with Piranha Gun.",
                10,
                Constants.MultiKillType.QuadraKill,
                Constants.WeaponType.PiranhaGun,
                coinReward: 20);

            AddComboAchievement(
                AchievementId.TeslaChainLord,
                "Tesla Chain Lord",
                "Get 30 Triple+ Kills with Tesla Gun.",
                30,
                null,
                Constants.WeaponType.TeslaGun,
                Constants.MultiKillType.TripleKill,
                coinReward: 30);

            AddComboAchievement(
                AchievementId.BerettaSpray,
                "Beretta Spray",
                "Get 100 Double Kills with Beretta.",
                100,
                Constants.MultiKillType.DoubleKill,
                Constants.WeaponType.Beretta,
                coinReward: 10);

            AddComboAchievement(
                AchievementId.BirdBlenderI,
                "Bird Blender I",
                "Get 100 Double Kills.",
                100,
                Constants.MultiKillType.DoubleKill,
                coinReward: 3);

            AddComboAchievement(
                AchievementId.BirdBlenderII,
                "Bird Blender II",
                "Get 250 Double Kills.",
                250,
                Constants.MultiKillType.DoubleKill,
                coinReward: 15);

            AddComboAchievement(
                AchievementId.TripleThreatI,
                "Triple Threat I",
                "Get 50 Triple Kills.",
                50,
                Constants.MultiKillType.TripleKill,
                coinReward: 5);

            AddComboAchievement(
                AchievementId.TripleThreatII,
                "Triple Threat II",
                "Get 150 Triple Kills.",
                150,
                Constants.MultiKillType.TripleKill,
                coinReward: 12);

            AddComboAchievement(
                AchievementId.OverkillI,
                "Overkill I",
                "Get 25 Quadra Kills.",
                25,
                Constants.MultiKillType.QuadraKill,
                coinReward: 5);

            AddComboAchievement(
                AchievementId.OverkillII,
                "Overkill II",
                "Get 75 Quadra Kills.",
                75,
                Constants.MultiKillType.QuadraKill,
                coinReward: 20);

            AddBirdAchievement(
                AchievementId.DuckHunterI,
                "Duck Hunter I",
                "Kill 10 ducks.",
                10,
                coinReward: 3);
            AddBirdAchievement(
                AchievementId.DuckHunterII,
                "Duck Hunter II",
                "Kill 100 ducks.",
                100,
                coinReward: 5);
            AddBirdAchievement(
                AchievementId.DuckHunterIII,
                "Duck Hunter III",
                "Kill 1000 ducks.",
                1000,
                coinReward: 10);
            AddBirdAchievement(
                AchievementId.DuckHunterIV,
                "Duck Hunter IV",
                "Kill 5000 ducks.",
                5000,
                coinReward: 50);

            AddBirdAchievement(
                AchievementId.SniperPrecision,
                "Sniper Precision",
                "Kill 200 ducks with Cabirne.",
                200,
                weaponType: Constants.WeaponType.Cabirne,
                coinReward: 7);
            AddBirdAchievement(
                AchievementId.LaserSweep,
                "Laser Sweep",
                "Kill 500 ducks with Laser Gun.",
                500,
                weaponType: Constants.WeaponType.LaserGun,
                coinReward: 10);
            AddBirdAchievement(
                AchievementId.SulkoRampage,
                "Sulko Rampage",
                "Kill 300 ducks with MrSulko.",
                300,
                weaponType: Constants.WeaponType.MrSulko,
                coinReward: 12);
            AddBirdAchievement(
                AchievementId.RifleVeteran,
                "Rifle Veteran",
                "Kill 1000 ducks with Rifle.",
                1000,
                weaponType: Constants.WeaponType.Rifle,
                coinReward: 15);

            AddBirdAchievement(
                AchievementId.BossSlayer,
                "Boss Slayer",
                $"Kill 50 {Constants.GetDuckDisplayName(Constants.DuckType.MK_VOJVODA)} ducks.",
                50,
                duckType: Constants.DuckType.MK_VOJVODA,
                coinReward: 7);
            AddBirdAchievement(
                AchievementId.CommanderDown,
                "Commander Down",
                $"Kill 150 {Constants.GetDuckDisplayName(Constants.DuckType.MK_VOJVODA)} ducks.",
                150,
                duckType: Constants.DuckType.MK_VOJVODA,
                coinReward: 20);
            AddBirdAchievement(
                AchievementId.ArcherCleanup,
                "Archer Cleanup",
                $"Kill 200 {Constants.GetDuckDisplayName(Constants.DuckType.MK_ARCHER)} ducks.",
                200,
                duckType: Constants.DuckType.MK_ARCHER,
                coinReward: 10);
            AddBirdAchievement(
                AchievementId.PhalarxBreaker,
                "Phalarx Breaker",
                $"Kill 200 {Constants.GetDuckDisplayName(Constants.DuckType.MK_PHALARX)} ducks.",
                200,
                duckType: Constants.DuckType.MK_PHALARX,
                coinReward: 30);

            AddBirdAchievement(
                AchievementId.EliteControlI,
                "Elite Control I",
                "Kill 100 elite ducks.",
                100,
                duckFilterMode: DuckFilterMode.EliteOnly,
                coinReward: 15);
            AddBirdAchievement(
                AchievementId.EliteControlII,
                "Elite Control II",
                "Kill 300 elite ducks.",
                300,
                duckFilterMode: DuckFilterMode.EliteOnly,
                coinReward: 50);
        }

        private void AddBirdAchievement(
            AchievementId id,
            string title,
            string description,
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
                title = title,
                description = description
            };
        }

        private void AddComboAchievement(
            AchievementId id,
            string title,
            string description,
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
                title = title,
                description = description
            };
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
                Debug.Log($"[Achievement] Unlocked: {definition.title} ({id}) reward: {definition.coinReward} coins");
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
