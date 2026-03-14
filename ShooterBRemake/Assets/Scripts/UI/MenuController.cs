using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShooterB
{
    public class MenuController : MonoBehaviour
    {
        private static Sprite runtimeBadgeCircleSprite;
        private const int DevPanelTapCount = 7;
        private const float DevPanelTapWindowSeconds = 1.5f;

        [Header("UI Elements")]
        public Button campaignButton;
        public Button armoryButton;
        public Button achievementsButton;
        public Button quitButton;
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI titleText;
        public Graphic logoTapTarget;
        public TextMeshProUGUI campaignButtonText;
        public TextMeshProUGUI armoryButtonText;
        public TextMeshProUGUI achievementsButtonText;
        public TextMeshProUGUI quitButtonText;

        [Header("Settings UI")]
        public Button settingsButton;
        public MenuSettingsModalController settingsModalController;

        [Header("Achievements Badge")]
        public RectTransform achievementsBadgeRoot;
        public TextMeshProUGUI achievementsBadgeText;
        public Vector2 achievementsBadgeOffset = new Vector2(-16f, 0f);
        public Vector2 achievementsBadgeSize = new Vector2(28f, 28f);
        public Color achievementsBadgeColor = new Color32(220, 45, 45, 255);
        public Color achievementsBadgeAdRewardColor = new Color32(56, 168, 74, 255);

        [Header("Localization")]
        public LanguageDropdownController languageDropdown;

        [Header("Dev Panel")]
        public bool allowBetaDevPanel = true;
        public CityConfig[] devPanelCampaignCities;

        private DevPanelController devPanelController;
        private int titleTapCount;
        private float lastTitleTapTime;

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
            EnsureSettingsUIReferences();
            RegisterSettingsListeners();

            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            InitializeLanguageDropdown();
            UpdateHighScore();
            RefreshLocalizedTexts();
            RefreshAchievementsBadge();
            EnsureDevPanel();
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

        private void OnSettingsClicked()
        {
            if (settingsModalController != null)
            {
                settingsModalController.Open();
                return;
            }

            GameLog.Warning("[MenuController] settingsModalController is not assigned.");
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
            EnsureLogoTapTrigger();
        }

        private void InitializeLanguageDropdown()
        {
            if (languageDropdown == null)
                languageDropdown = FindObjectOfType<LanguageDropdownController>(true);

            if (languageDropdown == null)
            {
                GameLog.Warning("[MenuController] languageDropdown reference is missing.");
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

        private void EnsureSettingsUIReferences()
        {
            if (settingsButton == null)
                GameLog.Warning("[MenuController] settingsButton is not assigned.");

            if (settingsModalController == null)
                GameLog.Warning("[MenuController] settingsModalController is not assigned.");
        }

        private void RegisterSettingsListeners()
        {
            if (settingsButton == null)
                return;

            settingsButton.onClick.RemoveListener(OnSettingsClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void EnsureDevPanel()
        {
            if (!IsDevPanelAllowed())
                return;

            if (devPanelController == null)
                devPanelController = gameObject.GetComponent<DevPanelController>() ?? gameObject.AddComponent<DevPanelController>();

            Canvas rootCanvas = ResolveUiCanvas();
            if (rootCanvas != null)
                devPanelController.SetCanvas(rootCanvas);
            else
                GameLog.Warning("[MenuController] Could not resolve the main menu canvas for the dev panel.");

            devPanelController.SetCampaignCities(devPanelCampaignCities);

            EnsureLogoTapTrigger();
        }

        private void EnsureLogoTapTrigger()
        {
            if (!IsDevPanelAllowed())
                return;

            if (logoTapTarget == null)
            {
                logoTapTarget = FindGraphicByName("Logo");
                if (logoTapTarget == null)
                    logoTapTarget = titleText;
            }

            if (logoTapTarget == null)
            {
                GameLog.Warning("[MenuController] Dev panel trigger target is not assigned. Set logoTapTarget on MenuController.");
                return;
            }

            logoTapTarget.raycastTarget = true;

            LogoTapTrigger trigger = logoTapTarget.GetComponent<LogoTapTrigger>();
            if (trigger == null)
                trigger = logoTapTarget.gameObject.AddComponent<LogoTapTrigger>();

            trigger.Initialize(HandleTitleTapped);
        }

        private Canvas ResolveUiCanvas()
        {
            if (logoTapTarget != null && logoTapTarget.canvas != null)
                return logoTapTarget.canvas;

            if (titleText != null && titleText.canvas != null)
                return titleText.canvas;

            if (campaignButton != null)
                return campaignButton.GetComponentInParent<Canvas>();

            return null;
        }

        private void HandleTitleTapped()
        {
            if (!IsDevPanelAllowed())
                return;

            if (Time.unscaledTime - lastTitleTapTime > DevPanelTapWindowSeconds)
            {
                GameLog.Log($"[MenuController] Logo tap window expired. Resetting count from {titleTapCount}.");
                titleTapCount = 0;
            }

            lastTitleTapTime = Time.unscaledTime;
            titleTapCount++;
            GameLog.Log($"[MenuController] Logo tapped. Count: {titleTapCount}/{DevPanelTapCount}");

            if (titleTapCount < DevPanelTapCount)
                return;

            titleTapCount = 0;
            EnsureDevPanel();
            GameLog.Log($"[MenuController] Logo tap threshold reached. Dev panel controller ready: {devPanelController != null}");
            if (devPanelController == null)
            {
                GameLog.Warning("[MenuController] Dev panel controller is null at toggle time.");
                return;
            }

            GameLog.Log("[MenuController] Logo tap threshold reached. Toggling dev panel.");
            devPanelController.Toggle();
        }

        private bool IsDevPanelAllowed()
        {
            return Debug.isDebugBuild || allowBetaDevPanel;
        }

        private void RefreshAchievementsBadge()
        {
            if (achievementsButton == null)
                return;

            EnsureAchievementsBadgeReferences();
            if (achievementsBadgeRoot == null || achievementsBadgeText == null)
                return;

            Image badgeImage = achievementsBadgeRoot.GetComponent<Image>();
            int unfinishedCount = DailyAwardsManager.Instance.GetUnfinishedTodayCount();
            bool hasPendingDailyObjectives = unfinishedCount > 0;
            bool hasPendingAdReward = !hasPendingDailyObjectives && DailyAwardsManager.Instance.CanClaimDailyAdWatchBonus();
            bool shouldShow = hasPendingDailyObjectives || hasPendingAdReward;

            achievementsBadgeRoot.gameObject.SetActive(shouldShow);
            if (!shouldShow)
                return;

            if (badgeImage != null)
                badgeImage.color = hasPendingDailyObjectives ? achievementsBadgeColor : achievementsBadgeAdRewardColor;

            achievementsBadgeText.text = hasPendingDailyObjectives
                ? (unfinishedCount > 99 ? "99+" : unfinishedCount.ToString())
                : "1";
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
            badgeImage.sprite = GetOrCreateRuntimeCircleSprite();

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

        private static Sprite GetOrCreateRuntimeCircleSprite()
        {
            if (runtimeBadgeCircleSprite != null)
                return runtimeBadgeCircleSprite;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                name = "AchievementsDailyBadgeCircle"
            };

            Color32[] pixels = new Color32[size * size];
            float radius = (size - 1) * 0.5f;
            float center = radius;
            float radiusSquared = radius * radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    bool inside = (dx * dx) + (dy * dy) <= radiusSquared;
                    pixels[(y * size) + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            runtimeBadgeCircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeBadgeCircleSprite.name = "AchievementsDailyBadgeCircleSprite";
            return runtimeBadgeCircleSprite;
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

        private static Graphic FindGraphicByName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return null;

            Graphic[] graphics = FindObjectsOfType<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null && graphics[i].gameObject.name == objectName)
                    return graphics[i];
            }

            return null;
        }
    }
}
