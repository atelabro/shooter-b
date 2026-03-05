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

        [Header("Achievements Badge")]
        public RectTransform achievementsBadgeRoot;
        public TextMeshProUGUI achievementsBadgeText;
        public Vector2 achievementsBadgeOffset = new Vector2(-16f, 0f);
        public Vector2 achievementsBadgeSize = new Vector2(28f, 28f);
        public Color achievementsBadgeColor = new Color32(220, 45, 45, 255);

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
            RefreshAchievementsBadge();
        }

        private void OnDestroy()
        {
            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void OnEnable()
        {
            RefreshAchievementsBadge();
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

        private void RefreshAchievementsBadge()
        {
            if (achievementsButton == null)
                return;

            EnsureAchievementsBadgeReferences();
            if (achievementsBadgeRoot == null || achievementsBadgeText == null)
                return;

            int unfinishedCount = DailyAwardsManager.Instance.GetUnfinishedTodayCount();
            bool shouldShow = unfinishedCount > 0;

            achievementsBadgeRoot.gameObject.SetActive(shouldShow);
            if (shouldShow)
                achievementsBadgeText.text = unfinishedCount > 99 ? "99+" : unfinishedCount.ToString();
        }

        private void EnsureAchievementsBadgeReferences()
        {
            if (achievementsBadgeRoot == null && achievementsButton != null)
            {
                Transform existing = achievementsButton.transform.Find("AchievementsDailyBadge");
                if (existing != null)
                    achievementsBadgeRoot = existing as RectTransform;
            }

            if (achievementsBadgeText == null && achievementsBadgeRoot != null)
                achievementsBadgeText = achievementsBadgeRoot.GetComponentInChildren<TextMeshProUGUI>(true);

            if (achievementsBadgeRoot != null && achievementsBadgeText != null)
                return;

            CreateAchievementsBadgeRuntime();
        }

        private void CreateAchievementsBadgeRuntime()
        {
            if (achievementsButton == null)
                return;

            GameObject badgeObject = new GameObject("AchievementsDailyBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
            badgeRect.SetParent(achievementsButton.transform, false);
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = achievementsBadgeSize;
            badgeRect.anchoredPosition = achievementsBadgeOffset;

            Image badgeImage = badgeObject.GetComponent<Image>();
            badgeImage.color = achievementsBadgeColor;
            badgeImage.raycastTarget = false;
            Sprite circleSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            if (circleSprite != null)
                badgeImage.sprite = circleSprite;

            GameObject textObject = new GameObject("CountText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(badgeRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "0";
            text.color = Color.white;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 18f;
            text.raycastTarget = false;

            achievementsBadgeRoot = badgeRect;
            achievementsBadgeText = text;
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
