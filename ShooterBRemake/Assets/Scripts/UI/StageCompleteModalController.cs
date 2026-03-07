using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class StageCompleteModalController : MonoBehaviour
    {
        private const float ContinueMapTransitionMinDelaySeconds = 1f;

        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Text")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI stageNameText;
        public TextMeshProUGUI restartButtonText;
        public TextMeshProUGUI backButtonText;
        public TextMeshProUGUI continueButtonText;

        [Header("Star Icons")]
        public Image[] starIcons;
        public Sprite filledStarSprite;
        public Sprite emptyStarSprite;

        [Header("Buttons")]
        public Button restartButton;
        public Button backButton;
        public Button continueButton;
        public Button menuButton;
        private Button legacyBackFallbackButton;
        private StageConfig lastShownStage;

        private void Start()
        {
            EnsureModalRoot();
            ResolveButtons();
            ResolveTextReferences();
            RefreshLocalizedTexts();

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (legacyBackFallbackButton != null && legacyBackFallbackButton != backButton)
                legacyBackFallbackButton.onClick.AddListener(OnBackClicked);

            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);

            if (legacyBackFallbackButton != null && legacyBackFallbackButton != backButton)
                legacyBackFallbackButton.onClick.RemoveListener(OnBackClicked);

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        public void Show(StageConfig config, long score)
        {
            EnsureModalRoot();
            lastShownStage = config;

            int stars = CampaignProgressManager.Instance.CalculateStars(config, score);

            CampaignProgressManager.Instance.SaveStageStars(config.stageIndex, stars);

            if (stageNameText != null)
                stageNameText.text = CampaignLocalizationResolver.GetStageName(config);

            ApplyStarIcons(stars);

            if (modalRoot != null)
                modalRoot.SetActive(true);
        }

        private void ApplyStarIcons(int earnedStars)
        {
            if (starIcons == null)
                return;

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] == null)
                    continue;

                bool earned = i < earnedStars;
                starIcons[i].sprite = earned ? filledStarSprite : emptyStarSprite;
                starIcons[i].enabled = earned ? filledStarSprite != null : emptyStarSprite != null;
            }
        }

        public void Hide()
        {
            EnsureModalRoot();

            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        public void OnRestartClicked()
        {
            SceneController.Instance.ReloadCurrentGameScene();
        }

        public void OnBackClicked()
        {
            Time.timeScale = 1f;
            SceneController.Instance.LoadCampaignMapScene();
        }

        public void OnContinueClicked()
        {
            Time.timeScale = 1f;

            StageConfig activeStage = CampaignProgressManager.Instance.ActiveStageConfig;
            StageConfig nextStage = CampaignProgressManager.Instance.GetNextStageInActiveCityRow();
            CityConfig activeCity = CampaignProgressManager.Instance.ActiveCityConfig;
            CityConfig[] allCities = CampaignProgressManager.Instance.CampaignCities;

            if (activeCity == null || allCities == null)
            {
                CampaignProgressManager.Instance.ClearPendingMapFocusTransition();
                SceneController.Instance.LoadCampaignMapScene();
                return;
            }

            if (nextStage == null)
            {
                CityConfig nextCity = FindNextUnlockedCity(activeCity, allCities);
                if (nextCity != null && nextCity.stages != null && nextCity.stages.Length > 0 && nextCity.stages[0] != null && activeStage != null)
                {
                    CampaignProgressManager.Instance.SetPendingMapFocusTransition(
                        activeCity,
                        activeStage,
                        nextCity,
                        nextCity.stages[0],
                        ContinueMapTransitionMinDelaySeconds);
                }
                else
                {
                    CampaignProgressManager.Instance.ClearPendingMapFocusTransition();
                }

                SceneController.Instance.LoadCampaignMapScene();
                return;
            }

            if (!CampaignProgressManager.Instance.IsStageUnlocked(nextStage, activeCity, allCities))
            {
                CampaignProgressManager.Instance.ClearPendingMapFocusTransition();
                SceneController.Instance.LoadCampaignMapScene();
                return;
            }

            CampaignProgressManager.Instance.ClearPendingMapFocusTransition();
            CampaignProgressManager.Instance.SetActiveCampaignLocation(activeCity, nextStage);
            SceneController.Instance.LoadCampaignStage(nextStage);
        }

        public void OnMenuClicked()
        {
            OnBackClicked();
        }

        private void EnsureModalRoot()
        {
            if (modalRoot == null)
                modalRoot = gameObject;
        }

        private void ResolveButtons()
        {
            if (continueButton == null && menuButton != null && menuButton.gameObject.name.Contains("Continue"))
            {
                continueButton = menuButton;
                menuButton = null;
            }

            if (backButton == null)
                backButton = FindButtonByName("BackButton");

            if (continueButton == null)
                continueButton = FindButtonByName("ContinueButton");

            legacyBackFallbackButton = menuButton;
        }

        private void ResolveTextReferences()
        {
            if (titleText == null)
                titleText = FindTextByContent("Stage Complete");

            if (restartButtonText == null && restartButton != null)
                restartButtonText = restartButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (backButtonText == null && backButton != null)
                backButtonText = backButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (continueButtonText == null && continueButton != null)
                continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void RefreshLocalizedTexts()
        {
            if (titleText != null)
                titleText.text = LocalizationManager.Instance.Get("campaign.stage_complete.title", "Stage Complete");

            if (restartButtonText != null)
                restartButtonText.text = LocalizationManager.Instance.Get("common.restart", "Restart");

            if (backButtonText != null)
                backButtonText.text = LocalizationManager.Instance.Get("common.back", "Back");

            if (continueButtonText != null)
                continueButtonText.text = LocalizationManager.Instance.Get("common.continue", "Continue");
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedTexts();

            if (stageNameText != null && lastShownStage != null)
                stageNameText.text = CampaignLocalizationResolver.GetStageName(lastShownStage);
        }

        private TextMeshProUGUI FindTextByContent(string content)
        {
            if (modalRoot == null || string.IsNullOrWhiteSpace(content))
                return null;

            TextMeshProUGUI[] texts = modalRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].text == content)
                    return texts[i];
            }

            return null;
        }

        private Button FindButtonByName(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
                return null;

            if (modalRoot == null)
                return null;

            Button[] buttons = modalRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == buttonName)
                    return buttons[i];
            }

            return null;
        }

        private static CityConfig FindNextUnlockedCity(CityConfig activeCity, CityConfig[] allCities)
        {
            if (activeCity == null || allCities == null)
                return null;

            int activeCityIndex = System.Array.IndexOf(allCities, activeCity);
            if (activeCityIndex < 0)
                return null;

            for (int i = activeCityIndex + 1; i < allCities.Length; i++)
            {
                CityConfig candidateCity = allCities[i];
                if (candidateCity == null || candidateCity.stages == null || candidateCity.stages.Length == 0 || candidateCity.stages[0] == null)
                    continue;

                if (!CampaignProgressManager.Instance.IsCityUnlocked(candidateCity, allCities))
                    continue;

                if (!CampaignProgressManager.Instance.IsStageUnlocked(candidateCity.stages[0], candidateCity, allCities))
                    continue;

                return candidateCity;
            }

            return null;
        }
    }
}
