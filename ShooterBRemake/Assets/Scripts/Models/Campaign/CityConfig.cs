using UnityEngine;

namespace ShooterB
{
    [CreateAssetMenu(fileName = "CityConfig", menuName = "ShooterB/City Config")]
    public class CityConfig : ScriptableObject
    {
        public string cityName;
        public string cityNameMk;
        [TextArea] public string briefingText;
        [TextArea] public string briefingTextMk;
        public string pinDisplayName;
        public string pinDisplayNameMk;
        public bool forcePinNameTwoRows;
        public string pinSpriteResourcePath;
        public Vector2 pinPosition;
        public StageConfig[] stages;
        public int starsRequiredToUnlock;
    }
}
