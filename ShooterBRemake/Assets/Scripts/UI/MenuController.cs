using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShooterB
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI Elements")]
        public Button campaignButton;
        public Button arcadeButton;
        public Button quitButton;
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI titleText;

        private void Start()
        {
            if (campaignButton != null)
                campaignButton.onClick.AddListener(OnCampaignClicked);

            if (arcadeButton != null)
                arcadeButton.onClick.AddListener(OnArcadeClicked);

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

        private void OnArcadeClicked()
        {
            SceneController.Instance.LoadGameScene(Constants.GameMode.Arcade);
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
