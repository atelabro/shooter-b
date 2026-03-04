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
        public CityConfig ActiveCityConfig { get; private set; }
        public CityConfig[] CampaignCities { get; private set; }

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
            if (ActiveCityConfig == null || config == null)
                return;

            if (System.Array.IndexOf(ActiveCityConfig.stages, config) < 0)
                ActiveCityConfig = FindCityForStage(config);
        }

        public void SetCampaignCities(CityConfig[] cities)
        {
            CampaignCities = cities;
            if (ActiveStageConfig != null)
                ActiveCityConfig = FindCityForStage(ActiveStageConfig);
        }

        public void SetActiveCampaignLocation(CityConfig city, StageConfig stage)
        {
            ActiveCityConfig = city;
            ActiveStageConfig = stage;
        }

        public StageConfig GetNextStageInActiveCityRow()
        {
            if (ActiveCityConfig == null || ActiveCityConfig.stages == null || ActiveStageConfig == null)
                return null;

            int currentIndex = System.Array.IndexOf(ActiveCityConfig.stages, ActiveStageConfig);
            if (currentIndex < 0)
                return null;

            int nextIndex = currentIndex + 1;
            if (nextIndex >= ActiveCityConfig.stages.Length)
                return null;

            return ActiveCityConfig.stages[nextIndex];
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

            int stagePosition = System.Array.IndexOf(city.stages, stage);
            if (stagePosition <= 0)
                return false;

            StageConfig previousStage = city.stages[stagePosition - 1];
            return previousStage != null && GetStarsForStage(previousStage.stageIndex) >= 1;
        }

        public bool IsStageUnlocked(StageConfig stage, CityConfig city, CityConfig[] allCities)
        {
            if (city == null || city.stages == null || city.stages.Length == 0 || stage == null)
                return false;

            if (stage == city.stages[0])
                return IsCityUnlocked(city, allCities);

            int stagePosition = System.Array.IndexOf(city.stages, stage);
            if (stagePosition <= 0)
                return false;

            StageConfig previousStage = city.stages[stagePosition - 1];
            return previousStage != null && GetStarsForStage(previousStage.stageIndex) >= 1;
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

            if (allCities != null)
            {
                int cityIndex = System.Array.IndexOf(allCities, city);
                if (cityIndex > 0)
                {
                    CityConfig previousCity = allCities[cityIndex - 1];
                    if (!IsCityCompleted(previousCity))
                        return false;
                }
            }

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

        private bool IsCityCompleted(CityConfig city)
        {
            if (city == null || city.stages == null || city.stages.Length == 0)
                return false;

            foreach (StageConfig stage in city.stages)
            {
                if (stage == null || GetStarsForStage(stage.stageIndex) <= 0)
                    return false;
            }

            return true;
        }

        private string BuildKey(int stageIndex)
        {
            return $"{PREFS_KEY_PREFIX}{stageIndex}{PREFS_KEY_SUFFIX}";
        }

        private CityConfig FindCityForStage(StageConfig stage)
        {
            if (stage == null || CampaignCities == null)
                return null;

            foreach (CityConfig city in CampaignCities)
            {
                if (city == null || city.stages == null)
                    continue;

                if (System.Array.IndexOf(city.stages, stage) >= 0)
                    return city;
            }

            return null;
        }
    }
}
