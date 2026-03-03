using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class AchievementUnlockPopupController : MonoBehaviour
    {
        public TMP_Text popupText;
        public Color headerColor = new Color(1f, 0.82f, 0.22f, 1f);

        private void Awake()
        {
            if (popupText == null)
                popupText = GetComponent<TMP_Text>();
        }

        public void Configure(string achievementTitle, int coinReward)
        {
            ConfigureCustom("ACHIEVEMENT UNLOCKED", achievementTitle, coinReward);
        }

        public void ConfigureCustom(string header, string title, int coinReward)
        {
            if (popupText == null)
                return;

            string safeHeader = string.IsNullOrWhiteSpace(header) ? "REWARD" : header;
            string safeTitle = string.IsNullOrWhiteSpace(title) ? "Objective" : title;
            int safeCoins = Mathf.Max(0, coinReward);

            popupText.text = $"{safeHeader}\n{safeTitle}\n+{safeCoins} COINS";
            popupText.color = headerColor;
        }
    }
}
