using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class GameOverModalController : MonoBehaviour
    {
        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Text")]
        public TextMeshProUGUI finalScoreText;
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI modeText;

        [Header("New High Score")]
        public GameObject newHighScoreBadge;

        [Header("Buttons")]
        public Button retryButton;
        public Button menuButton;

        private void Start()
        {
            EnsureModalRoot();

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);
        }

        public void Show(long finalScore, long highScore, Constants.GameMode mode, bool isNewHighScore)
        {
            EnsureModalRoot();

            bool isCampaign = mode == Constants.GameMode.Campaign;

            if (finalScoreText != null)
                finalScoreText.text = $"Score: {finalScore}";

            if (highScoreText != null)
            {
                highScoreText.gameObject.SetActive(!isCampaign);
                if (!isCampaign)
                    highScoreText.text = $"High: {highScore}";
            }

            if (modeText != null)
                modeText.text = $"Mode: {mode}";

            if (newHighScoreBadge != null)
                newHighScoreBadge.SetActive(!isCampaign && isNewHighScore);

            if (modalRoot != null)
                modalRoot.SetActive(true);
        }

        public void Hide()
        {
            EnsureModalRoot();

            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        public void OnRetryClicked()
        {
            SceneController.Instance.ReloadCurrentGameScene();
        }

        public void OnMenuClicked()
        {
            if (GameManager.Instance.CurrentGameMode == Constants.GameMode.Campaign)
                SceneController.Instance.LoadCampaignMapScene();
            else
                SceneController.Instance.ReturnToMenu();
        }

        private void EnsureModalRoot()
        {
            if (modalRoot == null)
                modalRoot = gameObject;
        }
    }
}
