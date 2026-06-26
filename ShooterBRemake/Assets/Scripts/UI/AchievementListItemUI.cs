using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class AchievementListItemUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI statusText;
        public Image backgroundImage;
        public Image progressFillImage;
        public Slider progressSlider;
        public GameObject achievedBadge;
        public GameObject rewardPanel;
        public Image rewardImage;
        public TextMeshProUGUI rewardText;

        [Header("State Colors")]
        public Color unlockedBackgroundColor = new Color(0.22f, 0.18f, 0.12f, 0.94f);
        public Color lockedBackgroundColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);

        public AchievementManager.AchievementId AchievementId { get; private set; }

        public void Bind(
            AchievementManager.AchievementId id,
            string title,
            string description,
            int progress,
            int target,
            bool isUnlocked,
            float normalizedProgress,
            int coinReward)
        {
            AchievementId = id;
            ApplyCommon(
                title,
                description,
                progress,
                target,
                isUnlocked,
                normalizedProgress,
                coinReward,
                LocalizationManager.Instance.Get("achievements.status.locked", "LOCKED"));
        }

        public void Bind(
            AchievementManager.AchievementId id,
            string title,
            string description,
            int progress,
            int target,
            bool isUnlocked,
            float normalizedProgress)
        {
            Bind(id, title, description, progress, target, isUnlocked, normalizedProgress, 0);
        }

        public void BindCustom(
            string title,
            string description,
            int progress,
            int target,
            bool isUnlocked,
            float normalizedProgress,
            int coinReward,
            string lockedStatusText = null)
        {
            ApplyCommon(title, description, progress, target, isUnlocked, normalizedProgress, coinReward, lockedStatusText);
        }

        private void ApplyCommon(
            string title,
            string description,
            int progress,
            int target,
            bool isUnlocked,
            float normalizedProgress,
            int coinReward,
            string lockedStatusText)
        {
            if (titleText != null)
                titleText.text = title;

            if (descriptionText != null)
                descriptionText.text = description;

            if (progressText != null)
                progressText.text = $"{progress}/{target}";

            if (statusText != null)
            {
                statusText.gameObject.SetActive(!isUnlocked);
                if (!isUnlocked)
                    statusText.text = string.IsNullOrWhiteSpace(lockedStatusText)
                        ? LocalizationManager.Instance.Get("achievements.status.locked", "LOCKED")
                        : lockedStatusText;
            }

            Image rowBackground = backgroundImage != null ? backgroundImage : GetComponent<Image>();
            if (rowBackground != null)
                rowBackground.color = isUnlocked ? unlockedBackgroundColor : lockedBackgroundColor;

            if (progressFillImage != null)
            {
                if (progressFillImage.type == Image.Type.Filled)
                {
                    progressFillImage.fillAmount = Mathf.Clamp01(normalizedProgress);
                }
                else
                {
                    RectTransform fillRect = progressFillImage.rectTransform;
                    fillRect.anchorMin = new Vector2(0f, fillRect.anchorMin.y);
                    fillRect.anchorMax = new Vector2(Mathf.Clamp01(normalizedProgress), fillRect.anchorMax.y);
                    fillRect.offsetMin = new Vector2(0f, fillRect.offsetMin.y);
                    fillRect.offsetMax = new Vector2(0f, fillRect.offsetMax.y);
                }
            }

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value = Mathf.Clamp01(normalizedProgress);
            }

            if (achievedBadge != null)
                achievedBadge.SetActive(isUnlocked);

            bool hasReward = coinReward > 0;
            if (rewardPanel != null)
                rewardPanel.SetActive(hasReward);
            if (rewardImage != null)
                rewardImage.gameObject.SetActive(hasReward);
            if (rewardText != null)
            {
                rewardText.gameObject.SetActive(hasReward);
                rewardText.text = $"+{Mathf.Max(0, coinReward)}";
            }
        }
    }
}
