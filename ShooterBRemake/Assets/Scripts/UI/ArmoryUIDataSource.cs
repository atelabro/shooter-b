using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public static class ArmoryUIDataSource
    {
        private struct WeaponStats
        {
            public Constants.WeaponFireMode fireMode;
            public float fireDelay;
            public float reloadDelay;
            public float travelSpeed;
            public float areaOfEffect;
            public int chainLightning;
            public string description;
        }

        private static readonly Dictionary<Constants.WeaponType, WeaponStats> StatsByWeapon =
            new Dictionary<Constants.WeaponType, WeaponStats>
            {
                {
                    Constants.WeaponType.Rifle,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.3f,
                        reloadDelay = 0.8f,
                        travelSpeed = 50f,
                        areaOfEffect = 0.9f,
                        chainLightning = 0,
                        description = "Hunters know this classic. Precise and dependable."
                    }
                },
                {
                    Constants.WeaponType.Cabirne,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.2f,
                        reloadDelay = 0.6f,
                        travelSpeed = 45f,
                        areaOfEffect = 0.4f,
                        chainLightning = 0,
                        description = "Sharper shots for cleaner picks in tight moments."
                    }
                },
                {
                    Constants.WeaponType.Beretta,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.HoldAutomatic,
                        fireDelay = 0.2f,
                        reloadDelay = 2.4f,
                        travelSpeed = 50f,
                        areaOfEffect = 0.24f,
                        chainLightning = 0,
                        description = "World-war steel turned into close-range spray control."
                    }
                },
                {
                    Constants.WeaponType.LaserGun,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.26f,
                        reloadDelay = 0.83f,
                        travelSpeed = 50f,
                        areaOfEffect = 1.1f,
                        chainLightning = 0,
                        description = "High-tech beam bursts with broad impact coverage."
                    }
                },
                {
                    Constants.WeaponType.PiranhaGun,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.9f,
                        reloadDelay = 1.4f,
                        travelSpeed = 16.67f,
                        areaOfEffect = 1.1f,
                        chainLightning = 0,
                        description = "Load piranhas into the launcher and cause pure chaos."
                    }
                },
                {
                    Constants.WeaponType.TeslaGun,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.1f,
                        reloadDelay = 0.4f,
                        travelSpeed = 60f,
                        areaOfEffect = 6.0f,
                        chainLightning = 2,
                        description = "Tesla coils chain lightning across nearby targets."
                    }
                },
                {
                    Constants.WeaponType.MrSulko,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.HoldAutomatic,
                        fireDelay = 0.1f,
                        reloadDelay = 0.72f,
                        travelSpeed = 58f,
                        areaOfEffect = 0.4f,
                        chainLightning = 0,
                        description = "A biochemical monster made for relentless pressure."
                    }
                }
            };

        private static readonly Constants.WeaponType[] OrderedWeapons =
        {
            Constants.WeaponType.PiranhaGun,
            Constants.WeaponType.Rifle,
            Constants.WeaponType.Cabirne,
            Constants.WeaponType.Beretta,
            Constants.WeaponType.LaserGun,
            Constants.WeaponType.MrSulko,
            Constants.WeaponType.TeslaGun
        };

        public static IReadOnlyList<Constants.WeaponType> GetOrderedWeapons()
        {
            return OrderedWeapons;
        }

        public static HashSet<Constants.WeaponType> BuildDefaultUnlockedSet()
        {
            return new HashSet<Constants.WeaponType>
            {
                Constants.WeaponType.PiranhaGun
            };
        }

        public static WeaponCardViewModel BuildCardModel(Constants.WeaponType type, Sprite iconOverride = null)
        {
            WeaponStats stats = GetStats(type);
            WeaponCardViewModel model = new WeaponCardViewModel
            {
                weaponType = type,
                displayName = GetDisplayName(type),
                description = stats.description,
                cost = type == Constants.WeaponType.PiranhaGun ? 0 : GenerateCost(stats),
                fireTypeLabel = stats.fireMode == Constants.WeaponFireMode.HoldAutomatic ? "Automatic" : "Single Tap",
                fireRateLabel = $"{(1f / Mathf.Max(0.01f, stats.fireDelay)):0.0} shots/sec",
                reloadLabel = $"{stats.reloadDelay:0.00}s",
                travelSpeedLabel = $"{stats.travelSpeed:0.##}",
                chainLightningLabel = stats.chainLightning > 0 ? $"{stats.chainLightning} jumps" : "None",
                aoeLabel = $"{stats.areaOfEffect:0.##}",
                icon = iconOverride
            };

            return model;
        }

        private static WeaponStats GetStats(Constants.WeaponType type)
        {
            if (StatsByWeapon.TryGetValue(type, out WeaponStats stats))
                return stats;

            return StatsByWeapon[Constants.WeaponType.Rifle];
        }

        private static int GenerateCost(WeaponStats stats)
        {
            float fireScore = 1f / Mathf.Max(0.08f, stats.fireDelay);
            float reloadScore = 1f / Mathf.Max(0.2f, stats.reloadDelay);
            float speedScore = stats.travelSpeed / 25f;
            float aoeScore = stats.areaOfEffect;
            float chainScore = stats.chainLightning * 1.8f;
            float autoBonus = stats.fireMode == Constants.WeaponFireMode.HoldAutomatic ? 1.5f : 0f;

            float totalScore = (fireScore * 8f) + (reloadScore * 7f) + (speedScore * 5f) + (aoeScore * 9f) + (chainScore * 12f) + (autoBonus * 10f);
            int rawCost = Mathf.RoundToInt(60f + totalScore * 8f);
            int roundedCost = Mathf.RoundToInt(rawCost / 5f) * 5;
            return Mathf.Clamp(roundedCost, 80, 900);
        }

        private static string GetDisplayName(Constants.WeaponType type)
        {
            switch (type)
            {
                case Constants.WeaponType.PiranhaGun:
                    return "Piranha Gun";
                case Constants.WeaponType.MrSulko:
                    return "Mr Sulko";
                case Constants.WeaponType.LaserGun:
                    return "Laser Gun";
                default:
                    return type.ToString();
            }
        }
    }
}
