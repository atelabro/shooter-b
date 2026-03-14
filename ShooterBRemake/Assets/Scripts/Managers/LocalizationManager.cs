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
            if (PlayerPrefs.HasKey(Constants.PREFS_LANGUAGE))
            {
                int raw = PlayerPrefs.GetInt(Constants.PREFS_LANGUAGE, (int)Language.English);
                if (!Enum.IsDefined(typeof(Language), raw))
                    raw = (int)Language.English;

                CurrentLanguage = (Language)raw;
                return;
            }

            CurrentLanguage = GetDefaultLanguageFromSystem();
            PlayerPrefs.SetInt(Constants.PREFS_LANGUAGE, (int)CurrentLanguage);
            PlayerPrefs.Save();
        }

        private Language GetDefaultLanguageFromSystem()
        {
            string systemLanguageName = Application.systemLanguage.ToString();
            return string.Equals(systemLanguageName, "Macedonian", StringComparison.OrdinalIgnoreCase)
                ? Language.Macedonian
                : Language.English;
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
                ["settings.title"] = "SETTINGS",
                ["settings.master"] = "Master Sound",
                ["settings.music"] = "Music",
                ["settings.sfx"] = "SFX",
                ["settings.vibration"] = "Enable Vibration",
                ["settings.language"] = "Language",
                ["settings.credits_body"] = "This game is a remake of the original DuckOff, first created in 2014 for Android only. It was rebuilt with care, respect for the original, and a lot of passion for bringing it back in a new form.",
                ["language.english"] = "English",
                ["language.macedonian"] = "Macedonian",

                ["common.back"] = "Back",
                ["common.armory"] = "Armory",
                ["common.resume"] = "Resume",
                ["common.restart"] = "Restart",
                ["common.continue"] = "Continue",
                ["common.close"] = "Close",
                ["common.menu"] = "Menu",
                ["common.stage"] = "Stage",
                ["common.start"] = "Start",
                ["common.coins_suffix"] = "COINS",

                ["campaign.map.title"] = "D.U.C.K. OPERATIONS",

                ["campaign.pause.title"] = "Game Paused",
                ["campaign.gameover.title"] = "Game Over",
                ["campaign.gameover.final_score"] = "Final Score",
                ["campaign.gameover.score_format"] = "Score: {0}",
                ["campaign.gameover.high_format"] = "High: {0}",
                ["campaign.gameover.mode_format"] = "Mode: {0}",
                ["campaign.mode.campaign"] = "Campaign",
                ["campaign.stage_complete.title"] = "Stage Complete",
                ["campaign.wave.label"] = "Wave {0}",

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
                ["campaign.starting.start"] = "Start",
                ["campaign.starting.plus_lives"] = "+2 Lives",
                ["campaign.starting.plus_bullets"] = "+ Bullets",
                ["campaign.starting.mercy_prompt"] = "Watch ad for rescue bonus",
                ["campaign.starting.mercy_granted"] = "Rescue bonus granted: +2 Lives",
                ["ads.status.completed"] = "Reward granted.",
                ["ads.status.canceled"] = "Ad canceled.",
                ["ads.status.failed"] = "Ad failed. Try again.",
                ["ads.status.unavailable"] = "Ad unavailable. Try again.",

                ["achievements.scene.title"] = "ACHIEVEMENTS",
                ["achievements.scene.general"] = "GENERAL",
                ["achievements.scene.daily_header_base"] = "DAILY AWARDS",
                ["achievements.scene.daily_header_claimed"] = "DAILY AWARDS (CLAIMED)",
                ["achievements.daily_ad_bonus.get_format"] = "Get +{0} coins",
                ["achievements.daily_ad_bonus.claimed"] = "CLAIMED",
                ["achievements.status.locked"] = "LOCKED",

                ["armory.scene.title"] = "ARMORY",
                ["armory.card.fire_format"] = "Fire: {0}",
                ["armory.card.rate_format"] = "Rate: {0}",
                ["armory.card.reload_format"] = "Reload: {0}",
                ["armory.card.travel_format"] = "Travel: {0}",
                ["armory.card.bullets_format"] = "Bullets: {0}",
                ["armory.card.aoe_format"] = "AoE: {0}",
                ["armory.card.locked_cost_format"] = "{0} {1}",
                ["armory.card.state.starter"] = "Starter",
                ["armory.card.state.unlocked"] = "Unlocked",
                ["armory.card.unlock_action"] = "View Unlock",
                ["armory.card.selected"] = "SELECTED",
                ["armory.fire_mode.single_tap"] = "Single Tap",
                ["armory.fire_mode.automatic"] = "Automatic",
                ["armory.modal.title_fallback"] = "Weapon",
                ["armory.modal.cost_format"] = "Cost: {0} {1}",
                ["armory.modal.status.have_format"] = "You have {0} {1}.",
                ["armory.modal.status.missing_format"] = "Missing {0} {1}.",
                ["armory.modal.status.already_unlocked"] = "This weapon is already unlocked.",
                ["armory.modal.button.unlock"] = "Unlock",
                ["armory.modal.button.close"] = "Close",
                ["armory.weapon.description.rifle"] = "Hunters know this classic. Precise and dependable.",
                ["armory.weapon.description.cabirne"] = "Sharper shots for cleaner picks in tight moments.",
                ["armory.weapon.description.beretta"] = "World-war steel turned into close-range spray control.",
                ["armory.weapon.description.laser"] = "High-tech beam bursts with broad impact coverage.",
                ["armory.weapon.description.piranha"] = "Load piranhas into the launcher and cause pure chaos.",
                ["armory.weapon.description.tesla"] = "Fires electric shots that chain lightning across nearby ducks.",
                ["armory.weapon.description.mrsulko"] = "A biochemical monster made for relentless pressure.",

                ["reward.header.default"] = "REWARD",
                ["reward.title.default"] = "Objective",
                ["reward.coins_suffix"] = "COINS",

                ["weapon.name.rifle"] = "Rifle",
                ["weapon.name.cabirne"] = "Cabirne",
                ["weapon.name.beretta"] = "Beretta",
                ["weapon.name.mrsulko"] = "MrSulko",
                ["weapon.name.laser"] = "Laser Gun",
                ["weapon.name.tesla"] = "Tesla Gun",
                ["weapon.name.piranha"] = "Piranha Gun",

                ["duck.name.mk_vojvoda"] = "MK Vojvoda",
                ["duck.name.mk_archer"] = "MK Archer",
                ["duck.name.mk_phalarx"] = "MK Phalarx",

                ["achievement.combo_label.double"] = "Double Kills",
                ["achievement.combo_label.triple"] = "Triple Kills",
                ["achievement.combo_label.quadra"] = "Quadra Kills",
                ["achievement.combo_label.triple_or_better"] = "Triple+ Kills",
                ["achievement.description.combo_exact"] = "Get {0} {1}.",
                ["achievement.description.combo_exact_with_weapon"] = "Get {0} {1} with {2}.",
                ["achievement.description.combo_min"] = "Get {0} {1}.",
                ["achievement.description.combo_min_with_weapon"] = "Get {0} {1} with {2}.",
                ["achievement.description.bird_kill"] = "Kill {0} ducks.",
                ["achievement.description.bird_kill_with_weapon"] = "Kill {0} ducks with {1}.",
                ["achievement.description.bird_kill_exact_duck"] = "Kill {0} {1} ducks.",
                ["achievement.description.bird_kill_elite"] = "Kill {0} elite ducks.",
                ["achievement.description.bird_kill_city_group"] = "Kill {0} {1} ducks.",

                ["achievement.city_group.skopje"] = "Macedonian",
                ["achievement.city_group.paris"] = "French",
                ["achievement.city_group.london"] = "British",
                ["achievement.city_group.newyork"] = "New York",
                ["achievement.city_group.tokyo"] = "Japanese",

                ["achievement.FieldPromotion.title"] = "Field Promotion",
                ["achievement.CityCleaner.title"] = "City Cleaner",
                ["achievement.ResistanceCracker.title"] = "Resistance Cracker",
                ["achievement.UprisingBreaker.title"] = "Uprising Breaker",
                ["achievement.NoFlyZone.title"] = "No-Fly Zone",
                ["achievement.ExterminatorGeneral.title"] = "Exterminator General",
                ["achievement.EliteControlI.title"] = "Elite Control I",
                ["achievement.EliteControlII.title"] = "Elite Control II",
                ["achievement.BossSlayerI.title"] = "Boss Slayer I",
                ["achievement.BossSlayerII.title"] = "Boss Slayer II",
                ["achievement.BossSlayerIII.title"] = "Boss Slayer III",
                ["achievement.SkopjeDefenderI.title"] = "Skopje Defender I",
                ["achievement.SkopjeDefenderII.title"] = "Skopje Defender II",
                ["achievement.ParisianHunterI.title"] = "Parisian Hunter I",
                ["achievement.ParisianHunterII.title"] = "Parisian Hunter II",
                ["achievement.LondonPatrolI.title"] = "London Patrol I",
                ["achievement.LondonPatrolII.title"] = "London Patrol II",
                ["achievement.NewYorkCleanupI.title"] = "New York Cleanup I",
                ["achievement.NewYorkCleanupII.title"] = "New York Cleanup II",
                ["achievement.TokyoOperationI.title"] = "Tokyo Operation I",
                ["achievement.TokyoOperationII.title"] = "Tokyo Operation II",
                ["achievement.RifleVeteran.title"] = "Rifle Veteran",
                ["achievement.CabirnePrecision.title"] = "Cabirne Precision",
                ["achievement.BerettaStorm.title"] = "Beretta Storm",
                ["achievement.SulkoRampage.title"] = "Sulko Rampage",
                ["achievement.LaserSweep.title"] = "Laser Sweep",
                ["achievement.TeslaChainLord.title"] = "Tesla Chain Lord",
                ["achievement.PiranhaMassacre.title"] = "Piranha Massacre",
                ["achievement.DoubleDownI.title"] = "Double Down I",
                ["achievement.DoubleDownII.title"] = "Double Down II",
                ["achievement.TripleThreatI.title"] = "Triple Threat I",
                ["achievement.TripleThreatII.title"] = "Triple Threat II",
                ["achievement.OverkillI.title"] = "Overkill I",
                ["achievement.OverkillII.title"] = "Overkill II",
                ["achievement.SkopjeBossHunter.title"] = "Skopje Boss Hunter",
                ["achievement.ParisBossHunter.title"] = "Paris Boss Hunter",
                ["achievement.LondonBossHunter.title"] = "London Boss Hunter",
                ["achievement.NewYorkBossHunter.title"] = "New York Boss Hunter",
                ["achievement.TokyoBossHunter.title"] = "Tokyo Boss Hunter",

                ["daily.Kill40.title"] = "Daily Hunter",
                ["daily.Kill80.title"] = "Feather Reaper",
                ["daily.Kill120.title"] = "Aerial Purge",
                ["daily.Combo8.title"] = "Combo Builder",
                ["daily.Combo16.title"] = "Combo Addict",
                ["daily.TripleOrBetter4.title"] = "Triple Pressure",
                ["daily.TripleOrBetter8.title"] = "Triple Execution",
                ["daily.Elite8.title"] = "Elite Patrol",
                ["daily.Elite15.title"] = "Elite Sweep",
                ["daily.Boss5.title"] = "Boss Hunting",
                ["daily.Complete2.title"] = "Frontline Duty",
                ["daily.Complete4.title"] = "Endurance Duty",

                ["daily.description.bird_kills"] = "Kill {0} ducks.",
                ["daily.description.combo_any"] = "Trigger {0} combos.",
                ["daily.description.combo_triple_or_better"] = "Get {0} triple-or-better combos.",
                ["daily.description.elite_kills"] = "Kill {0} elite ducks.",
                ["daily.description.boss_kills"] = "Kill {0} boss ducks.",
                ["daily.description.stage_completed"] = "Complete {0} campaign stages.",
                ["daily.description.stage_completed_singular"] = "Complete {0} campaign stage.",
                ["daily.description.game_completed"] = "Finish {0} games.",
                ["daily.description.game_completed_singular"] = "Finish {0} game."
            };

            tables[Language.Macedonian] = new Dictionary<string, string>
            {
                ["menu.title"] = "ДУКОФ",
                ["menu.campaign"] = "КАМПАЊА",
                ["menu.armory"] = "ОРУЖЈЕ",
                ["menu.achievements"] = "ДОСТИГНУВАЊА",
                ["menu.quit"] = "ИЗЛЕЗ",
                ["menu.high_score"] = "Најдобар резултат",
                ["settings.title"] = "ПОСТАВКИ",
                ["settings.master"] = "Аудио",
                ["settings.music"] = "Музика",
                ["settings.sfx"] = "ЗВУЦИ",
                ["settings.vibration"] = "Овозможи вибрации",
                ["settings.language"] = "Јазик",
                ["settings.credits_body"] = "Оваа игра е преработка на оригиналната DuckOff, првпат создадена во 2014 година само за Android. Преработката е направена со грижа, почит кон оригиналот и многу страст за да се врати во нова подобра форма.",
                ["language.english"] = "Англиски",
                ["language.macedonian"] = "Македонски",

                ["common.back"] = "Назад",
                ["common.armory"] = "Оружје",
                ["common.resume"] = "Продолжи",
                ["common.restart"] = "Рестарт",
                ["common.continue"] = "Продолжи",
                ["common.close"] = "Затвори",
                ["common.menu"] = "Мени",
                ["common.stage"] = "Ниво",
                ["common.start"] = "Почеток",
                ["common.coins_suffix"] = "ЗЛАТНИЦИ",

                ["campaign.map.title"] = "ОПЕРАЦИИ D.U.C.K.",

                ["campaign.pause.title"] = "Играта е паузирана",
                ["campaign.gameover.title"] = "Крај на играта",
                ["campaign.gameover.final_score"] = "Краен резултат",
                ["campaign.gameover.score_format"] = "Резултат: {0}",
                ["campaign.gameover.high_format"] = "Најдобар: {0}",
                ["campaign.gameover.mode_format"] = "Режим: {0}",
                ["campaign.mode.campaign"] = "Кампања",
                ["campaign.stage_complete.title"] = "Ниво завршено",
                ["campaign.wave.label"] = "Бран {0}",

                ["campaign.hud.score_format"] = "Резултат: {0}",
                ["campaign.hud.lives_format"] = "Животи: {0}",
                ["campaign.hud.reloading"] = "Репетирам",
                ["campaign.hud.combo.double"] = "ДВОЈНО УБИСТВО",
                ["campaign.hud.combo.triple"] = "ТРОЈНО УБИСТВО",
                ["campaign.hud.combo.quadra"] = "ЧЕТВОРНО УБИСТВО",
                ["campaign.hud.combo.default"] = "КОМБО",
                ["campaign.hud.popup.achievement_unlocked"] = "ОТКЛУЧЕНО ДОСТИГНУВАЊЕ",
                ["campaign.hud.popup.daily_objective_complete"] = "ДНЕВНА ЦЕЛ ЗАВРШЕНА",
                ["campaign.hud.popup.daily_set_complete"] = "ДНЕВЕН СЕТ ЗАВРШЕН",
                ["campaign.hud.popup.daily_set_body"] = "Сите дневни цели се завршени",

                ["campaign.starting.default_stage"] = "Ниво",
                ["campaign.starting.start"] = "Почеток",
                ["campaign.starting.plus_lives"] = "+2 Животи",
                ["campaign.starting.plus_bullets"] = "+ Куршуми",
                ["campaign.starting.mercy_prompt"] = "Гледај реклама за спас бонус",
                ["campaign.starting.mercy_granted"] = "Спас бонус доделен: +2 животи",
                ["ads.status.completed"] = "Наградата е доделена.",
                ["ads.status.canceled"] = "Рекламата е прекината.",
                ["ads.status.failed"] = "Рекламата не успеа. Обиди се пак.",
                ["ads.status.unavailable"] = "Реклама не е достапна. Обиди се пак.",

                ["achievements.scene.title"] = "ДОСТИГНУВАЊА",
                ["achievements.scene.general"] = "ОПШТО",
                ["achievements.scene.daily_header_base"] = "ДНЕВНИ",
                ["achievements.scene.daily_header_claimed"] = "ДНЕВНИ (ГОТОВО)",
                ["achievements.daily_ad_bonus.get_format"] = "Земи +{0} златници",
                ["achievements.daily_ad_bonus.claimed"] = "ЗЕМЕНО",
                ["achievements.status.locked"] = "ЗАКЛУЧЕНО",

                ["armory.scene.title"] = "ОРУЖЈЕ",
                ["armory.card.fire_format"] = "Пукање: {0}",
                ["armory.card.rate_format"] = "Брзина: {0}",
                ["armory.card.reload_format"] = "Полнење: {0}",
                ["armory.card.travel_format"] = "Дострел: {0}",
                ["armory.card.bullets_format"] = "Куршуми: {0}",
                ["armory.card.aoe_format"] = "Опсег: {0}",
                ["armory.card.locked_cost_format"] = "{0} {1}",
                ["armory.card.state.starter"] = "Почетно",
                ["armory.card.state.unlocked"] = "Отклучено",
                ["armory.card.unlock_action"] = "Отклучи",
                ["armory.card.selected"] = "ИЗБРАНО",
                ["armory.fire_mode.single_tap"] = "Еден истрел",
                ["armory.fire_mode.automatic"] = "Автоматско",
                ["armory.modal.title_fallback"] = "Оружје",
                ["armory.modal.cost_format"] = "Цена: {0} {1}",
                ["armory.modal.status.have_format"] = "Имаш {0} {1}.",
                ["armory.modal.status.missing_format"] = "Недостасуваат {0} {1}.",
                ["armory.modal.status.already_unlocked"] = "Ова оружје е веќе отклучено.",
                ["armory.modal.button.unlock"] = "Отклучи",
                ["armory.modal.button.close"] = "Затвори",
                ["armory.weapon.description.rifle"] = "Ловците ја знаат оваа класика. Прецизна и сигурна.",
                ["armory.weapon.description.cabirne"] = "Поостри истрели за почисти погодоци во тесни ситуации.",
                ["armory.weapon.description.beretta"] = "Старо воено железо претворено во блиска рафална контрола.",
                ["armory.weapon.description.laser"] = "Високотехнолошки ласерски истрели со широк опфат.",
                ["armory.weapon.description.piranha"] = "Наполни пирани во фрлачот и направи чист хаос.",
                ["armory.weapon.description.tesla"] = "Испукува електрични истрели што се префрлаат меѓу блиски патки.",
                ["armory.weapon.description.mrsulko"] = "Биохемиски ѕвер за постојан притисок.",

                ["reward.header.default"] = "НАГРАДА",
                ["reward.title.default"] = "Цел",
                ["reward.coins_suffix"] = "ЗЛАТНИЦИ",

                ["weapon.name.rifle"] = "Пушка",
                ["weapon.name.cabirne"] = "Кабирне",
                ["weapon.name.beretta"] = "Берета",
                ["weapon.name.mrsulko"] = "МрСулко",
                ["weapon.name.laser"] = "Ласер Пушка",
                ["weapon.name.tesla"] = "Тесла Пушка",
                ["weapon.name.piranha"] = "Пирана Пушка",

                ["duck.name.mk_vojvoda"] = "МК Војвода",
                ["duck.name.mk_archer"] = "МК Стрелец",
                ["duck.name.mk_phalarx"] = "МК Фаларкс",

                ["achievement.combo_label.double"] = "Двојни убиства",
                ["achievement.combo_label.triple"] = "Тројни убиства",
                ["achievement.combo_label.quadra"] = "Четворни убиства",
                ["achievement.combo_label.triple_or_better"] = "Тројни+ убиства",
                ["achievement.description.combo_exact"] = "Направи {0} {1}.",
                ["achievement.description.combo_exact_with_weapon"] = "Направи {0} {1} со {2}.",
                ["achievement.description.combo_min"] = "Направи {0} {1}.",
                ["achievement.description.combo_min_with_weapon"] = "Направи {0} {1} со {2}.",
                ["achievement.description.bird_kill"] = "Убиј {0} патки.",
                ["achievement.description.bird_kill_with_weapon"] = "Убиј {0} патки со {1}.",
                ["achievement.description.bird_kill_exact_duck"] = "Убиј {0} патки од тип {1}.",
                ["achievement.description.bird_kill_elite"] = "Убиј {0} елитни патки.",
                ["achievement.description.bird_kill_city_group"] = "Убиј {0} {1} патки.",

                ["achievement.city_group.skopje"] = "Македонски",
                ["achievement.city_group.paris"] = "Француски",
                ["achievement.city_group.london"] = "Британски",
                ["achievement.city_group.newyork"] = "Њујоршки",
                ["achievement.city_group.tokyo"] = "Јапонски",

                ["achievement.FieldPromotion.title"] = "Теренска Промоција",
                ["achievement.CityCleaner.title"] = "Чистач на Градови",
                ["achievement.ResistanceCracker.title"] = "Кршач на Отпор",
                ["achievement.UprisingBreaker.title"] = "Кршач на Востание",
                ["achievement.NoFlyZone.title"] = "Зона Без Летање",
                ["achievement.ExterminatorGeneral.title"] = "Генерал Истребувач",
                ["achievement.EliteControlI.title"] = "Елитна Контрола I",
                ["achievement.EliteControlII.title"] = "Елитна Контрола II",
                ["achievement.BossSlayerI.title"] = "Убиец на Шефови I",
                ["achievement.BossSlayerII.title"] = "Убиец на Шефови II",
                ["achievement.BossSlayerIII.title"] = "Убиец на Шефови III",
                ["achievement.SkopjeDefenderI.title"] = "Бранител на Скопје I",
                ["achievement.SkopjeDefenderII.title"] = "Бранител на Скопје II",
                ["achievement.ParisianHunterI.title"] = "Паризиски Ловец I",
                ["achievement.ParisianHunterII.title"] = "Паризиски Ловец II",
                ["achievement.LondonPatrolI.title"] = "Лондонска Патрола I",
                ["achievement.LondonPatrolII.title"] = "Лондонска Патрола II",
                ["achievement.NewYorkCleanupI.title"] = "Чистење на Њујорк I",
                ["achievement.NewYorkCleanupII.title"] = "Чистење на Њујорк II",
                ["achievement.TokyoOperationI.title"] = "Токиска Операција I",
                ["achievement.TokyoOperationII.title"] = "Токиска Операција II",
                ["achievement.RifleVeteran.title"] = "Ветеран со Пушка",
                ["achievement.CabirnePrecision.title"] = "Кабирне Прецизност",
                ["achievement.BerettaStorm.title"] = "Берета Бура",
                ["achievement.SulkoRampage.title"] = "Сулко Збес",
                ["achievement.LaserSweep.title"] = "Ласерско Чистење",
                ["achievement.TeslaChainLord.title"] = "Господар на Тесла Синџир",
                ["achievement.PiranhaMassacre.title"] = "Пирана Масакр",
                ["achievement.DoubleDownI.title"] = "Двоен Удар I",
                ["achievement.DoubleDownII.title"] = "Двоен Удар II",
                ["achievement.TripleThreatI.title"] = "Тројна Закана I",
                ["achievement.TripleThreatII.title"] = "Тројна Закана II",
                ["achievement.OverkillI.title"] = "Оверкил I",
                ["achievement.OverkillII.title"] = "Оверкил II",
                ["achievement.SkopjeBossHunter.title"] = "Ловец на Шефот од Скопје",
                ["achievement.ParisBossHunter.title"] = "Ловец на Шефот од Париз",
                ["achievement.LondonBossHunter.title"] = "Ловец на Шефот од Лондон",
                ["achievement.NewYorkBossHunter.title"] = "Ловец на Шефот од Њујорк",
                ["achievement.TokyoBossHunter.title"] = "Ловец на Шефот од Токио",

                ["daily.Kill40.title"] = "Дневен Ловец",
                ["daily.Kill80.title"] = "Жетвар на Пердуви",
                ["daily.Kill120.title"] = "Воздушна Чистка",
                ["daily.Combo8.title"] = "Комбо Градител",
                ["daily.Combo16.title"] = "Комбо Зависник",
                ["daily.TripleOrBetter4.title"] = "Троен Притисок",
                ["daily.TripleOrBetter8.title"] = "Тројна Егзекуција",
                ["daily.Elite8.title"] = "Елитна Патрола",
                ["daily.Elite15.title"] = "Елитно Чистење",
                ["daily.Boss5.title"] = "Лов на Шефови",
                ["daily.Complete2.title"] = "Фронтова Должност",
                ["daily.Complete4.title"] = "Издржливост",

                ["daily.description.bird_kills"] = "Убиј {0} патки.",
                ["daily.description.combo_any"] = "Направи {0} комбоа.",
                ["daily.description.combo_triple_or_better"] = "Направи {0} тројни или подобри комбоа.",
                ["daily.description.elite_kills"] = "Убиј {0} елитни патки.",
                ["daily.description.boss_kills"] = "Убиј {0} шеф-патки.",
                ["daily.description.stage_completed"] = "Заврши {0} кампањски нивоа.",
                ["daily.description.stage_completed_singular"] = "Заврши {0} кампањско ниво.",
                ["daily.description.game_completed"] = "Заврши {0} игри.",
                ["daily.description.game_completed_singular"] = "Заврши {0} игра."
            };
        }
    }
}
