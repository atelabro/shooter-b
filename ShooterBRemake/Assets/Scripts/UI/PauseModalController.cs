using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace ShooterB
{
    public class PauseModalController : MonoBehaviour
    {
        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Buttons")]
        public Button resumeButton;
        public Button restartButton;
        public Button menuButton;
        public TextMeshProUGUI pauseTitleText;
        public TextMeshProUGUI resumeButtonText;
        public TextMeshProUGUI restartButtonText;
        public TextMeshProUGUI menuButtonText;
        private bool isInitialized;
        private ModalDialogAnimator modalAnimator;
        private Coroutine closeRoutine;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (!isInitialized)
                return;

            if (closeRoutine != null)
                StopCoroutine(closeRoutine);

            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(OnResumeClicked);

            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);

            if (menuButton != null)
                menuButton.onClick.RemoveListener(OnMenuClicked);

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        public void Show()
        {
            EnsureInitialized();

            if (GameManager.Instance.IsGameOver)
                return;

            EnsureModalRoot();

            if (modalRoot != null)
            {
                EnsureAnimator();

                if (modalAnimator != null)
                    modalAnimator.Show();
                else
                    modalRoot.SetActive(true);
            }

            GameManager.Instance.PauseGame();
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

        public void Toggle()
        {
            if (GameManager.Instance.IsGameOver)
                return;

            if (GameManager.Instance.IsPaused)
                OnResumeClicked();
            else
                Show();
        }

        public void OnResumeClicked()
        {
            StartCloseTransition(() =>
            {
                GameManager.Instance.ResumeGame();
            });
        }

        public void OnRestartClicked()
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

            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

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
            if (pauseTitleText == null)
                pauseTitleText = FindTextByContent("Game Paused");

            if (resumeButtonText == null && resumeButton != null)
                resumeButtonText = resumeButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (restartButtonText == null && restartButton != null)
                restartButtonText = restartButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (menuButtonText == null && menuButton != null)
                menuButtonText = menuButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void RefreshLocalizedTexts()
        {
            if (pauseTitleText != null)
                pauseTitleText.text = LocalizationManager.Instance.Get("campaign.pause.title", "Game Paused");

            if (resumeButtonText != null)
                resumeButtonText.text = LocalizationManager.Instance.Get("common.resume", "Resume");

            if (restartButtonText != null)
                restartButtonText.text = LocalizationManager.Instance.Get("common.restart", "Restart");

            if (menuButtonText != null)
                menuButtonText.text = LocalizationManager.Instance.Get("common.back", "Back");
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedTexts();
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

            if (closeRoutine != null)
                StopCoroutine(closeRoutine);

            closeRoutine = StartCoroutine(CloseThenInvoke(onClosed));
        }

        private IEnumerator CloseThenInvoke(System.Action onClosed)
        {
            float delay = 0f;
            if (modalRoot != null)
            {
                EnsureAnimator();
                delay = modalAnimator != null ? modalAnimator.HideWithDelay() : 0f;
                if (modalAnimator == null)
                    modalRoot.SetActive(false);
            }

            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            closeRoutine = null;
            onClosed?.Invoke();
        }
    }
}
