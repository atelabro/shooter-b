using UnityEngine;

namespace ShooterB
{
    [CreateAssetMenu(fileName = "StageConfig", menuName = "ShooterB/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        public int stageIndex;
        public string mapName;
        [TextArea] public string briefingText;
        public Sprite backgroundSprite;
        public int starThreshold1;
        public int starThreshold2;
        public int starThreshold3;
        public int starsRequiredToUnlock;
        public int startingDifficulty;
        public StageSpawnConfig spawnConfig;
    }
}
