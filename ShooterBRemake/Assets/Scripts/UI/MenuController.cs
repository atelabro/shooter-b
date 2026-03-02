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

        private void Start()
        {
            if (campaignButton != null)
                campaignButton.onClick.AddListener(OnCampaignClicked);

            if (armoryButton != null)
                armoryButton.onClick.AddListener(OnArmoryClicked);

            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            UpdateHighScore();

            if (titleText != null)
                titleText.text = "DUCKOFF";
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
                highScoreText.text = $"High Score: {highScore}";
            }
        }
    }
}
