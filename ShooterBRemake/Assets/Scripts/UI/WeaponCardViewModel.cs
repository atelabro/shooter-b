using UnityEngine;

namespace ShooterB
{
    public class WeaponCardViewModel
    {
        public Constants.WeaponType weaponType;
        public bool isConsumable;
        public string displayName;
        public string description;
        public int cost;
        public int ownedCount;
        public string fireTypeLabel;
        public string fireRateLabel;
        public string reloadLabel;
        public string travelSpeedLabel;
        public string bulletsLabel;
        public string aoeLabel;
        public Sprite icon;
    }
}
