using UnityEngine;

namespace ShooterB
{
    public static class Constants
    {
        public const float CAMERA_WIDTH = 1920f;
        public const float CAMERA_HEIGHT = 1080f;

        public const int INITIAL_LIVES = 3;
        public const int MAX_LIVES = 3;
        public const int BONUS_LIFE_BIRD_COUNT = 50;

        public const int MIN_DIFFICULTY = 1;
        public const int MAX_DIFFICULTY = 35;
        public const int INITIAL_DIFFICULTY = 1;

        public const float HEADER_HEIGHT = 200f;
        public const float FOOTER_HEIGHT = 200f;

        public const int SORTING_LAYER_DEAD_DUCKS = 0;
        public const int SORTING_LAYER_BACKGROUND = 1;
        public const int SORTING_LAYER_DUCKS = 10;
        public const int SORTING_LAYER_BULLETS = 15;
        public const int SORTING_LAYER_UI = 20;

        public const string PREFS_HIGH_SCORE_ARCADE = "HighScore_Arcade";
        public const string PREFS_SELECTED_WEAPON = "SelectedWeapon";
        public const string PREFS_UNLOCKED_WEAPONS = "UnlockedWeapons";
        public const string PREFS_COINS = "Coins";
        public const string PREFS_LANGUAGE = "Language";
        public const string PREFS_MASTER_VOLUME = "MasterVolume";
        public const string PREFS_MUSIC_VOLUME = "MusicVolume";
        public const string PREFS_SFX_VOLUME = "SfxVolume";
        public const string PREFS_VIBRATION_ENABLED = "VibrationEnabled";
        public const string FONT_COLOR_HEX = "FFB02A";

        public enum GameMode
        {
            Campaign,
            Arcade
        }

        public enum WeaponType
        {
            Rifle,
            Cabirne,
            Beretta,
            MrSulko,
            LaserGun,
            TeslaGun,
            PiranhaGun
        }

        public enum WeaponFireMode
        {
            SingleTap,
            HoldAutomatic
        }

        public enum DuckType
        {
            Type0,
            Type1,
            Type2,
            Type4 = 4,
            MK_PHALARX,
            MK_ARCHER,
            MK_VOJVODA,
            FRENCH_REVOLUTIONARY,
            FRENCH_NAPOLEON,
            FRENCH_ARTIST,
            BRITISH_REDCOAT,
            BRITISH_POLICE,
            BRITISH_PUNK,
            USA_POLICE,
            USA_WORKER,
            USA_BUSINESS,
            JAPANESE_SAMURAI,
            USA_BOSS_DUCK,
            JAPANESE_STRAW_DUCK,
            JAPANESE_KIMONO_DUCK,
            FRENCH_MUSKETEER,
            MK_SAMUIL_GUARD,
            MK_SAMUIL_ELITE,
            MK_SAMUIL_BOSS_DUCK,
            FRENCH_MUSKETEER_BOSS_DUCK,
            JAPANESE_SAMURAI_BOSS_DUCK,
            BRITISH_SHERLOCK_BOSS_DUCK,
            USA_SWAT = 28,
            USA_ADMIRAL = 29,
            USA_ADMIRAL_BOSS_DUCK = 30,
            USA_HOLLYWOOD = 31,
            USA_LEO = 32,
            USA_TOM = 33,
            USA_MARINE = 34,
            EGYPT_MUMMY = 35,
            EGYPT_PHARAOH = 36,
            EGYPT_ANUBIS = 37,
            EGYPT_RAIDER = 38,
            EGYPT_SCARAB = 39,
            EGYPT_SCARAB_BOSS_DUCK = 40
        }

        public static string GetDuckDisplayName(DuckType type)
        {
            switch (type)
            {
                case DuckType.Type0: return "Duck I";
                case DuckType.Type1: return "Duck II";
                case DuckType.Type2: return "Duck III";
                case DuckType.Type4: return "Duck V";
                case DuckType.MK_PHALARX: return "Macedonian Phalanx";
                case DuckType.MK_ARCHER: return "Macedonian Archer";
                case DuckType.MK_VOJVODA: return "Macedonian Vojvoda";
                case DuckType.FRENCH_REVOLUTIONARY: return "French Revolutionary";
                case DuckType.FRENCH_NAPOLEON: return "French Napoleon";
                case DuckType.FRENCH_ARTIST: return "French Artist";
                case DuckType.FRENCH_MUSKETEER: return "French Musketeer";
                case DuckType.FRENCH_MUSKETEER_BOSS_DUCK: return "French Musketeer Boss Duck";
                case DuckType.BRITISH_REDCOAT: return "British Redcoat";
                case DuckType.BRITISH_POLICE: return "British Police";
                case DuckType.BRITISH_PUNK: return "British Punk";
                case DuckType.BRITISH_SHERLOCK_BOSS_DUCK: return "British Sherlock Boss Duck";
                case DuckType.USA_POLICE: return "USA Police";
                case DuckType.USA_WORKER: return "USA Worker";
                case DuckType.USA_BUSINESS: return "USA Business";
                case DuckType.USA_SWAT: return "USA SWAT";
                case DuckType.USA_ADMIRAL: return "USA Admiral";
                case DuckType.USA_ADMIRAL_BOSS_DUCK: return "USA Admiral Boss Duck";
                case DuckType.USA_HOLLYWOOD: return "USA Hollywood";
                case DuckType.USA_LEO: return "USA Leo";
                case DuckType.USA_TOM: return "USA Tom";
                case DuckType.USA_MARINE: return "USA Marine";
                case DuckType.EGYPT_MUMMY: return "Egypt Mummy";
                case DuckType.EGYPT_PHARAOH: return "Egypt Pharaoh";
                case DuckType.EGYPT_ANUBIS: return "Egypt Anubis";
                case DuckType.EGYPT_RAIDER: return "Egypt Raider";
                case DuckType.EGYPT_SCARAB: return "Egypt Scarab";
                case DuckType.EGYPT_SCARAB_BOSS_DUCK: return "Egypt Scarab Boss Duck";
                case DuckType.JAPANESE_SAMURAI: return "Japanese Samurai";
                case DuckType.JAPANESE_SAMURAI_BOSS_DUCK: return "Japanese Samurai Boss Duck";
                case DuckType.USA_BOSS_DUCK: return "USA Boss Duck";
                case DuckType.JAPANESE_STRAW_DUCK: return "Japanese Straw Duck";
                case DuckType.JAPANESE_KIMONO_DUCK: return "Japanese Kimono Duck";
                case DuckType.MK_SAMUIL_GUARD: return "Samuil Guard";
                case DuckType.MK_SAMUIL_ELITE: return "Samuil Elite";
                case DuckType.MK_SAMUIL_BOSS_DUCK: return "Samuil Boss Duck";
                // Reserve this switch for future region-specific duck additions (e.g. France).
                default: return type.ToString();
            }
        }

        public static string GetDuckDebugName(DuckType type)
        {
            switch (type)
            {
                case DuckType.Type0: return "TYPE_0";
                case DuckType.Type1: return "TYPE_1";
                case DuckType.Type2: return "TYPE_2";
                case DuckType.Type4: return "TYPE_4";
                case DuckType.MK_PHALARX: return "MK_PHALARX";
                case DuckType.MK_ARCHER: return "MK_ARCHER";
                case DuckType.MK_VOJVODA: return "MK_VOJVODA";
                case DuckType.FRENCH_REVOLUTIONARY: return "FRENCH_REVOLUTIONARY";
                case DuckType.FRENCH_NAPOLEON: return "FRENCH_NAPOLEON";
                case DuckType.FRENCH_ARTIST: return "FRENCH_ARTIST";
                case DuckType.FRENCH_MUSKETEER: return "FRENCH_MUSKETEER";
                case DuckType.FRENCH_MUSKETEER_BOSS_DUCK: return "FRENCH_MUSKETEER_BOSS_DUCK";
                case DuckType.BRITISH_REDCOAT: return "BRITISH_REDCOAT";
                case DuckType.BRITISH_POLICE: return "BRITISH_POLICE";
                case DuckType.BRITISH_PUNK: return "BRITISH_PUNK";
                case DuckType.BRITISH_SHERLOCK_BOSS_DUCK: return "BRITISH_SHERLOCK_BOSS_DUCK";
                case DuckType.USA_POLICE: return "USA_POLICE";
                case DuckType.USA_WORKER: return "USA_WORKER";
                case DuckType.USA_BUSINESS: return "USA_BUSINESS";
                case DuckType.USA_SWAT: return "USA_SWAT";
                case DuckType.USA_ADMIRAL: return "USA_ADMIRAL";
                case DuckType.USA_ADMIRAL_BOSS_DUCK: return "USA_ADMIRAL_BOSS_DUCK";
                case DuckType.USA_HOLLYWOOD: return "USA_HOLLYWOOD";
                case DuckType.USA_LEO: return "USA_LEO";
                case DuckType.USA_TOM: return "USA_TOM";
                case DuckType.USA_MARINE: return "USA_MARINE";
                case DuckType.EGYPT_MUMMY: return "EGYPT_MUMMY";
                case DuckType.EGYPT_PHARAOH: return "EGYPT_PHARAOH";
                case DuckType.EGYPT_ANUBIS: return "EGYPT_ANUBIS";
                case DuckType.EGYPT_RAIDER: return "EGYPT_RAIDER";
                case DuckType.EGYPT_SCARAB: return "EGYPT_SCARAB";
                case DuckType.EGYPT_SCARAB_BOSS_DUCK: return "EGYPT_SCARAB_BOSS_DUCK";
                case DuckType.JAPANESE_SAMURAI: return "JAPANESE_SAMURAI";
                case DuckType.JAPANESE_SAMURAI_BOSS_DUCK: return "JAPANESE_SAMURAI_BOSS_DUCK";
                case DuckType.USA_BOSS_DUCK: return "USA_BOSS_DUCK";
                case DuckType.JAPANESE_STRAW_DUCK: return "JAPANESE_STRAW_DUCK";
                case DuckType.JAPANESE_KIMONO_DUCK: return "JAPANESE_KIMONO_DUCK";
                case DuckType.MK_SAMUIL_GUARD: return "MK_SAMUIL_GUARD";
                case DuckType.MK_SAMUIL_ELITE: return "MK_SAMUIL_ELITE";
                case DuckType.MK_SAMUIL_BOSS_DUCK: return "MK_SAMUIL_BOSS_DUCK";
                default: return type.ToString();
            }
        }

        public static bool IsEliteDuckType(DuckType type)
        {
            switch (type)
            {
                case DuckType.MK_VOJVODA:
                case DuckType.MK_SAMUIL_ELITE:
                case DuckType.FRENCH_REVOLUTIONARY:
                case DuckType.FRENCH_MUSKETEER:
                case DuckType.BRITISH_PUNK:
                case DuckType.JAPANESE_SAMURAI:
                case DuckType.EGYPT_ANUBIS:
                case DuckType.EGYPT_SCARAB:
                case DuckType.MK_SAMUIL_BOSS_DUCK:
                case DuckType.USA_BOSS_DUCK:
                case DuckType.USA_ADMIRAL_BOSS_DUCK:
                case DuckType.FRENCH_MUSKETEER_BOSS_DUCK:
                case DuckType.JAPANESE_SAMURAI_BOSS_DUCK:
                case DuckType.BRITISH_SHERLOCK_BOSS_DUCK:
                case DuckType.EGYPT_SCARAB_BOSS_DUCK:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBossDuckType(DuckType type)
        {
            switch (type)
            {
                case DuckType.MK_SAMUIL_BOSS_DUCK:
                case DuckType.USA_BOSS_DUCK:
                case DuckType.USA_ADMIRAL_BOSS_DUCK:
                case DuckType.FRENCH_MUSKETEER_BOSS_DUCK:
                case DuckType.JAPANESE_SAMURAI_BOSS_DUCK:
                case DuckType.BRITISH_SHERLOCK_BOSS_DUCK:
                case DuckType.EGYPT_SCARAB_BOSS_DUCK:
                    return true;
                default:
                    return false;
            }
        }

        public enum MovementPattern
        {
            GoStraight,
            GoTop,
            GoBottom
        }

        public enum DuckStartLane
        {
            Unspecified = 0,
            Lane1 = 1,
            Lane2 = 2,
            Lane3 = 3,
            Lane4 = 4,
            Lane5 = 5,
            Lane6 = 6,
            Lane7 = 7,
            Lane8 = 8,
            Lane9 = 9
        }

        public enum DuckPathProjection
        {
            Random = 0,
            Straight = 1,
            BezierMountain = 2,
            BezierValley = 3,
            DiagonalRise = 4,
            DiagonalFall = 5,
            SinWave = 6,
            SinWaveBig = 7,
            ZigZagTopFirst = 8,
            ZigZagBottomFirst = 9,
            SinWaveStartDown = 10,
            BounceMid = 11,
            DiagonalV = 12,
            DiagonalInverseV = 13,
            BossCenterWeave = 14,
            BossFigureEight = 15,
            BossCornerTraverse = 16
        }

        public enum MultiKillType
        {
            DoubleKill,
            TripleKill,
            QuadraKill
        }

        public enum SceneType
        {
            Splash,
            Menu,
            CampaignMap,
            Game,
            CampaignGame,
            Armory,
            Achievements,
            Loading
        }

        public static class DuckPoints
        {
            public const int TYPE_0 = 1;
            public const int TYPE_1 = 2;
            public const int TYPE_2 = 5;
            public const int TYPE_4 = 15;
            public const int MK_PHALARX = 1;
            public const int MK_ARCHER = 1;
            public const int MK_VOJVODA = 2;
            public const int FRENCH_REVOLUTIONARY = 2;
            public const int FRENCH_NAPOLEON = 2;
            public const int FRENCH_ARTIST = 2;
            public const int FRENCH_MUSKETEER = 2;
            public const int FRENCH_MUSKETEER_BOSS_DUCK = 8;
            public const int BRITISH_REDCOAT = 2;
            public const int BRITISH_POLICE = 3;
            public const int BRITISH_PUNK = 4;
            public const int BRITISH_SHERLOCK_BOSS_DUCK = 8;
            public const int USA_POLICE = 3;
            public const int USA_WORKER = 3;
            public const int USA_BUSINESS = 3;
            public const int USA_SWAT = 4;
            public const int USA_ADMIRAL = 4;
            public const int USA_ADMIRAL_BOSS_DUCK = 8;
            public const int USA_HOLLYWOOD = 3;
            public const int USA_LEO = 3;
            public const int USA_TOM = 3;
            public const int USA_MARINE = 4;
            public const int EGYPT_MUMMY = 3;
            public const int EGYPT_PHARAOH = 4;
            public const int EGYPT_ANUBIS = 4;
            public const int EGYPT_RAIDER = 3;
            public const int EGYPT_SCARAB = 4;
            public const int EGYPT_SCARAB_BOSS_DUCK = 8;
            public const int JAPANESE_SAMURAI = 4;
            public const int JAPANESE_SAMURAI_BOSS_DUCK = 8;
            public const int USA_BOSS_DUCK = 5;
            public const int JAPANESE_STRAW_DUCK = 4;
            public const int JAPANESE_KIMONO_DUCK = 4;
            public const int MK_SAMUIL_GUARD = 1;
            public const int MK_SAMUIL_ELITE = 1;
            public const int MK_SAMUIL_BOSS_DUCK = 8;

            public static int GetPoints(DuckType type)
            {
                switch (type)
                {
                    case DuckType.Type0: return TYPE_0;
                    case DuckType.Type1: return TYPE_1;
                    case DuckType.Type2: return TYPE_2;
                    case DuckType.Type4: return TYPE_4;
                    case DuckType.MK_PHALARX: return MK_PHALARX;
                    case DuckType.MK_ARCHER: return MK_ARCHER;
                    case DuckType.MK_VOJVODA: return MK_VOJVODA;
                    case DuckType.FRENCH_REVOLUTIONARY: return FRENCH_REVOLUTIONARY;
                    case DuckType.FRENCH_NAPOLEON: return FRENCH_NAPOLEON;
                    case DuckType.FRENCH_ARTIST: return FRENCH_ARTIST;
                    case DuckType.FRENCH_MUSKETEER: return FRENCH_MUSKETEER;
                    case DuckType.FRENCH_MUSKETEER_BOSS_DUCK: return FRENCH_MUSKETEER_BOSS_DUCK;
                    case DuckType.BRITISH_REDCOAT: return BRITISH_REDCOAT;
                    case DuckType.BRITISH_POLICE: return BRITISH_POLICE;
                    case DuckType.BRITISH_PUNK: return BRITISH_PUNK;
                    case DuckType.BRITISH_SHERLOCK_BOSS_DUCK: return BRITISH_SHERLOCK_BOSS_DUCK;
                    case DuckType.USA_POLICE: return USA_POLICE;
                    case DuckType.USA_WORKER: return USA_WORKER;
                    case DuckType.USA_BUSINESS: return USA_BUSINESS;
                    case DuckType.USA_SWAT: return USA_SWAT;
                    case DuckType.USA_ADMIRAL: return USA_ADMIRAL;
                    case DuckType.USA_ADMIRAL_BOSS_DUCK: return USA_ADMIRAL_BOSS_DUCK;
                    case DuckType.USA_HOLLYWOOD: return USA_HOLLYWOOD;
                    case DuckType.USA_LEO: return USA_LEO;
                    case DuckType.USA_TOM: return USA_TOM;
                    case DuckType.USA_MARINE: return USA_MARINE;
                    case DuckType.EGYPT_MUMMY: return EGYPT_MUMMY;
                    case DuckType.EGYPT_PHARAOH: return EGYPT_PHARAOH;
                    case DuckType.EGYPT_ANUBIS: return EGYPT_ANUBIS;
                    case DuckType.EGYPT_RAIDER: return EGYPT_RAIDER;
                    case DuckType.EGYPT_SCARAB: return EGYPT_SCARAB;
                    case DuckType.EGYPT_SCARAB_BOSS_DUCK: return EGYPT_SCARAB_BOSS_DUCK;
                    case DuckType.JAPANESE_SAMURAI: return JAPANESE_SAMURAI;
                    case DuckType.JAPANESE_SAMURAI_BOSS_DUCK: return JAPANESE_SAMURAI_BOSS_DUCK;
                    case DuckType.USA_BOSS_DUCK: return USA_BOSS_DUCK;
                    case DuckType.JAPANESE_STRAW_DUCK: return JAPANESE_STRAW_DUCK;
                    case DuckType.JAPANESE_KIMONO_DUCK: return JAPANESE_KIMONO_DUCK;
                    case DuckType.MK_SAMUIL_GUARD: return MK_SAMUIL_GUARD;
                    case DuckType.MK_SAMUIL_ELITE: return MK_SAMUIL_ELITE;
                    case DuckType.MK_SAMUIL_BOSS_DUCK: return MK_SAMUIL_BOSS_DUCK;
                    default: return TYPE_0;
                }
            }
        }

        public static class DuckHealth
        {
            public const int DEFAULT = 1;
            public const int MK_SAMUIL_BOSS_DUCK = 8;
            public const int USA_BOSS_DUCK = 8;
            public const int USA_ADMIRAL_BOSS_DUCK = 8;
            public const int FRENCH_MUSKETEER_BOSS_DUCK = 8;
            public const int JAPANESE_SAMURAI_BOSS_DUCK = 8;
            public const int BRITISH_SHERLOCK_BOSS_DUCK = 8;
            public const int EGYPT_SCARAB_BOSS_DUCK = 8;

            public static int GetMaxHealth(DuckType type)
            {
                switch (type)
                {
                    case DuckType.MK_SAMUIL_BOSS_DUCK: return MK_SAMUIL_BOSS_DUCK;
                    case DuckType.USA_BOSS_DUCK: return USA_BOSS_DUCK;
                    case DuckType.USA_ADMIRAL_BOSS_DUCK: return USA_ADMIRAL_BOSS_DUCK;
                    case DuckType.FRENCH_MUSKETEER_BOSS_DUCK: return FRENCH_MUSKETEER_BOSS_DUCK;
                    case DuckType.JAPANESE_SAMURAI_BOSS_DUCK: return JAPANESE_SAMURAI_BOSS_DUCK;
                    case DuckType.BRITISH_SHERLOCK_BOSS_DUCK: return BRITISH_SHERLOCK_BOSS_DUCK;
                    case DuckType.EGYPT_SCARAB_BOSS_DUCK: return EGYPT_SCARAB_BOSS_DUCK;
                    default: return DEFAULT;
                }
            }
        }

        public static class DuckSpawnProbability
        {
            public const float TYPE_0 = 0.56f;
            public const float TYPE_1 = 0.13f;
            public const float TYPE_2 = 0.07f;
            public const float TYPE_4 = 0.04f;
            public const float MK_PHALARX = 0.02f;
            public const float MK_ARCHER = 0.02f;
            public const float BRITISH_REDCOAT = 0.02f;
            public const float BRITISH_POLICE = 0.02f;
            public const float BRITISH_PUNK = 0.02f;
            public const float JAPANESE_SAMURAI = 0.02f;
            public const float FRENCH_MUSKETEER = 0.02f;

            public const float SINGLE_DUCK = 0.60f;
            public const float DOUBLE_DUCK = 0.20f;
            public const float FLEET_DUCK = 0.13f;
            public const float BONUS_WEAPON = 0.07f;
        }

        public static class ComboPoints
        {
            public const int DOUBLE_KILL = 30;
            public const int TRIPLE_KILL = 50;
            public const int QUADRA_KILL = 100;

            public static int GetPoints(MultiKillType type)
            {
                switch (type)
                {
                    case MultiKillType.DoubleKill: return DOUBLE_KILL;
                    case MultiKillType.TripleKill: return TRIPLE_KILL;
                    case MultiKillType.QuadraKill: return QUADRA_KILL;
                    default: return 0;
                }
            }
        }

        public static class DifficultyProgression
        {
            public static int GetBirdsForNextDifficulty(int currentDifficulty)
            {
                if (currentDifficulty >= 1 && currentDifficulty <= 5)
                    return 10;
                else if (currentDifficulty >= 6 && currentDifficulty <= 13)
                    return 15;
                else if (currentDifficulty >= 14 && currentDifficulty <= 23)
                    return 23;
                else if (currentDifficulty >= 24 && currentDifficulty <= 33)
                    return 52;
                else
                    return 100;
            }

            public static int GetBirdsForNextDifficultyArcadeVeryHard(int currentDifficulty)
            {
                if (currentDifficulty >= 1 && currentDifficulty <= 10)
                    return 4;
                else if (currentDifficulty >= 11 && currentDifficulty <= 20)
                    return 6;
                else if (currentDifficulty >= 21 && currentDifficulty <= 30)
                    return 8;
                else
                    return 10;
            }
        }

        public static class SpawnTiming
        {
            public const float ARCADE_VERY_HARD_MULTIPLIER = 0.90f;
            public const float ARCADE_VERY_HARD_MIN_DELAY = 0.16f;
            public const float ARCADE_VERY_HARD_INITIAL_DELAY = 0.70f;
            public const int ARCADE_VERY_HARD_MIN_ACTIVE_DUCKS = 5;
            public const int ARCADE_VERY_HARD_SPAWN_BATCH = 1;

            public static float GetSpawnDelay(int difficulty)
            {
                float baseDelay;
                float randomRange;

                if (difficulty < 11)
                {
                    baseDelay = 2.2f; // was 2.5f
                    randomRange = 1.0f;
                }
                else if (difficulty < 20)
                {
                    baseDelay = 1.7f; // was 2.0f
                    randomRange = 1.0f;
                }
                else if (difficulty < 30)
                {
                    baseDelay = 1.2f; // was 1.5f
                    randomRange = 1.0f;
                }
                else if (difficulty < 35)
                {
                    baseDelay = 0.7f; // was 1.0f
                    randomRange = 0.9f;
                }
                else
                {
                    baseDelay = 0.35f; // was 0.5f
                    randomRange = 0.4f;
                }

                return baseDelay - Random.Range(0f, randomRange);
            }
        }

        public static class DuckSpeed
        {
            public const float BASE_SPEED = 3f; // was 2f
            public const float ARCADE_VERY_HARD_MULTIPLIER = 1.075f;

            public static float GetSpeed(int difficulty)
            {
                float speedBonus;

                if (difficulty < 11)
                {
                    speedBonus = Random.Range(0f, 1.5f);
                }
                else if (difficulty < 21)
                {
                    speedBonus = Random.Range(0.25f, 1.75f);
                }
                else if (difficulty < 31)
                {
                    speedBonus = Random.Range(0.5f, 2.25f);
                }
                else
                {
                    speedBonus = Random.Range(1f, 3.25f);
                }

                return BASE_SPEED + speedBonus;
            }
        }

        public static class MultiplierColors
        {
            public static Color GetColor(int multiplier)
            {
                if (multiplier >= 1 && multiplier <= 3)
                    return Color.gray;
                else if (multiplier >= 4 && multiplier <= 5)
                    return Color.green;
                else
                    return Color.red;
            }
        }
    }
}
