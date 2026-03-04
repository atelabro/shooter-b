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
            return LocalizationManager.Instance.CurrentLanguage == LocalizationManager.Language.Macedonian;
        }
    }
}
