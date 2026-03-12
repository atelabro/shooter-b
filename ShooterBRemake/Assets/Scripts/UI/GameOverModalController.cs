using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ShooterB
{
    public class GameOverModalController : MonoBehaviour
    {
        private sealed class GameOverModalActionRunner : MonoBehaviour
        {
        }

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
        private bool isInitialized;
        private ModalDialogAnimator modalAnimator;
        private Coroutine closeRoutine;
        private static GameOverModalActionRunner actionRunner;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (!isInitialized)
                return;

            CancelPendingCloseTransition();

            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.RemoveListener(OnMenuClicked);

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        public void Show(long finalScore, long highScore, Constants.GameMode mode, bool isNewHighScore)
        {
            EnsureInitialized();
            EnsureModalRoot();
            hasLastShowData = true;
            lastFinalScore = finalScore;
            lastHighScore = highScore;
            lastMode = mode;
            lastIsNewHighScore = isNewHighScore;

            bool isCampaign = mode == Constants.GameMode.Campaign;
            string highFormat = LocalizationManager.Instance.Get("campaign.gameover.high_format", "High: {0}");

            if (finalScoreText != null)
                finalScoreText.gameObject.SetActive(false);

            if (highScoreText != null)
            {
                highScoreText.gameObject.SetActive(!isCampaign);
                if (!isCampaign)
                    highScoreText.text = string.Format(highFormat, highScore);
            }

            if (modeText != null)
                modeText.gameObject.SetActive(false);

            if (newHighScoreBadge != null)
                newHighScoreBadge.SetActive(!isCampaign && isNewHighScore);

            if (modalRoot != null)
            {
                EnsureAnimator();

                if (modalAnimator != null)
                    modalAnimator.Show();
                else
                    modalRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            EnsureInitialized();
            EnsureModalRoot();

            if (modalRoot != null)
            {
                EnsureAnimator();

                if (modalAnimator != null)
                    modalAnimator.Hide();
                else
                    modalRoot.SetActive(false);
            }
        }

        public void OnRetryClicked()
        {
            StartCloseTransition(() =>
            {
                Time.timeScale = 1f;
                SceneController.Instance.ReloadCurrentGameScene();
            });
        }

        public void OnMenuClicked()
        {
            StartCloseTransition(() =>
            {
                Time.timeScale = 1f;
                if (GameManager.Instance.CurrentGameMode == Constants.GameMode.Campaign)
                    SceneController.Instance.LoadCampaignMapScene();
                else
                    SceneController.Instance.ReturnToMenu();
            });
        }

        private void EnsureModalRoot()
        {
            if (modalRoot == null)
                modalRoot = gameObject;
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
                return;

            EnsureModalRoot();
            EnsureAnimator();
            ResolveTextReferences();
            RefreshLocalizedTexts();

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);

            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            isInitialized = true;
        }

        private void EnsureAnimator()
        {
            if (modalRoot == null)
                return;

            modalAnimator = modalRoot.GetComponent<ModalDialogAnimator>();
            if (modalAnimator == null)
                modalAnimator = modalRoot.AddComponent<ModalDialogAnimator>();

            modalAnimator.modalRoot = modalRoot;
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
            {
                bool isCampaign = lastMode == Constants.GameMode.Campaign;
                string highFormat = LocalizationManager.Instance.Get("campaign.gameover.high_format", "High: {0}");

                if (highScoreText != null)
                {
                    highScoreText.gameObject.SetActive(!isCampaign);
                    if (!isCampaign)
                        highScoreText.text = string.Format(highFormat, lastHighScore);
                }

                if (newHighScoreBadge != null)
                    newHighScoreBadge.SetActive(!isCampaign && lastIsNewHighScore);
            }
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

        private void StartCloseTransition(System.Action onClosed)
        {
            EnsureInitialized();
            EnsureModalRoot();

            CancelPendingCloseTransition();

            float delay = 0f;
            if (modalRoot != null)
            {
                EnsureAnimator();
                delay = modalAnimator != null ? modalAnimator.HideWithDelay() : 0f;
                if (modalAnimator == null)
                    modalRoot.SetActive(false);
            }

            closeRoutine = EnsureActionRunner().StartCoroutine(InvokeAfterDelay(delay, () =>
            {
                closeRoutine = null;
                onClosed?.Invoke();
            }));
        }

        private static GameOverModalActionRunner EnsureActionRunner()
        {
            if (actionRunner != null)
                return actionRunner;

            GameObject runnerObject = new GameObject("GameOverModalActionRunner");
            DontDestroyOnLoad(runnerObject);
            actionRunner = runnerObject.AddComponent<GameOverModalActionRunner>();
            return actionRunner;
        }

        private static IEnumerator InvokeAfterDelay(float delay, System.Action onClosed)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            onClosed?.Invoke();
        }

        private void CancelPendingCloseTransition()
        {
            if (closeRoutine != null && actionRunner != null)
            {
                actionRunner.StopCoroutine(closeRoutine);
                closeRoutine = null;
            }
        }
    }
}
