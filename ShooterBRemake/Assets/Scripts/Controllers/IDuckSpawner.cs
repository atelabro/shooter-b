using UnityEngine;

namespace ShooterB
{
    public interface IDuckSpawner
    {
        int ActiveDuckCount { get; }
        int DamageAllActiveDucks(int damageAmount, Constants.WeaponType weaponType);
        int FreezeAllActiveDucks(float duration);
        void PauseSpawningFor(float duration);
        void ReturnDuckToPool(GameObject duck);
    }
}
