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
            public string descriptionKey;
        }

        private static readonly Constants.WeaponType[] OrderedWeapons =
        {
            Constants.WeaponType.PiranhaGun,
            Constants.WeaponType.Cabirne,
            Constants.WeaponType.Beretta,
            Constants.WeaponType.Rifle,
            Constants.WeaponType.MrSulko,
            Constants.WeaponType.LaserGun,
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

        public static WeaponCardViewModel BuildCardModel(Constants.WeaponType type, Weapon weaponPrefab, Sprite iconOverride = null)
        {
            WeaponStats stats = GetStats(type, weaponPrefab);
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
                cost = GetHardcodedCost(type),
                fireTypeLabel = LocalizationManager.Instance.Get(
                    fireModeKey,
                    stats.fireMode == Constants.WeaponFireMode.HoldAutomatic ? "Automatic" : "Single Tap"),
                fireRateLabel = $"{(1f / Mathf.Max(0.01f, stats.fireDelay)):0.0} shots/sec",
                reloadLabel = $"{stats.reloadDelay:0.00}s",
                travelSpeedLabel = $"{stats.travelSpeed:0.##}",
                bulletsLabel = stats.maxBullets.ToString(),
                aoeLabel = $"{stats.areaOfEffect:0.##}",
                icon = iconOverride != null ? iconOverride : weaponPrefab != null ? weaponPrefab.weaponIcon : null
            };

            return model;
        }

        public static WeaponCardViewModel BuildZeusThunderModel(Sprite iconOverride = null)
        {
            return new WeaponCardViewModel
            {
                weaponType = Constants.WeaponType.TeslaGun,
                isConsumable = true,
                displayName = LocalizationManager.Instance.Get("superweapon.zeus.name", "Zeus Thunder"),
                description = LocalizationManager.Instance.Get(
                    "superweapon.zeus.description",
                    "Drag Zeus into battle to call thunder from above. Deals 10 damage to every duck on screen."),
                cost = Constants.ZEUS_THUNDER_COST,
                ownedCount = GameManager.Instance.ZeusThunderCount,
                fireTypeLabel = LocalizationManager.Instance.Get("superweapon.zeus.type", "Drag Power"),
                fireRateLabel = string.Empty,
                reloadLabel = string.Empty,
                travelSpeedLabel = string.Empty,
                bulletsLabel = string.Empty,
                aoeLabel = LocalizationManager.Instance.Get("superweapon.zeus.aoe", "Full screen"),
                icon = iconOverride
            };
        }

        private static WeaponStats GetStats(Constants.WeaponType type, Weapon weaponPrefab)
        {
            WeaponStats stats = new WeaponStats
            {
                fireMode = Constants.WeaponFireMode.SingleTap,
                fireDelay = 0.3f,
                reloadDelay = 0.8f,
                maxBullets = 1,
                travelSpeed = 0f,
                areaOfEffect = 0f,
                descriptionKey = GetDescriptionKey(type)
            };

            if (weaponPrefab == null)
            {
                GameLog.Warning($"[ARMORY] Missing weapon prefab for {type}; using fallback display values.");
                return stats;
            }

            stats.fireMode = weaponPrefab.fireMode;
            stats.fireDelay = weaponPrefab.fireDelay;
            stats.reloadDelay = weaponPrefab.refillDelay;
            stats.maxBullets = weaponPrefab.maxBullets;

            if (weaponPrefab.bulletPrefab == null)
            {
                GameLog.Warning($"[ARMORY] Weapon prefab {weaponPrefab.name} has no bullet prefab assigned.");
                return stats;
            }

            Bullet bullet = weaponPrefab.bulletPrefab.GetComponent<Bullet>();
            if (bullet == null)
            {
                GameLog.Warning($"[ARMORY] Bullet prefab {weaponPrefab.bulletPrefab.name} is missing Bullet component.");
                return stats;
            }

            stats.travelSpeed = bullet.baseSpeed;
            stats.areaOfEffect = bullet.effectiveRadius;

            TeslaBullet teslaBullet = bullet as TeslaBullet;
            if (teslaBullet != null)
                stats.areaOfEffect = teslaBullet.aoeRadius;

            return stats;
        }

        private static string GetDescriptionKey(Constants.WeaponType type)
        {
            switch (type)
            {
                case Constants.WeaponType.Rifle:
                    return "armory.weapon.description.rifle";
                case Constants.WeaponType.Cabirne:
                    return "armory.weapon.description.cabirne";
                case Constants.WeaponType.Beretta:
                    return "armory.weapon.description.beretta";
                case Constants.WeaponType.LaserGun:
                    return "armory.weapon.description.laser";
                case Constants.WeaponType.PiranhaGun:
                    return "armory.weapon.description.piranha";
                case Constants.WeaponType.TeslaGun:
                    return "armory.weapon.description.tesla";
                case Constants.WeaponType.MrSulko:
                    return "armory.weapon.description.mrsulko";
                default:
                    return string.Empty;
            }
        }

        private static int GetHardcodedCost(Constants.WeaponType type)
        {
            switch (type)
            {
                case Constants.WeaponType.PiranhaGun: return 0;
                case Constants.WeaponType.Cabirne:    return 150;
                case Constants.WeaponType.Beretta:    return 200;
                case Constants.WeaponType.Rifle:      return 350;
                case Constants.WeaponType.MrSulko:    return 550;
                case Constants.WeaponType.LaserGun:   return 800;
                case Constants.WeaponType.TeslaGun:   return 1050;
                default:                              return 500;
            }
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
