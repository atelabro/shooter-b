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
                ["menu.high_score"] = "High Score",

                ["common.back"] = "Back",
                ["common.armory"] = "Armory",
                ["common.resume"] = "Resume",
                ["common.restart"] = "Restart",
                ["common.continue"] = "Continue",
                ["common.menu"] = "Menu",
                ["common.stage"] = "Stage",
                ["common.start"] = "Start",

                ["campaign.map.title"] = "D.U.C.K. OPERATIONS",

                ["campaign.pause.title"] = "Game Paused",
                ["campaign.gameover.title"] = "Game Over",
                ["campaign.gameover.final_score"] = "Final Score",
                ["campaign.gameover.score_format"] = "Score: {0}",
                ["campaign.gameover.high_format"] = "High: {0}",
                ["campaign.gameover.mode_format"] = "Mode: {0}",
                ["campaign.mode.campaign"] = "Campaign",
                ["campaign.stage_complete.title"] = "Stage Complete",

                ["campaign.hud.score_format"] = "Score: {0}",
                ["campaign.hud.lives_format"] = "Lives: {0}",
                ["campaign.hud.reloading"] = "Reloading",
                ["campaign.hud.combo.double"] = "DOUBLE KILL",
                ["campaign.hud.combo.triple"] = "TRIPLE KILL",
                ["campaign.hud.combo.quadra"] = "QUADRA KILL",
                ["campaign.hud.combo.default"] = "COMBO",
                ["campaign.hud.popup.achievement_unlocked"] = "ACHIEVEMENT UNLOCKED",
                ["campaign.hud.popup.daily_objective_complete"] = "DAILY OBJECTIVE COMPLETE",
                ["campaign.hud.popup.daily_set_complete"] = "DAILY SET COMPLETE",
                ["campaign.hud.popup.daily_set_body"] = "All daily objectives completed",

                ["campaign.starting.default_stage"] = "Stage",
                ["campaign.starting.start"] = "Start"
            };

            tables[Language.Macedonian] = new Dictionary<string, string>
            {
                ["menu.title"] = "ДУКОФ",
                ["menu.campaign"] = "КАМПАЊА",
                ["menu.armory"] = "ОРУЖЈЕ",
                ["menu.achievements"] = "ДОСТИГНУВАЊА",
                ["menu.quit"] = "ИЗЛЕЗ",
                ["menu.high_score"] = "Најдобар резултат",

                ["common.back"] = "Назад",
                ["common.armory"] = "Оружје",
                ["common.resume"] = "Продолжи",
                ["common.restart"] = "Рестарт",
                ["common.continue"] = "Продолжи",
                ["common.menu"] = "Мени",
                ["common.stage"] = "Ниво",
                ["common.start"] = "Почеток",

                ["campaign.map.title"] = "ОПЕРАЦИИ D.U.C.K.",

                ["campaign.pause.title"] = "Играта е паузирана",
                ["campaign.gameover.title"] = "Крај на играта",
                ["campaign.gameover.final_score"] = "Краен резултат",
                ["campaign.gameover.score_format"] = "Резултат: {0}",
                ["campaign.gameover.high_format"] = "Најдобар: {0}",
                ["campaign.gameover.mode_format"] = "Режим: {0}",
                ["campaign.mode.campaign"] = "Кампања",
                ["campaign.stage_complete.title"] = "Ниво завршено",

                ["campaign.hud.score_format"] = "Резултат: {0}",
                ["campaign.hud.lives_format"] = "Животи: {0}",
                ["campaign.hud.reloading"] = "Се полни",
                ["campaign.hud.combo.double"] = "ДВОЈНО УБИСТВО",
                ["campaign.hud.combo.triple"] = "ТРОЈНО УБИСТВО",
                ["campaign.hud.combo.quadra"] = "ЧЕТВОРНО УБИСТВО",
                ["campaign.hud.combo.default"] = "КОМБО",
                ["campaign.hud.popup.achievement_unlocked"] = "ОТКЛУЧЕНО ДОСТИГНУВАЊЕ",
                ["campaign.hud.popup.daily_objective_complete"] = "ДНЕВНА ЦЕЛ ЗАВРШЕНА",
                ["campaign.hud.popup.daily_set_complete"] = "ДНЕВЕН СЕТ ЗАВРШЕН",
                ["campaign.hud.popup.daily_set_body"] = "Сите дневни цели се завршени",

                ["campaign.starting.default_stage"] = "Ниво",
                ["campaign.starting.start"] = "Почеток"
            };
        }
    }
}
