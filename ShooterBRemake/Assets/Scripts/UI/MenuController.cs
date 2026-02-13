using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShooterB
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI Elements")]
        public Button playButton;
        public Button quitButton;
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI titleText;

        private void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            UpdateHighScore();

            if (titleText != null)
                titleText.text = "DUCKOFF";

            Debug.Log("MenuController initialized");
        }

        private void OnPlayClicked()
        {
            Debug.Log("Play button clicked - Starting Normal mode");
            SceneController.Instance.LoadGameScene(Constants.GameMode.Normal);
        }

        private void OnQuitClicked()
        {
            Debug.Log("Quit button clicked");
            SceneController.Instance.QuitGame();
        }

        private void UpdateHighScore()
        {
            if (highScoreText != null)
            {
                int highScore = PlayerPrefs.GetInt(Constants.PREFS_HIGH_SCORE_NORMAL, 0);
                highScoreText.text = $"High Score: {highScore}";
            }
        }
    }
}
