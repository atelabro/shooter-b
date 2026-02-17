using UnityEngine;

namespace ShooterB
{
    [CreateAssetMenu(fileName = "CityConfig", menuName = "ShooterB/City Config")]
    public class CityConfig : ScriptableObject
    {
        public string cityName;
        [TextArea] public string briefingText;
        public Vector2 pinPosition;
        public StageConfig[] stages;
        public int starsRequiredToUnlock;
    }
}
