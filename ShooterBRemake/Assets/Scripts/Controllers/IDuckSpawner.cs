using UnityEngine;

namespace ShooterB
{
    public interface IDuckSpawner
    {
        int ActiveDuckCount { get; }
        int DamageAllActiveDucks(int damageAmount, Constants.WeaponType weaponType);
        void ReturnDuckToPool(GameObject duck);
    }
}
