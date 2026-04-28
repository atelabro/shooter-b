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
            BossSlayerI,
            BossSlayerII,
            BossSlayerIII,
            SkopjeDefenderI,
            SkopjeDefenderII,
            ParisianHunterI,
            ParisianHunterII,
            LondonPatrolI,
            LondonPatrolII,
            NewYorkCleanupI,
            NewYorkCleanupII,
            LosAngelesSweepI,
            LosAngelesSweepII,
            TokyoOperationI,
            TokyoOperationII,
            CairoExpeditionI,
            CairoExpeditionII,
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
            OverkillII,
            SkopjeBossHunter,
            ParisBossHunter,
            LondonBossHunter,
            NewYorkBossHunter,
            LosAngelesBossHunter,
            TokyoBossHunter,
            CairoBossHunter
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
            EliteOnly,
            Group
        }

        private enum DuckGroup
        {
            None,
            Skopje,
            Paris,
            London,
            NewYork,
            LosAngeles,
            Tokyo,
            Cairo
        }

        private struct AchievementDefinition
        {
            public AchievementId id;
            public ProgressSource progressSource;
            public Constants.MultiKillType? comboType;
            public DuckFilterMode duckFilterMode;
            public DuckGroup duckGroup;
            public DuckGroup requiredCampaignGroup;
            public Constants.DuckType duckType;
            public Constants.WeaponType? weaponType;
            public int targetCount;
            public int coinReward;
            public string titleKey;
        }

        private const int CurrentSchemaVersion = 6;
        private const string SchemaVersionKey = "Achievement_SchemaVersion";
        private const string AchievementCoinSfxResourcePath = "Audio/coin";

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
            "BerettaSpray",
            "EliteControlIII",
            "EliteControlIV",
            "ArcherCleanupI",
            "ArcherCleanupII",
            "PhalarxBreakerI",
            "PhalarxBreakerII"
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
        private GameManager cachedGameManager;
        private AudioSource sfxSource;
        private AudioClip achievementCoinSfx;
        private bool hasLoggedMissingCoinSfx;

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
            AudioSettingsManager.Instance.OnAudioSettingsChanged += HandleAudioSettingsChanged;
        }

        private void OnDestroy()
        {
            if (cachedGameManager != null)
            {
                cachedGameManager.OnComboKillDetailed -= HandleComboKillDetailed;
                cachedGameManager.OnBirdKilled -= HandleBirdKilled;
            }

            if (AudioSettingsManager.HasInstance)
                AudioSettingsManager.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
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
            cachedGameManager = GameManager.Instance;
            if (cachedGameManager == null)
                return;

            cachedGameManager.OnComboKillDetailed -= HandleComboKillDetailed;
            cachedGameManager.OnComboKillDetailed += HandleComboKillDetailed;
            cachedGameManager.OnBirdKilled -= HandleBirdKilled;
            cachedGameManager.OnBirdKilled += HandleBirdKilled;
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

            AddBirdAchievement(AchievementId.BossSlayerI, 60, duckFilterMode: DuckFilterMode.EliteOnly, coinReward: 15);
            AddBirdAchievement(AchievementId.BossSlayerII, 180, duckFilterMode: DuckFilterMode.EliteOnly, coinReward: 40);
            AddBirdAchievement(AchievementId.BossSlayerIII, 360, duckFilterMode: DuckFilterMode.EliteOnly, coinReward: 80);
            AddBirdAchievement(AchievementId.SkopjeBossHunter, 10, duckType: Constants.DuckType.MK_SAMUIL_BOSS_DUCK, requiredCampaignGroup: DuckGroup.Skopje, coinReward: 18);
            AddBirdAchievement(AchievementId.ParisBossHunter, 10, duckType: Constants.DuckType.FRENCH_MUSKETEER_BOSS_DUCK, requiredCampaignGroup: DuckGroup.Paris, coinReward: 18);
            AddBirdAchievement(AchievementId.LondonBossHunter, 10, duckType: Constants.DuckType.BRITISH_SHERLOCK_BOSS_DUCK, requiredCampaignGroup: DuckGroup.London, coinReward: 18);
            AddBirdAchievement(AchievementId.NewYorkBossHunter, 10, duckType: Constants.DuckType.USA_BOSS_DUCK, requiredCampaignGroup: DuckGroup.NewYork, coinReward: 18);
            AddBirdAchievement(AchievementId.LosAngelesBossHunter, 10, duckType: Constants.DuckType.USA_ADMIRAL_BOSS_DUCK, requiredCampaignGroup: DuckGroup.LosAngeles, coinReward: 18);
            AddBirdAchievement(AchievementId.TokyoBossHunter, 10, duckType: Constants.DuckType.JAPANESE_SAMURAI_BOSS_DUCK, requiredCampaignGroup: DuckGroup.Tokyo, coinReward: 18);
            AddBirdAchievement(AchievementId.CairoBossHunter, 10, duckType: Constants.DuckType.EGYPT_SCARAB_BOSS_DUCK, requiredCampaignGroup: DuckGroup.Cairo, coinReward: 18);

            AddCityAchievement(AchievementId.SkopjeDefenderI, 100, DuckGroup.Skopje, coinReward: 18);
            AddCityAchievement(AchievementId.SkopjeDefenderII, 350, DuckGroup.Skopje, coinReward: 50);
            AddCityAchievement(AchievementId.ParisianHunterI, 100, DuckGroup.Paris, coinReward: 18);
            AddCityAchievement(AchievementId.ParisianHunterII, 350, DuckGroup.Paris, coinReward: 50);
            AddCityAchievement(AchievementId.LondonPatrolI, 100, DuckGroup.London, coinReward: 18);
            AddCityAchievement(AchievementId.LondonPatrolII, 350, DuckGroup.London, coinReward: 50);
            AddCityAchievement(AchievementId.NewYorkCleanupI, 100, DuckGroup.NewYork, coinReward: 18);
            AddCityAchievement(AchievementId.NewYorkCleanupII, 350, DuckGroup.NewYork, coinReward: 50);
            AddCityAchievement(AchievementId.LosAngelesSweepI, 100, DuckGroup.LosAngeles, coinReward: 18);
            AddCityAchievement(AchievementId.LosAngelesSweepII, 350, DuckGroup.LosAngeles, coinReward: 50);
            AddCityAchievement(AchievementId.TokyoOperationI, 100, DuckGroup.Tokyo, coinReward: 18);
            AddCityAchievement(AchievementId.TokyoOperationII, 350, DuckGroup.Tokyo, coinReward: 50);
            AddCityAchievement(AchievementId.CairoExpeditionI, 100, DuckGroup.Cairo, coinReward: 18);
            AddCityAchievement(AchievementId.CairoExpeditionII, 350, DuckGroup.Cairo, coinReward: 50);

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
            DuckGroup requiredCampaignGroup = DuckGroup.None,
            int coinReward = 0)
        {
            definitions[id] = new AchievementDefinition
            {
                id = id,
                progressSource = ProgressSource.BirdKill,
                comboType = null,
                duckFilterMode = duckType.HasValue ? DuckFilterMode.Exact : duckFilterMode,
                requiredCampaignGroup = requiredCampaignGroup,
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
                requiredCampaignGroup = DuckGroup.None,
                duckType = default,
                weaponType = null,
                targetCount = targetCount,
                coinReward = Mathf.Max(0, coinReward),
                titleKey = GetTitleKey(id)
            };
        }

        private void AddCityAchievement(AchievementId id, int targetCount, DuckGroup group, int coinReward = 0)
        {
            definitions[id] = new AchievementDefinition
            {
                id = id,
                progressSource = ProgressSource.BirdKill,
                comboType = null,
                duckFilterMode = DuckFilterMode.Group,
                duckGroup = group,
                requiredCampaignGroup = group,
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

        private static DuckGroup GetDuckGroup(Constants.DuckType duckType)
        {
            switch (duckType)
            {
                case Constants.DuckType.MK_PHALARX:
                case Constants.DuckType.MK_ARCHER:
                case Constants.DuckType.MK_VOJVODA:
                case Constants.DuckType.MK_SAMUIL_BOSS_DUCK:
                    return DuckGroup.Skopje;
                case Constants.DuckType.FRENCH_REVOLUTIONARY:
                case Constants.DuckType.FRENCH_NAPOLEON:
                case Constants.DuckType.FRENCH_ARTIST:
                case Constants.DuckType.FRENCH_MUSKETEER:
                case Constants.DuckType.FRENCH_MUSKETEER_BOSS_DUCK:
                    return DuckGroup.Paris;
                case Constants.DuckType.BRITISH_REDCOAT:
                case Constants.DuckType.BRITISH_POLICE:
                case Constants.DuckType.BRITISH_PUNK:
                case Constants.DuckType.BRITISH_SHERLOCK_BOSS_DUCK:
                    return DuckGroup.London;
                case Constants.DuckType.USA_POLICE:
                case Constants.DuckType.USA_WORKER:
                case Constants.DuckType.USA_BUSINESS:
                case Constants.DuckType.USA_SWAT:
                case Constants.DuckType.USA_ADMIRAL:
                case Constants.DuckType.USA_ADMIRAL_BOSS_DUCK:
                    return DuckGroup.LosAngeles;
                case Constants.DuckType.USA_HOLLYWOOD:
                case Constants.DuckType.USA_LEO:
                case Constants.DuckType.USA_TOM:
                case Constants.DuckType.USA_MARINE:
                case Constants.DuckType.USA_BOSS_DUCK:
                    return DuckGroup.NewYork;
                case Constants.DuckType.JAPANESE_SAMURAI:
                case Constants.DuckType.JAPANESE_SAMURAI_BOSS_DUCK:
                case Constants.DuckType.JAPANESE_STRAW_DUCK:
                case Constants.DuckType.JAPANESE_KIMONO_DUCK:
                case Constants.DuckType.JAPANESE_YAKUZA_BOSS_DUCK:
                    return DuckGroup.Tokyo;
                case Constants.DuckType.EGYPT_MUMMY:
                case Constants.DuckType.EGYPT_PHARAOH:
                case Constants.DuckType.EGYPT_ANUBIS:
                case Constants.DuckType.EGYPT_RAIDER:
                case Constants.DuckType.EGYPT_SCARAB:
                case Constants.DuckType.EGYPT_SCARAB_BOSS_DUCK:
                    return DuckGroup.Cairo;
                default:
                    return DuckGroup.None;
            }
        }

        private static DuckGroup GetActiveCampaignGroup()
        {
            CityConfig activeCity = CampaignProgressManager.Instance.ActiveCityConfig;
            if (activeCity == null || string.IsNullOrWhiteSpace(activeCity.cityName))
                return DuckGroup.None;

            switch (activeCity.cityName.Trim())
            {
                case "Skopje":
                    return DuckGroup.Skopje;
                case "Paris":
                    return DuckGroup.Paris;
                case "London":
                    return DuckGroup.London;
                case "New York":
                    return DuckGroup.NewYork;
                case "Los Angeles":
                    return DuckGroup.LosAngeles;
                case "Tokyo":
                    return DuckGroup.Tokyo;
                case "Cairo":
                    return DuckGroup.Cairo;
                default:
                    return DuckGroup.None;
            }
        }

        private static bool IsUsaDuck(Constants.DuckType duckType)
        {
            return duckType == Constants.DuckType.USA_POLICE ||
                   duckType == Constants.DuckType.USA_WORKER ||
                   duckType == Constants.DuckType.USA_BUSINESS ||
                   duckType == Constants.DuckType.USA_SWAT ||
                   duckType == Constants.DuckType.USA_ADMIRAL ||
                   duckType == Constants.DuckType.USA_ADMIRAL_BOSS_DUCK ||
                   duckType == Constants.DuckType.USA_HOLLYWOOD ||
                   duckType == Constants.DuckType.USA_LEO ||
                   duckType == Constants.DuckType.USA_TOM ||
                   duckType == Constants.DuckType.USA_MARINE ||
                   duckType == Constants.DuckType.USA_BOSS_DUCK;
        }

        private static bool DoesDuckCountForGroup(Constants.DuckType duckType, DuckGroup group)
        {
            if (group == DuckGroup.NewYork || group == DuckGroup.LosAngeles)
            {
                if (!IsUsaDuck(duckType))
                    return false;

                DuckGroup activeGroup = GetActiveCampaignGroup();
                if (activeGroup == DuckGroup.NewYork || activeGroup == DuckGroup.LosAngeles)
                    return activeGroup == group;
            }

            return GetDuckGroup(duckType) == group;
        }

        private static string GetCityGroupNameKey(DuckGroup group)
        {
            switch (group)
            {
                case DuckGroup.Skopje:  return "achievement.city_group.skopje";
                case DuckGroup.Paris:   return "achievement.city_group.paris";
                case DuckGroup.London:  return "achievement.city_group.london";
                case DuckGroup.NewYork: return "achievement.city_group.newyork";
                case DuckGroup.LosAngeles: return "achievement.city_group.losangeles";
                case DuckGroup.Tokyo:   return "achievement.city_group.tokyo";
                case DuckGroup.Cairo:   return "achievement.city_group.cairo";
                default:                return null;
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
                string format = IsBossAchievement(definition)
                    ? LocalizationManager.Instance.Get("achievement.description.bird_kill_boss", "Kill {0} boss ducks.")
                    : LocalizationManager.Instance.Get("achievement.description.bird_kill_elite", "Kill {0} elite ducks.");
                return string.Format(format, definition.targetCount);
            }

            if (definition.duckFilterMode == DuckFilterMode.Group)
            {
                string cityKey = GetCityGroupNameKey(definition.duckGroup);
                string cityName = cityKey != null
                    ? LocalizationManager.Instance.Get(cityKey, definition.duckGroup.ToString())
                    : definition.duckGroup.ToString();
                string format = LocalizationManager.Instance.Get(
                    "achievement.description.bird_kill_city_group", "Kill {0} {1} ducks.");
                return string.Format(format, definition.targetCount, cityName);
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
            GameLog.Log($"[Achievement] Schema upgraded {storedVersion} -> {CurrentSchemaVersion}. Achievement progress reset.");
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

                if (definition.requiredCampaignGroup != DuckGroup.None &&
                    GetActiveCampaignGroup() != definition.requiredCampaignGroup)
                    continue;

                if (definition.duckFilterMode == DuckFilterMode.Exact && definition.duckType != duckType)
                    continue;

                if (definition.weaponType.HasValue && definition.weaponType.Value != weaponType)
                    continue;

                if (definition.duckFilterMode == DuckFilterMode.EliteOnly && !IsEligibleSpecialDuck(definition, duckType))
                    continue;

                if (definition.duckFilterMode == DuckFilterMode.Group &&
                    !DoesDuckCountForGroup(duckType, definition.duckGroup))
                    continue;

                IncrementProgress(definition.id, 1);
            }
        }

        private static bool IsBossAchievement(AchievementDefinition definition)
        {
            return definition.id == AchievementId.BossSlayerI ||
                   definition.id == AchievementId.BossSlayerII ||
                   definition.id == AchievementId.BossSlayerIII;
        }

        private static bool IsEligibleSpecialDuck(AchievementDefinition definition, Constants.DuckType duckType)
        {
            return IsBossAchievement(definition)
                ? Constants.IsBossDuckType(duckType)
                : Constants.IsEliteDuckType(duckType);
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
                PlayAchievementUnlockSfx();
                OnAchievementUnlocked?.Invoke(id);
                GameLog.Log($"[Achievement] Unlocked: {GetTitle(id)} ({id}) reward: {definition.coinReward} coins");
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

        private void PlayAchievementUnlockSfx()
        {
            EnsureAchievementSfxLoaded();
            if (achievementCoinSfx == null || sfxSource == null)
                return;

            sfxSource.PlayOneShot(achievementCoinSfx);
        }

        private void EnsureAchievementSfxLoaded()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.GetComponent<AudioSource>();
                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();

                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
            }

            if (achievementCoinSfx != null)
                return;

            achievementCoinSfx = Resources.Load<AudioClip>(AchievementCoinSfxResourcePath);
            if (achievementCoinSfx == null && !hasLoggedMissingCoinSfx)
            {
                hasLoggedMissingCoinSfx = true;
                GameLog.Warning($"[AchievementManager] Missing achievement SFX clip at Resources/{AchievementCoinSfxResourcePath}.");
            }
        }

        private void HandleAudioSettingsChanged()
        {
            if (sfxSource == null)
                return;

            sfxSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
        }
    }
}
