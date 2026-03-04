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
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI finalScoreHeaderText;
        public TextMeshProUGUI finalScoreText;
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI modeText;

        [Header("New High Score")]
        public GameObject newHighScoreBadge;

        [Header("Buttons")]
        public Button retryButton;
        public Button menuButton;
        public TextMeshProUGUI retryButtonText;
        public TextMeshProUGUI menuButtonText;

        private bool hasLastShowData;
        private long lastFinalScore;
        private long lastHighScore;
        private Constants.GameMode lastMode;
        private bool lastIsNewHighScore;

        private void Start()
        {
            EnsureModalRoot();
            ResolveTextReferences();
            RefreshLocalizedTexts();

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);

            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDestroy()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.RemoveListener(OnMenuClicked);

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        public void Show(long finalScore, long highScore, Constants.GameMode mode, bool isNewHighScore)
        {
            EnsureModalRoot();
            hasLastShowData = true;
            lastFinalScore = finalScore;
            lastHighScore = highScore;
            lastMode = mode;
            lastIsNewHighScore = isNewHighScore;

            bool isCampaign = mode == Constants.GameMode.Campaign;
            string scoreFormat = LocalizationManager.Instance.Get("campaign.gameover.score_format", "Score: {0}");
            string highFormat = LocalizationManager.Instance.Get("campaign.gameover.high_format", "High: {0}");
            string modeFormat = LocalizationManager.Instance.Get("campaign.gameover.mode_format", "Mode: {0}");
            string modeLabel = isCampaign
                ? LocalizationManager.Instance.Get("campaign.mode.campaign", "Campaign")
                : mode.ToString();

            if (finalScoreText != null)
                finalScoreText.text = string.Format(scoreFormat, finalScore);

            if (highScoreText != null)
            {
                highScoreText.gameObject.SetActive(!isCampaign);
                if (!isCampaign)
                    highScoreText.text = string.Format(highFormat, highScore);
            }

            if (modeText != null)
                modeText.text = string.Format(modeFormat, modeLabel);

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

        private void ResolveTextReferences()
        {
            if (titleText == null)
                titleText = FindTextByContent("Game Over");

            if (finalScoreHeaderText == null)
                finalScoreHeaderText = FindTextByContent("Final Score");

            if (retryButtonText == null && retryButton != null)
                retryButtonText = retryButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (menuButtonText == null && menuButton != null)
                menuButtonText = menuButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void RefreshLocalizedTexts()
        {
            if (titleText != null)
                titleText.text = LocalizationManager.Instance.Get("campaign.gameover.title", "Game Over");

            if (finalScoreHeaderText != null)
                finalScoreHeaderText.text = LocalizationManager.Instance.Get("campaign.gameover.final_score", "Final Score");

            if (retryButtonText != null)
                retryButtonText.text = LocalizationManager.Instance.Get("common.restart", "Restart");

            if (menuButtonText != null)
                menuButtonText.text = LocalizationManager.Instance.Get("common.back", "Back");
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedTexts();

            if (modalRoot != null && modalRoot.activeInHierarchy && hasLastShowData)
                Show(lastFinalScore, lastHighScore, lastMode, lastIsNewHighScore);
        }

        private TextMeshProUGUI FindTextByContent(string content)
        {
            if (modalRoot == null || string.IsNullOrWhiteSpace(content))
                return null;

            TextMeshProUGUI[] texts = modalRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].text == content)
                    return texts[i];
            }

            return null;
        }
    }
}
