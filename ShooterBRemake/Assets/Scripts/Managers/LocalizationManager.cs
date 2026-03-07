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

                ["achievements.scene.title"] = "ACHIEVEMENTS",
                ["achievements.scene.general"] = "GENERAL",
                ["achievements.scene.daily_header_base"] = "DAILY AWARDS",
                ["achievements.scene.daily_header_claimed"] = "DAILY AWARDS (CLAIMED)",
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

                ["achievement.BirdBlenderI.title"] = "Bird Blender I",
                ["achievement.BirdBlenderII.title"] = "Bird Blender II",
                ["achievement.BossSlayer.title"] = "Boss Slayer",
                ["achievement.CommanderDown.title"] = "Commander Down",
                ["achievement.DuckHunterI.title"] = "Duck Hunter I",
                ["achievement.DuckHunterII.title"] = "Duck Hunter II",
                ["achievement.DuckHunterIII.title"] = "Duck Hunter III",
                ["achievement.DuckHunterIV.title"] = "Duck Hunter IV",
                ["achievement.EliteControlI.title"] = "Elite Control I",
                ["achievement.EliteControlII.title"] = "Elite Control II",
                ["achievement.ArcherCleanup.title"] = "Archer Cleanup",
                ["achievement.LaserSweep.title"] = "Laser Sweep",
                ["achievement.OverkillI.title"] = "Overkill I",
                ["achievement.OverkillII.title"] = "Overkill II",
                ["achievement.PhalarxBreaker.title"] = "Phalarx Breaker",
                ["achievement.PiranhaDoubleTrouble.title"] = "Piranha Double Trouble",
                ["achievement.PiranhaMassacre.title"] = "Piranha Massacre",
                ["achievement.PiranhaDoubleKill50.title"] = "Predator School",
                ["achievement.RifleVeteran.title"] = "Rifle Veteran",
                ["achievement.SniperPrecision.title"] = "Sniper Precision",
                ["achievement.SulkoRampage.title"] = "Sulko Rampage",
                ["achievement.TeslaChainLord.title"] = "Tesla Chain Lord",
                ["achievement.TripleThreatI.title"] = "Triple Threat I",
                ["achievement.TripleThreatII.title"] = "Triple Threat II",
                ["achievement.BerettaSpray.title"] = "Beretta Spray",

                ["daily.Kill15.title"] = "Daily Hunter",
                ["daily.Kill30.title"] = "Feather Collector",
                ["daily.Kill50.title"] = "Bird Purge",
                ["daily.Combo3.title"] = "Combo Starter",
                ["daily.Combo6.title"] = "Combo Builder",
                ["daily.TripleOrBetter2.title"] = "Triple Pressure",
                ["daily.Elite3.title"] = "Elite Patrol",
                ["daily.Elite6.title"] = "Elite Sweep",
                ["daily.Finish1.title"] = "One More Round",
                ["daily.Finish2.title"] = "Persistent Hunter",

                ["daily.description.bird_kills"] = "Kill {0} ducks.",
                ["daily.description.combo_any"] = "Trigger {0} combos.",
                ["daily.description.combo_triple_or_better"] = "Get {0} triple-or-better combos.",
                ["daily.description.elite_kills"] = "Kill {0} elite ducks.",
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

                ["common.back"] = "Назад",
                ["common.armory"] = "Оружје",
                ["common.resume"] = "Продолжи",
                ["common.restart"] = "Рестарт",
                ["common.continue"] = "Продолжи",
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

                ["achievements.scene.title"] = "ДОСТИГНУВАЊА",
                ["achievements.scene.general"] = "ОПШТО",
                ["achievements.scene.daily_header_base"] = "ДНЕВНИ",
                ["achievements.scene.daily_header_claimed"] = "ДНЕВНИ (ГОТОВО)",
                ["achievements.status.locked"] = "ЗАКЛУЧЕНО",

                ["armory.scene.title"] = "ОРУЖЈЕ",
                ["armory.card.fire_format"] = "Пукање: {0}",
                ["armory.card.rate_format"] = "Брзина: {0}",
                ["armory.card.reload_format"] = "Полнење: {0}",
                ["armory.card.travel_format"] = "Патување: {0}",
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

                ["achievement.BirdBlenderI.title"] = "Птичји Миксер I",
                ["achievement.BirdBlenderII.title"] = "Птичји Миксер II",
                ["achievement.BossSlayer.title"] = "Убиец на Шефови",
                ["achievement.CommanderDown.title"] = "Командантот Падна",
                ["achievement.DuckHunterI.title"] = "Ловец на Патки I",
                ["achievement.DuckHunterII.title"] = "Ловец на Патки II",
                ["achievement.DuckHunterIII.title"] = "Ловец на Патки III",
                ["achievement.DuckHunterIV.title"] = "Ловец на Патки IV",
                ["achievement.EliteControlI.title"] = "Елитна Контрола I",
                ["achievement.EliteControlII.title"] = "Елитна Контрола II",
                ["achievement.ArcherCleanup.title"] = "Чистење на Стрелци",
                ["achievement.LaserSweep.title"] = "Ласерско Чистење",
                ["achievement.OverkillI.title"] = "Оверкил I",
                ["achievement.OverkillII.title"] = "Оверкил II",
                ["achievement.PhalarxBreaker.title"] = "Кршач на Фаларкс",
                ["achievement.PiranhaDoubleTrouble.title"] = "Пирана Двојна Невоља",
                ["achievement.PiranhaMassacre.title"] = "Пирана Масакр",
                ["achievement.PiranhaDoubleKill50.title"] = "Школа за Предатори",
                ["achievement.RifleVeteran.title"] = "Ветеран со Пушка",
                ["achievement.SniperPrecision.title"] = "Снајперска Прецизност",
                ["achievement.SulkoRampage.title"] = "Сулко Рампаж",
                ["achievement.TeslaChainLord.title"] = "Господар на Тесла Синџир",
                ["achievement.TripleThreatI.title"] = "Тројна Закана I",
                ["achievement.TripleThreatII.title"] = "Тројна Закана II",
                ["achievement.BerettaSpray.title"] = "Берета Спреј",

                ["daily.Kill15.title"] = "Дневен Ловец",
                ["daily.Kill30.title"] = "Колекционер на Пердуви",
                ["daily.Kill50.title"] = "Птичја Чистка",
                ["daily.Combo3.title"] = "Комбо Почетник",
                ["daily.Combo6.title"] = "Комбо Градител",
                ["daily.TripleOrBetter2.title"] = "Троен Притисок",
                ["daily.Elite3.title"] = "Елитна Патрола",
                ["daily.Elite6.title"] = "Елитно Чистење",
                ["daily.Finish1.title"] = "Уште Една Рунда",
                ["daily.Finish2.title"] = "Упорен Ловец",

                ["daily.description.bird_kills"] = "Убиј {0} патки.",
                ["daily.description.combo_any"] = "Направи {0} комбоа.",
                ["daily.description.combo_triple_or_better"] = "Направи {0} тројни или подобри комбоа.",
                ["daily.description.elite_kills"] = "Убиј {0} елитни патки.",
                ["daily.description.game_completed"] = "Заврши {0} игри.",
                ["daily.description.game_completed_singular"] = "Заврши {0} игра."
            };
        }
    }
}
