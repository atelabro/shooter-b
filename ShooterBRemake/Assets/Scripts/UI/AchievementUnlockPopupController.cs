using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class AchievementUnlockPopupController : MonoBehaviour
    {
        public TMP_Text headerText;
        public TMP_Text titleText;
        public TMP_Text rewardText;

        // Backward-compatible fallback for old single-text prefabs.
        public TMP_Text popupText;
        public Color headerColor = new Color(1f, 0.82f, 0.22f, 1f);
        public Color bodyColor = Color.white;

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
        }

        public void Configure(string achievementTitle, int coinReward)
        {
            ConfigureCustom(
                LocalizationManager.Instance.Get("campaign.hud.popup.achievement_unlocked", "ACHIEVEMENT UNLOCKED"),
                achievementTitle,
                coinReward);
        }

        public void ConfigureCustom(string header, string title, int coinReward)
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

                return;
            }

            if (popupText == null)
                return;

            popupText.text = $"{safeHeader}\n{safeTitle}\n+{safeCoins} {coinsSuffix}";
            popupText.color = headerColor;
        }
    }
}
