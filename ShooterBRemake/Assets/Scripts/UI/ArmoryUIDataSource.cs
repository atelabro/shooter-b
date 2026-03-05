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
            public int maxBullets;
            public float travelSpeed;
            public float areaOfEffect;
            public int chainLightning;
            public string descriptionKey;
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
                        maxBullets = 2,
                        travelSpeed = 50f,
                        areaOfEffect = 1.22f,
                        chainLightning = 0,
                        descriptionKey = "armory.weapon.description.rifle"
                    }
                },
                {
                    Constants.WeaponType.Cabirne,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.2f,
                        reloadDelay = 0.6f,
                        maxBullets = 7,
                        travelSpeed = 45f,
                        areaOfEffect = 0.54f,
                        chainLightning = 0,
                        descriptionKey = "armory.weapon.description.cabirne"
                    }
                },
                {
                    Constants.WeaponType.Beretta,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.HoldAutomatic,
                        fireDelay = 0.2f,
                        reloadDelay = 2.4f,
                        maxBullets = 14,
                        travelSpeed = 50f,
                        areaOfEffect = 0.32f,
                        chainLightning = 0,
                        descriptionKey = "armory.weapon.description.beretta"
                    }
                },
                {
                    Constants.WeaponType.LaserGun,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.26f,
                        reloadDelay = 0.83f,
                        maxBullets = 11,
                        travelSpeed = 50f,
                        areaOfEffect = 1.49f,
                        chainLightning = 0,
                        descriptionKey = "armory.weapon.description.laser"
                    }
                },
                {
                    Constants.WeaponType.PiranhaGun,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.9f,
                        reloadDelay = 1.4f,
                        maxBullets = 3,
                        travelSpeed = 16.67f,
                        areaOfEffect = 1.49f,
                        chainLightning = 0,
                        descriptionKey = "armory.weapon.description.piranha"
                    }
                },
                {
                    Constants.WeaponType.TeslaGun,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.SingleTap,
                        fireDelay = 0.1f,
                        reloadDelay = 0.4f,
                        maxBullets = 5,
                        travelSpeed = 60f,
                        areaOfEffect = 6.0f,
                        chainLightning = 2,
                        descriptionKey = "armory.weapon.description.tesla"
                    }
                },
                {
                    Constants.WeaponType.MrSulko,
                    new WeaponStats
                    {
                        fireMode = Constants.WeaponFireMode.HoldAutomatic,
                        fireDelay = 0.1f,
                        reloadDelay = 0.72f,
                        maxBullets = 14,
                        travelSpeed = 58f,
                        areaOfEffect = 0.54f,
                        chainLightning = 0,
                        descriptionKey = "armory.weapon.description.mrsulko"
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
                Constants.WeaponType.PiranhaGun,
                Constants.WeaponType.Rifle,
                Constants.WeaponType.Cabirne,
                Constants.WeaponType.Beretta,
                Constants.WeaponType.MrSulko,
                Constants.WeaponType.TeslaGun
            };
        }

        public static WeaponCardViewModel BuildCardModel(Constants.WeaponType type, Sprite iconOverride = null)
        {
            WeaponStats stats = GetStats(type);
            string weaponNameKey = GetWeaponNameKey(type);
            string fireModeKey = stats.fireMode == Constants.WeaponFireMode.HoldAutomatic
                ? "armory.fire_mode.automatic"
                : "armory.fire_mode.single_tap";

            WeaponCardViewModel model = new WeaponCardViewModel
            {
                weaponType = type,
                displayName = weaponNameKey == null
                    ? GetDefaultDisplayName(type)
                    : LocalizationManager.Instance.Get(weaponNameKey, GetDefaultDisplayName(type)),
                description = LocalizationManager.Instance.Get(stats.descriptionKey, GetDefaultDescription(type)),
                cost = type == Constants.WeaponType.PiranhaGun ? 0 : GenerateCost(stats),
                fireTypeLabel = LocalizationManager.Instance.Get(
                    fireModeKey,
                    stats.fireMode == Constants.WeaponFireMode.HoldAutomatic ? "Automatic" : "Single Tap"),
                fireRateLabel = $"{(1f / Mathf.Max(0.01f, stats.fireDelay)):0.0} shots/sec",
                reloadLabel = $"{stats.reloadDelay:0.00}s",
                travelSpeedLabel = $"{stats.travelSpeed:0.##}",
                bulletsLabel = stats.maxBullets.ToString(),
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

        private static string GetDefaultDisplayName(Constants.WeaponType type)
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

        private static string GetDefaultDescription(Constants.WeaponType type)
        {
            switch (type)
            {
                case Constants.WeaponType.Rifle:
                    return "Hunters know this classic. Precise and dependable.";
                case Constants.WeaponType.Cabirne:
                    return "Sharper shots for cleaner picks in tight moments.";
                case Constants.WeaponType.Beretta:
                    return "World-war steel turned into close-range spray control.";
                case Constants.WeaponType.LaserGun:
                    return "High-tech beam bursts with broad impact coverage.";
                case Constants.WeaponType.PiranhaGun:
                    return "Load piranhas into the launcher and cause pure chaos.";
                case Constants.WeaponType.TeslaGun:
                    return "Fires electric shots that chain lightning across nearby ducks.";
                case Constants.WeaponType.MrSulko:
                    return "A biochemical monster made for relentless pressure.";
                default:
                    return string.Empty;
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
    }
}
