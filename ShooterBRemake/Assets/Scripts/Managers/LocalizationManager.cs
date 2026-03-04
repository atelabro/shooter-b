using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public class LocalizationManager : MonoBehaviour
    {
        public enum Language
        {
            English = 0,
            Macedonian = 1
        }

        private static LocalizationManager instance;
        public static bool HasInstance => instance != null;
        public static LocalizationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("LocalizationManager");
                    instance = go.AddComponent<LocalizationManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public event Action<Language> OnLanguageChanged;

        public Language CurrentLanguage { get; private set; } = Language.English;

        private readonly Dictionary<Language, Dictionary<string, string>> tables =
            new Dictionary<Language, Dictionary<string, string>>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeTables();
            LoadLanguagePreference();
        }

        public void SetLanguage(Language language)
        {
            if (CurrentLanguage == language)
                return;

            CurrentLanguage = language;
            PlayerPrefs.SetInt(Constants.PREFS_LANGUAGE, (int)CurrentLanguage);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke(CurrentLanguage);
        }

        public string Get(string key, string fallback = null)
        {
            if (TryGet(CurrentLanguage, key, out string value))
                return value;

            if (CurrentLanguage != Language.English && TryGet(Language.English, key, out value))
                return value;

            return string.IsNullOrWhiteSpace(fallback) ? key : fallback;
        }

        public static string GetLanguageCode(Language language)
        {
            return language == Language.Macedonian ? "MK" : "EN";
        }

        private bool TryGet(Language language, string key, out string value)
        {
            value = null;

            if (!tables.TryGetValue(language, out Dictionary<string, string> map))
                return false;

            return map.TryGetValue(key, out value);
        }

        private void LoadLanguagePreference()
        {
            int raw = PlayerPrefs.GetInt(Constants.PREFS_LANGUAGE, (int)Language.English);
            if (!Enum.IsDefined(typeof(Language), raw))
                raw = (int)Language.English;

            CurrentLanguage = (Language)raw;
        }

        private void InitializeTables()
        {
            tables.Clear();

            tables[Language.English] = new Dictionary<string, string>
            {
                ["menu.title"] = "DUCKOFF",
                ["menu.campaign"] = "CAMPAIGN",
                ["menu.armory"] = "ARMORY",
                ["menu.achievements"] = "ACHIEVEMENTS",
                ["menu.quit"] = "QUIT",
                ["menu.high_score"] = "High Score"
            };

            tables[Language.Macedonian] = new Dictionary<string, string>
            {
                ["menu.title"] = "ДУКОФ",
                ["menu.campaign"] = "КАМПАЊА",
                ["menu.armory"] = "ОРУЖЈЕ",
                ["menu.achievements"] = "ДОСТИГНУВАЊА",
                ["menu.quit"] = "ИЗЛЕЗ",
                ["menu.high_score"] = "Најдобар резултат"
            };
        }
    }
}
