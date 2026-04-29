using UnityEngine;

namespace ShooterB
{
    public static class CampaignLocalizationResolver
    {
        public static string GetCityName(CityConfig city)
        {
            if (city == null)
                return string.Empty;

            if (IsMacedonian() && !string.IsNullOrWhiteSpace(city.cityNameMk))
                return city.cityNameMk;

            return city.cityName ?? string.Empty;
        }

        public static string GetCityPinName(CityConfig city)
        {
            if (city == null)
                return string.Empty;

            string configuredName = IsMacedonian()
                ? city.pinDisplayNameMk
                : city.pinDisplayName;

            if (string.IsNullOrWhiteSpace(configuredName) && IsMacedonian())
                configuredName = city.pinDisplayName;

            if (!string.IsNullOrWhiteSpace(configuredName))
                return configuredName;

            string cityName = GetCityName(city);
            return city.forcePinNameTwoRows
                ? SplitLastSpace(cityName)
                : cityName;
        }

        private static string SplitLastSpace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            int splitIndex = value.Trim().LastIndexOf(' ');
            if (splitIndex <= 0 || splitIndex >= value.Length - 1)
                return value;

            return value.Substring(0, splitIndex) + "\n" + value.Substring(splitIndex + 1);
        }

        public static string GetCityBriefing(CityConfig city)
        {
            if (city == null)
                return string.Empty;

            if (IsMacedonian() && !string.IsNullOrWhiteSpace(city.briefingTextMk))
                return city.briefingTextMk;

            return city.briefingText ?? string.Empty;
        }

        public static string GetStageName(StageConfig stage)
        {
            if (stage == null)
                return string.Empty;

            if (IsMacedonian() && !string.IsNullOrWhiteSpace(stage.mapNameMk))
                return stage.mapNameMk;

            return stage.mapName ?? string.Empty;
        }

        public static string GetStageBriefing(StageConfig stage)
        {
            if (stage == null)
                return string.Empty;

            if (IsMacedonian() && !string.IsNullOrWhiteSpace(stage.briefingTextMk))
                return stage.briefingTextMk;

            return stage.briefingText ?? string.Empty;
        }

        private static bool IsMacedonian()
        {
            int savedLanguage = PlayerPrefs.GetInt(
                Constants.PREFS_LANGUAGE,
                (int)LocalizationManager.Language.English);
            bool prefsMacedonian = savedLanguage == (int)LocalizationManager.Language.Macedonian;

            bool managerMacedonian = LocalizationManager.HasInstance &&
                LocalizationManager.Instance.CurrentLanguage == LocalizationManager.Language.Macedonian;

            return prefsMacedonian || managerMacedonian;
        }
    }
}
