using UnityEngine;

namespace ShooterB
{
    public class CampaignProgressManager : MonoBehaviour
    {
        private static CampaignProgressManager instance;
        public static CampaignProgressManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("CampaignProgressManager");
                    instance = go.AddComponent<CampaignProgressManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private const string PREFS_KEY_PREFIX = "Campaign_Stage_";
        private const string PREFS_KEY_SUFFIX = "_Stars";

        public StageConfig ActiveStageConfig { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetActiveStage(StageConfig config)
        {
            ActiveStageConfig = config;
        }

        public int GetStarsForStage(int stageIndex)
        {
            return PlayerPrefs.GetInt(BuildKey(stageIndex), 0);
        }

        public void SaveStageStars(int stageIndex, int stars)
        {
            int current = GetStarsForStage(stageIndex);
            if (stars > current)
            {
                PlayerPrefs.SetInt(BuildKey(stageIndex), stars);
                PlayerPrefs.Save();
            }
        }

        public bool IsStageUnlocked(StageConfig config)
        {
            if (config.stageIndex == 0)
                return true;

            int totalStars = 0;
            for (int i = 0; i < config.stageIndex; i++)
                totalStars += GetStarsForStage(i);

            return totalStars >= config.starsRequiredToUnlock;
        }

        public int CalculateStars(StageConfig config, long score)
        {
            if (score >= config.starThreshold3) return 3;
            if (score >= config.starThreshold2) return 2;
            if (score >= config.starThreshold1) return 1;
            return 0;
        }

        private string BuildKey(int stageIndex)
        {
            return $"{PREFS_KEY_PREFIX}{stageIndex}{PREFS_KEY_SUFFIX}";
        }
    }
}
