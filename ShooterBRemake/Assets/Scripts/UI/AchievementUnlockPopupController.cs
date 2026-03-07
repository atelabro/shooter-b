using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class AchievementUnlockPopupController : MonoBehaviour
    {
        public TMP_Text headerText;
        public TMP_Text titleText;
        public TMP_Text rewardText;
        public Button actionButton;
        public TMP_Text actionButtonText;

        // Backward-compatible fallback for old single-text prefabs.
        public TMP_Text popupText;
        public Color headerColor = new Color(1f, 0.82f, 0.22f, 1f);
        public Color bodyColor = Color.white;

        private Func<bool> onActionButtonClicked;
        private bool rewardPositionCached;
        private Vector2 defaultRewardAnchoredPosition;

        private void Awake()
        {
            if (headerText == null)
            {
                Transform child = transform.Find("Panel/HeaderText");
                if (child != null)
                    headerText = child.GetComponent<TMP_Text>();
            }

            if (titleText == null)
            {
                Transform child = transform.Find("Panel/TitleText");
                if (child != null)
                    titleText = child.GetComponent<TMP_Text>();
            }

            if (rewardText == null)
            {
                Transform child = transform.Find("Panel/RewardText");
                if (child != null)
                    rewardText = child.GetComponent<TMP_Text>();
            }

            if (popupText == null)
                popupText = GetComponent<TMP_Text>();

            if (actionButton == null)
            {
                Transform buttonTransform = transform.Find("Panel/ActionButton");
                if (buttonTransform != null)
                    actionButton = buttonTransform.GetComponent<Button>();
            }

            if (actionButton != null && actionButtonText == null)
                actionButtonText = actionButton.GetComponentInChildren<TMP_Text>(true);
        }

        public void Configure(string achievementTitle, int coinReward)
        {
            ConfigureCustom(
                LocalizationManager.Instance.Get("campaign.hud.popup.achievement_unlocked", "ACHIEVEMENT UNLOCKED"),
                achievementTitle,
                coinReward);
        }

        public void ConfigureCustom(
            string header,
            string title,
            int coinReward,
            bool showActionButton = false,
            int actionBonusCoins = 0,
            bool actionAlreadyClaimed = false,
            Func<bool> onActionClick = null)
        {
            string safeHeader = string.IsNullOrWhiteSpace(header)
                ? LocalizationManager.Instance.Get("reward.header.default", "REWARD")
                : header;
            string safeTitle = string.IsNullOrWhiteSpace(title)
                ? LocalizationManager.Instance.Get("reward.title.default", "Objective")
                : title;
            string coinsSuffix = LocalizationManager.Instance.Get("reward.coins_suffix", "COINS");
            int safeCoins = Mathf.Max(0, coinReward);

            if (headerText != null || titleText != null || rewardText != null)
            {
                if (headerText != null)
                {
                    headerText.text = safeHeader;
                    headerText.color = headerColor;
                }

                if (titleText != null)
                {
                    titleText.text = safeTitle;
                    titleText.color = bodyColor;
                }

                if (rewardText != null)
                {
                    rewardText.text = $"+{safeCoins} {coinsSuffix}";
                    rewardText.color = bodyColor;
                }

                ConfigureActionButton(showActionButton, actionBonusCoins, actionAlreadyClaimed, onActionClick);

                return;
            }

            if (popupText == null)
                return;

            popupText.text = $"{safeHeader}\n{safeTitle}\n+{safeCoins} {coinsSuffix}";
            popupText.color = headerColor;
        }

        private void ConfigureActionButton(bool show, int bonusCoins, bool alreadyClaimed, Func<bool> onClick)
        {
            if (actionButton == null && show)
                CreateActionButton();

            if (actionButton == null)
                return;

            if (rewardText != null)
            {
                RectTransform rewardRect = rewardText.rectTransform;
                if (rewardRect != null)
                {
                    if (!rewardPositionCached)
                    {
                        defaultRewardAnchoredPosition = rewardRect.anchoredPosition;
                        rewardPositionCached = true;
                    }

                    rewardRect.anchoredPosition = show
                        ? new Vector2(defaultRewardAnchoredPosition.x, defaultRewardAnchoredPosition.y + 34f)
                        : defaultRewardAnchoredPosition;
                }
            }

            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
            onActionButtonClicked = null;
            actionButton.gameObject.SetActive(show);
            if (!show)
                return;

            string label;
            bool interactable;
            if (alreadyClaimed)
            {
                label = LocalizationManager.Instance.Get("achievements.daily_ad_bonus.claimed", "CLAIMED");
                interactable = false;
            }
            else
            {
                string format = LocalizationManager.Instance.Get("achievements.daily_ad_bonus.get_format", "Get +{0} coins");
                label = string.Format(format, Mathf.Max(0, bonusCoins));
                interactable = true;
                onActionButtonClicked = onClick;
                actionButton.onClick.AddListener(HandleActionButtonClicked);
            }

            actionButton.interactable = interactable;
            if (actionButtonText != null)
            {
                actionButtonText.text = label;
                actionButtonText.color = Color.white;
            }
        }

        private void HandleActionButtonClicked()
        {
            if (onActionButtonClicked == null)
                return;

            bool claimed = onActionButtonClicked.Invoke();
            if (claimed)
                ConfigureActionButton(true, 0, true, null);
        }

        private void CreateActionButton()
        {
            Transform panel = transform.Find("Panel");
            if (panel == null)
                return;

            GameObject buttonObject = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(320f, 46f);
            buttonRect.anchoredPosition = new Vector2(0f, 10f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.22f, 0.58f, 0.19f, 0.96f);

            actionButton = buttonObject.GetComponent<Button>();
            ColorBlock colors = actionButton.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.52f, 0.52f, 0.52f, 0.9f);
            actionButton.colors = colors;

            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);

            actionButtonText = labelObject.GetComponent<TextMeshProUGUI>();
            actionButtonText.alignment = TextAlignmentOptions.Center;
            actionButtonText.fontSize = 24f;
            actionButtonText.fontStyle = FontStyles.Bold;
            actionButtonText.enableWordWrapping = false;
            actionButtonText.text = string.Empty;
            actionButtonText.color = Color.white;
        }
    }
}
