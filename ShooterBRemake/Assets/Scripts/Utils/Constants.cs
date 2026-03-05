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
        public const string PREFS_COINS = "Coins";
        public const string PREFS_LANGUAGE = "Language";
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
            Type3,
            Type4,
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
            JAPANESE_SAMURAI
        }

        public static string GetDuckDisplayName(DuckType type)
        {
            switch (type)
            {
                case DuckType.Type0: return "Duck I";
                case DuckType.Type1: return "Duck II";
                case DuckType.Type2: return "Duck III";
                case DuckType.Type3: return "Duck IV";
                case DuckType.Type4: return "Duck V";
                case DuckType.MK_PHALARX: return "Macedonian Phalanx";
                case DuckType.MK_ARCHER: return "Macedonian Archer";
                case DuckType.MK_VOJVODA: return "Macedonian Vojvoda";
                case DuckType.FRENCH_REVOLUTIONARY: return "French Revolutionary";
                case DuckType.FRENCH_NAPOLEON: return "French Napoleon";
                case DuckType.FRENCH_ARTIST: return "French Artist";
                case DuckType.BRITISH_REDCOAT: return "British Redcoat";
                case DuckType.BRITISH_POLICE: return "British Police";
                case DuckType.BRITISH_PUNK: return "British Punk";
                case DuckType.USA_POLICE: return "USA Police";
                case DuckType.USA_WORKER: return "USA Worker";
                case DuckType.USA_BUSINESS: return "USA Business";
                case DuckType.JAPANESE_SAMURAI: return "Japanese Samurai";
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
                case DuckType.Type3: return "TYPE_3";
                case DuckType.Type4: return "TYPE_4";
                case DuckType.MK_PHALARX: return "MK_PHALARX";
                case DuckType.MK_ARCHER: return "MK_ARCHER";
                case DuckType.MK_VOJVODA: return "MK_VOJVODA";
                case DuckType.FRENCH_REVOLUTIONARY: return "FRENCH_REVOLUTIONARY";
                case DuckType.FRENCH_NAPOLEON: return "FRENCH_NAPOLEON";
                case DuckType.FRENCH_ARTIST: return "FRENCH_ARTIST";
                case DuckType.BRITISH_REDCOAT: return "BRITISH_REDCOAT";
                case DuckType.BRITISH_POLICE: return "BRITISH_POLICE";
                case DuckType.BRITISH_PUNK: return "BRITISH_PUNK";
                case DuckType.USA_POLICE: return "USA_POLICE";
                case DuckType.USA_WORKER: return "USA_WORKER";
                case DuckType.USA_BUSINESS: return "USA_BUSINESS";
                case DuckType.JAPANESE_SAMURAI: return "JAPANESE_SAMURAI";
                default: return type.ToString();
            }
        }

        public enum MovementPattern
        {
            GoStraight,
            GoTop,
            GoBottom
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
            public const int TYPE_3 = 10;
            public const int TYPE_4 = 15;
            public const int MK_PHALARX = 1;
            public const int MK_ARCHER = 1;
            public const int MK_VOJVODA = 2;
            public const int FRENCH_REVOLUTIONARY = 2;
            public const int FRENCH_NAPOLEON = 2;
            public const int FRENCH_ARTIST = 2;
            public const int BRITISH_REDCOAT = 2;
            public const int BRITISH_POLICE = 3;
            public const int BRITISH_PUNK = 4;
            public const int USA_POLICE = 3;
            public const int USA_WORKER = 3;
            public const int USA_BUSINESS = 3;
            public const int JAPANESE_SAMURAI = 4;

            public static int GetPoints(DuckType type)
            {
                switch (type)
                {
                    case DuckType.Type0: return TYPE_0;
                    case DuckType.Type1: return TYPE_1;
                    case DuckType.Type2: return TYPE_2;
                    case DuckType.Type3: return TYPE_3;
                    case DuckType.Type4: return TYPE_4;
                    case DuckType.MK_PHALARX: return MK_PHALARX;
                    case DuckType.MK_ARCHER: return MK_ARCHER;
                    case DuckType.MK_VOJVODA: return MK_VOJVODA;
                    case DuckType.FRENCH_REVOLUTIONARY: return FRENCH_REVOLUTIONARY;
                    case DuckType.FRENCH_NAPOLEON: return FRENCH_NAPOLEON;
                    case DuckType.FRENCH_ARTIST: return FRENCH_ARTIST;
                    case DuckType.BRITISH_REDCOAT: return BRITISH_REDCOAT;
                    case DuckType.BRITISH_POLICE: return BRITISH_POLICE;
                    case DuckType.BRITISH_PUNK: return BRITISH_PUNK;
                    case DuckType.USA_POLICE: return USA_POLICE;
                    case DuckType.USA_WORKER: return USA_WORKER;
                    case DuckType.USA_BUSINESS: return USA_BUSINESS;
                    case DuckType.JAPANESE_SAMURAI: return JAPANESE_SAMURAI;
                    default: return TYPE_0;
                }
            }
        }

        public static class DuckSpawnProbability
        {
            public const float TYPE_0 = 0.58f;
            public const float TYPE_1 = 0.13f;
            public const float TYPE_2 = 0.07f;
            public const float TYPE_3 = 0.06f;
            public const float TYPE_4 = 0.04f;
            public const float MK_PHALARX = 0.02f;
            public const float MK_ARCHER = 0.02f;
            public const float BRITISH_REDCOAT = 0.02f;
            public const float BRITISH_POLICE = 0.02f;
            public const float BRITISH_PUNK = 0.02f;
            public const float JAPANESE_SAMURAI = 0.02f;

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
