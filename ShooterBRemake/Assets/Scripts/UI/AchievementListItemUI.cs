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

        [Header("State Colors")]
        public Color unlockedBackgroundColor = new Color(0.18f, 0.34f, 0.2f, 1f);
        public Color lockedBackgroundColor = new Color(0.18f, 0.18f, 0.2f, 1f);

        public AchievementManager.AchievementId AchievementId { get; private set; }

        public void Bind(
            AchievementManager.AchievementId id,
            string title,
            string description,
            int progress,
            int target,
            bool isUnlocked,
            float normalizedProgress)
        {
            AchievementId = id;

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
                    statusText.text = "LOCKED";
            }

            if (backgroundImage != null)
                backgroundImage.color = isUnlocked ? unlockedBackgroundColor : lockedBackgroundColor;

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
        }
    }
}
