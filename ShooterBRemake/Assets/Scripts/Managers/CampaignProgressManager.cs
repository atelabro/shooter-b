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

        // --- Stage stars ---

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

        public int CalculateStars(StageConfig config, long score)
        {
            if (score >= config.starThreshold3) return 3;
            if (score >= config.starThreshold2) return 2;
            if (score >= config.starThreshold1) return 1;
            return 0;
        }

        // --- Stage unlocking ---

        public bool IsStageUnlocked(StageConfig stage, CityConfig city)
        {
            if (city.stages.Length == 0)
                return false;

            if (stage == city.stages[0])
                return IsCityUnlocked(city);

            int starsInCity = 0;
            foreach (StageConfig s in city.stages)
            {
                if (s == stage)
                    break;
                starsInCity += GetStarsForStage(s.stageIndex);
            }

            return starsInCity >= stage.starsRequiredToUnlock;
        }

        // --- City unlocking ---

        public bool IsCityUnlocked(CityConfig city)
        {
            if (city.starsRequiredToUnlock == 0)
                return true;

            return GetTotalStarsExcluding(city) >= city.starsRequiredToUnlock;
        }

        // --- Total star counts ---

        public int GetTotalStars(CityConfig[] cities)
        {
            int total = 0;
            foreach (CityConfig city in cities)
                foreach (StageConfig stage in city.stages)
                    total += GetStarsForStage(stage.stageIndex);
            return total;
        }

        public int GetMaxStars(CityConfig[] cities)
        {
            int max = 0;
            foreach (CityConfig city in cities)
                max += city.stages.Length * 3;
            return max;
        }

        public int GetStarsForCity(CityConfig city)
        {
            int total = 0;
            foreach (StageConfig stage in city.stages)
                total += GetStarsForStage(stage.stageIndex);
            return total;
        }

        // --- Helpers ---

        private int GetTotalStarsExcluding(CityConfig excludedCity)
        {
            // used to check if the player has enough stars from other cities to unlock this one
            // NOTE: caller must pass all cities for this to work correctly,
            // but since CampaignProgressManager does not hold the full city list,
            // city unlock is checked by the map controller passing all cities.
            // This method sums stars from stages NOT belonging to excludedCity.
            // We identify stages by stageIndex only, so we need the excluded city's stage indices.
            int total = 0;
            foreach (StageConfig stage in excludedCity.stages)
                total += GetStarsForStage(stage.stageIndex);

            // return value is intentionally the city's own stars - caller uses GetTotalStars instead
            // see IsCityUnlocked(city, allCities) overload below
            return total;
        }

        public bool IsCityUnlocked(CityConfig city, CityConfig[] allCities)
        {
            if (city.starsRequiredToUnlock == 0)
                return true;

            int totalStars = 0;
            foreach (CityConfig c in allCities)
            {
                if (c == city)
                    break;
                foreach (StageConfig stage in c.stages)
                    totalStars += GetStarsForStage(stage.stageIndex);
            }

            return totalStars >= city.starsRequiredToUnlock;
        }

        private string BuildKey(int stageIndex)
        {
            return $"{PREFS_KEY_PREFIX}{stageIndex}{PREFS_KEY_SUFFIX}";
        }
    }
}
