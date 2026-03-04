using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShooterB
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI Elements")]
        public Button campaignButton;
        public Button armoryButton;
        public Button achievementsButton;
        public Button quitButton;
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI campaignButtonText;
        public TextMeshProUGUI armoryButtonText;
        public TextMeshProUGUI achievementsButtonText;
        public TextMeshProUGUI quitButtonText;

        [Header("Localization")]
        public LanguageDropdownController languageDropdown;

        private void Start()
        {
            _ = LocalizationManager.Instance;

            if (campaignButton != null)
                campaignButton.onClick.AddListener(OnCampaignClicked);

            if (armoryButton != null)
                armoryButton.onClick.AddListener(OnArmoryClicked);

            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            ResolveMenuTextReferences();
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            InitializeLanguageDropdown();
            UpdateHighScore();
            RefreshLocalizedTexts();
        }

        private void OnDestroy()
        {
            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void OnCampaignClicked()
        {
            SceneController.Instance.LoadCampaignMapScene();
        }

        private void OnArmoryClicked()
        {
            SceneController.Instance.LoadArmoryScene();
        }

        private void OnAchievementsClicked()
        {
            SceneController.Instance.LoadAchievementsScene();
        }

        private void OnQuitClicked()
        {
            SceneController.Instance.QuitGame();
        }

        private void UpdateHighScore()
        {
            if (highScoreText != null)
            {
                int highScore = PlayerPrefs.GetInt(Constants.PREFS_HIGH_SCORE_ARCADE, 0);
                string label = LocalizationManager.Instance.Get("menu.high_score", "High Score");
                highScoreText.text = $"{label}: {highScore}";
            }
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedTexts();
        }

        private void RefreshLocalizedTexts()
        {
            if (titleText != null)
                titleText.text = LocalizationManager.Instance.Get("menu.title", "DUCKOFF");

            if (campaignButtonText != null)
                campaignButtonText.text = LocalizationManager.Instance.Get("menu.campaign", "CAMPAIGN");

            if (armoryButtonText != null)
                armoryButtonText.text = LocalizationManager.Instance.Get("menu.armory", "ARMORY");

            if (achievementsButtonText != null)
                achievementsButtonText.text = LocalizationManager.Instance.Get("menu.achievements", "ACHIEVEMENTS");

            if (quitButtonText != null)
                quitButtonText.text = LocalizationManager.Instance.Get("menu.quit", "QUIT");

            UpdateHighScore();
        }

        private void InitializeLanguageDropdown()
        {
            if (languageDropdown == null)
            {
                languageDropdown = FindObjectOfType<LanguageDropdownController>(true);
            }

            if (languageDropdown == null)
            {
                Debug.LogWarning("[MenuController] languageDropdown reference is missing.");
                return;
            }

            languageDropdown.Initialize();
        }

        private void ResolveMenuTextReferences()
        {
            if (campaignButtonText == null && campaignButton != null)
                campaignButtonText = FindText(campaignButton.transform, "CampainText");

            if (armoryButtonText == null && armoryButton != null)
                armoryButtonText = FindText(armoryButton.transform, "ArmoryText");

            if (achievementsButtonText == null && achievementsButton != null)
                achievementsButtonText = FindText(achievementsButton.transform, "AchievementsText");

            if (quitButtonText == null && quitButton != null)
                quitButtonText = FindText(quitButton.transform, "QuitButton");
        }

        private static TextMeshProUGUI FindText(Transform root, string preferredChildName)
        {
            if (root == null)
                return null;

            if (!string.IsNullOrWhiteSpace(preferredChildName))
            {
                Transform child = root.Find(preferredChildName);
                if (child != null)
                {
                    TextMeshProUGUI preferred = child.GetComponent<TextMeshProUGUI>();
                    if (preferred != null)
                        return preferred;
                }
            }

            return root.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
