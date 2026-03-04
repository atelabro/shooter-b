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
            ConfigureCustom("ACHIEVEMENT UNLOCKED", achievementTitle, coinReward);
        }

        public void ConfigureCustom(string header, string title, int coinReward)
        {
            string safeHeader = string.IsNullOrWhiteSpace(header) ? "REWARD" : header;
            string safeTitle = string.IsNullOrWhiteSpace(title) ? "Objective" : title;
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
                    rewardText.text = $"+{safeCoins} COINS";
                    rewardText.color = bodyColor;
                }

                return;
            }

            if (popupText == null)
                return;

            popupText.text = $"{safeHeader}\n{safeTitle}\n+{safeCoins} COINS";
            popupText.color = headerColor;
        }
    }
}
