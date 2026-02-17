using UnityEngine;

namespace ShooterB
{
    [CreateAssetMenu(fileName = "StageConfig", menuName = "ShooterB/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        public int stageIndex;
        public string cityName;
        [TextArea] public string briefingText;
        public string backgroundId;
        public int duckKillGoal;
        public int starThreshold1;
        public int starThreshold2;
        public int starThreshold3;
        public int starsRequiredToUnlock;
        public int startingDifficulty;
    }
}
