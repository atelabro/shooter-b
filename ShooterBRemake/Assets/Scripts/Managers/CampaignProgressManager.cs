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
        private const string CityFirstCompletionAdShownPrefix = "Campaign_CityFirstCompletionAdShown_";

        public StageConfig ActiveStageConfig { get; private set; }
        public CityConfig ActiveCityConfig { get; private set; }
        public CityConfig[] CampaignCities { get; private set; }
        public bool HasPendingMapFocusTransition { get; private set; }

        private CityConfig pendingMapFocusFromCity;
        private StageConfig pendingMapFocusFromStage;
        private CityConfig pendingMapFocusToCity;
        private StageConfig pendingMapFocusToStage;
        private float pendingMapFocusMinDelay;

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

        public void SetPendingMapFocusTransition(
            CityConfig fromCity,
            StageConfig fromStage,
            CityConfig toCity,
            StageConfig toStage,
            float minDelaySeconds)
        {
            if (fromCity == null || fromStage == null || toCity == null || toStage == null)
            {
                ClearPendingMapFocusTransition();
                return;
            }

            pendingMapFocusFromCity = fromCity;
            pendingMapFocusFromStage = fromStage;
            pendingMapFocusToCity = toCity;
            pendingMapFocusToStage = toStage;
            pendingMapFocusMinDelay = Mathf.Max(0f, minDelaySeconds);
            HasPendingMapFocusTransition = true;
        }

        public bool TryConsumePendingMapFocusTransition(
            out CityConfig fromCity,
            out StageConfig fromStage,
            out CityConfig toCity,
            out StageConfig toStage,
            out float minDelaySeconds)
        {
            if (!HasPendingMapFocusTransition)
            {
                fromCity = null;
                fromStage = null;
                toCity = null;
                toStage = null;
                minDelaySeconds = 0f;
                return false;
            }

            fromCity = pendingMapFocusFromCity;
            fromStage = pendingMapFocusFromStage;
            toCity = pendingMapFocusToCity;
            toStage = pendingMapFocusToStage;
            minDelaySeconds = pendingMapFocusMinDelay;

            ClearPendingMapFocusTransition();
            return true;
        }

        public void ClearPendingMapFocusTransition()
        {
            HasPendingMapFocusTransition = false;
            pendingMapFocusFromCity = null;
            pendingMapFocusFromStage = null;
            pendingMapFocusToCity = null;
            pendingMapFocusToStage = null;
            pendingMapFocusMinDelay = 0f;
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

        public void UnlockAllCityStagesExceptCountryside(CityConfig[] cities = null)
        {
            CityConfig[] sourceCities = GetResolvedCities(cities);
            if (sourceCities == null || sourceCities.Length == 0)
            {
                GameLog.Warning("[CampaignProgressManager] Cannot unlock city stages because no campaign cities are configured.");
                return;
            }

            for (int cityIndex = 0; cityIndex < sourceCities.Length; cityIndex++)
            {
                CityConfig city = sourceCities[cityIndex];
                if (ShouldSkipCityForBulkStageToggle(city))
                    continue;

                if (city.stages == null)
                    continue;

                for (int stageIndex = 0; stageIndex < city.stages.Length; stageIndex++)
                {
                    StageConfig stage = city.stages[stageIndex];
                    if (stage == null)
                        continue;

                    SetStageStars(stage.stageIndex, 1);
                }
            }

            PlayerPrefs.Save();
        }

        public void LockAllCityStagesExceptCountryside(CityConfig[] cities = null)
        {
            CityConfig[] sourceCities = GetResolvedCities(cities);
            if (sourceCities == null || sourceCities.Length == 0)
            {
                GameLog.Warning("[CampaignProgressManager] Cannot lock city stages because no campaign cities are configured.");
                return;
            }

            for (int cityIndex = 0; cityIndex < sourceCities.Length; cityIndex++)
            {
                CityConfig city = sourceCities[cityIndex];
                if (ShouldSkipCityForBulkStageToggle(city))
                    continue;

                if (city.stages == null)
                    continue;

                for (int stageIndex = 0; stageIndex < city.stages.Length; stageIndex++)
                {
                    StageConfig stage = city.stages[stageIndex];
                    if (stage == null)
                        continue;

                    SetStageStars(stage.stageIndex, 0);
                }
            }

            PlayerPrefs.Save();
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
                    CityConfig previousCity = FindPreviousPlayableCity(allCities, cityIndex);
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

        private static CityConfig FindPreviousPlayableCity(CityConfig[] allCities, int cityIndex)
        {
            for (int i = cityIndex - 1; i >= 0; i--)
            {
                CityConfig previousCity = allCities[i];
                if (previousCity != null && previousCity.stages != null && previousCity.stages.Length > 0)
                    return previousCity;
            }

            return null;
        }

        public bool IsCityCompleted(CityConfig city)
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

        public bool HasShownCityFirstCompletionAd(CityConfig city)
        {
            if (city == null)
                return false;

            return PlayerPrefs.GetInt(BuildCityFirstCompletionAdShownKey(city), 0) == 1;
        }

        public void MarkCityFirstCompletionAdShown(CityConfig city)
        {
            if (city == null)
                return;

            PlayerPrefs.SetInt(BuildCityFirstCompletionAdShownKey(city), 1);
            PlayerPrefs.Save();
        }

        private string BuildKey(int stageIndex)
        {
            return $"{PREFS_KEY_PREFIX}{stageIndex}{PREFS_KEY_SUFFIX}";
        }

        private void SetStageStars(int stageIndex, int stars)
        {
            PlayerPrefs.SetInt(BuildKey(stageIndex), Mathf.Max(0, stars));
        }

        private CityConfig[] GetResolvedCities(CityConfig[] cities)
        {
            if (cities != null && cities.Length > 0)
                return cities;

            return CampaignCities;
        }

        private static bool ShouldSkipCityForBulkStageToggle(CityConfig city)
        {
            return city == null || string.Equals(city.cityName, "Countryside", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildCityFirstCompletionAdShownKey(CityConfig city)
        {
            if (city == null)
                return CityFirstCompletionAdShownPrefix + "Unknown";

            int firstStageIndex = -1;
            if (city.stages != null && city.stages.Length > 0 && city.stages[0] != null)
                firstStageIndex = city.stages[0].stageIndex;

            string cityToken = firstStageIndex >= 0
                ? firstStageIndex.ToString()
                : (string.IsNullOrWhiteSpace(city.cityName) ? city.name : city.cityName).Replace(" ", "_");

            return $"{CityFirstCompletionAdShownPrefix}{cityToken}";
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
