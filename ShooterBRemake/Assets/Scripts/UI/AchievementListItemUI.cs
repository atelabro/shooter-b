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
                progressText.text = $"Progress: {progress}/{target}";

            if (statusText != null)
            {
                statusText.text = isUnlocked ? "UNLOCKED" : "LOCKED";
            }

            if (backgroundImage != null)
                backgroundImage.color = isUnlocked ? unlockedBackgroundColor : lockedBackgroundColor;

            if (progressFillImage != null)
            {
                progressFillImage.type = Image.Type.Filled;
                progressFillImage.fillMethod = Image.FillMethod.Horizontal;
                progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                progressFillImage.fillAmount = Mathf.Clamp01(normalizedProgress);
            }

            if (achievedBadge != null)
                achievedBadge.SetActive(isUnlocked);
        }
    }
}
