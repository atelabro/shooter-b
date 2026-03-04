using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class StageCompleteModalController : MonoBehaviour
    {
        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Text")]
        public TextMeshProUGUI stageNameText;

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

        private void Start()
        {
            EnsureModalRoot();
            ResolveButtons();

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (legacyBackFallbackButton != null && legacyBackFallbackButton != backButton)
                legacyBackFallbackButton.onClick.AddListener(OnBackClicked);
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
        }

        public void Show(StageConfig config, long score)
        {
            EnsureModalRoot();

            int stars = CampaignProgressManager.Instance.CalculateStars(config, score);

            CampaignProgressManager.Instance.SaveStageStars(config.stageIndex, stars);

            if (stageNameText != null)
                stageNameText.text = config.mapName;

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

            StageConfig nextStage = CampaignProgressManager.Instance.GetNextStageInActiveCityRow();
            CityConfig activeCity = CampaignProgressManager.Instance.ActiveCityConfig;
            CityConfig[] allCities = CampaignProgressManager.Instance.CampaignCities;

            if (nextStage == null || activeCity == null || allCities == null)
            {
                SceneController.Instance.LoadCampaignMapScene();
                return;
            }

            if (!CampaignProgressManager.Instance.IsStageUnlocked(nextStage, activeCity, allCities))
            {
                SceneController.Instance.LoadCampaignMapScene();
                return;
            }

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
    }
}
