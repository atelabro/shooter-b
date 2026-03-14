using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class GameOverModalController : MonoBehaviour
    {
        private const string SadTrumpetResourcePath = "Audio/sad_trumpet";
        private const string ModalOpenResourcePath = "Audio/modal_open";
        private const string ButtonRevealResourcePath = "Audio/small_drum";

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

        [Header("Title Animation")]
        [Min(0f)] public float titleRevealDelay = 0.08f;
        [Min(0.01f)] public float titleDropDuration = 0.65f;
        [Min(0.01f)] public float titleWobbleDuration = 0.35f;
        [Range(0.5f, 1.2f)] public float titleStartScale = 0.72f;
        [Range(0.8f, 1.5f)] public float titleOvershootScale = 1.18f;
        public Vector2 titleStartOffset = new Vector2(0f, 120f);
        public float titleWobbleAngle = 8f;

        [Header("Button Animation")]
        [Min(0f)] public float buttonRevealStagger = 0.06f;
        [Min(0.01f)] public float buttonBangDuration = 0.18f;
        [Range(0.5f, 1.2f)] public float buttonBangStartScale = 0.78f;
        [Range(0.8f, 1.4f)] public float buttonBangOvershootScale = 1.08f;

        private bool hasLastShowData;
        private long lastFinalScore;
        private long lastHighScore;
        private Constants.GameMode lastMode;
        private bool lastIsNewHighScore;
        private bool isInitialized;
        private ModalDialogAnimator modalAnimator;
        private AudioSource gameOverAudioSource;
        private AudioClip sadTrumpetClip;
        private AudioClip modalOpenClip;
        private AudioClip buttonRevealClip;
        private Coroutine closeRoutine;
        private Coroutine introSequenceCoroutine;
        private bool introSequenceCompleted;
        private static GameOverModalActionRunner actionRunner;
        private Vector3 titleBaseScale = Vector3.one;
        private Vector2 titleBaseAnchoredPosition = Vector2.zero;
        private bool hasCapturedTitleBaseScale;
        private bool hasCapturedTitleBaseAnchoredPosition;
        private Vector3 retryButtonBaseScale = Vector3.one;
        private bool hasCapturedRetryButtonBaseScale;
        private Vector3 menuButtonBaseScale = Vector3.one;
        private bool hasCapturedMenuButtonBaseScale;
        private CanvasGroup retryButtonCanvasGroup;
        private CanvasGroup menuButtonCanvasGroup;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (!isInitialized)
                return;

            CancelPendingCloseTransition();
            StopIntroSequence();

            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.RemoveListener(OnMenuClicked);

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;

            if (AudioSettingsManager.HasInstance)
                AudioSettingsManager.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
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

            StopIntroSequence();
            introSequenceCompleted = false;
            ResetIntroVisuals();

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

            PlayModalOpenClip();
            introSequenceCoroutine = StartCoroutine(PlayIntroSequence());
        }

        public void Hide()
        {
            EnsureInitialized();
            EnsureModalRoot();
            StopIntroSequence();

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
            EnsureAudioReady();
            CaptureAnimationDefaults();
            ValidateRequiredReferences();
            RefreshLocalizedTexts();

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);

            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            AudioSettingsManager.Instance.OnAudioSettingsChanged += HandleAudioSettingsChanged;
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

        private void EnsureAudioReady()
        {
            if (gameOverAudioSource == null)
            {
                gameOverAudioSource = GetComponent<AudioSource>();
                if (gameOverAudioSource == null)
                    gameOverAudioSource = gameObject.AddComponent<AudioSource>();

                gameOverAudioSource.playOnAwake = false;
                gameOverAudioSource.loop = false;
            }

            gameOverAudioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();

            if (sadTrumpetClip == null)
                sadTrumpetClip = Resources.Load<AudioClip>(SadTrumpetResourcePath);

            if (modalOpenClip == null)
                modalOpenClip = Resources.Load<AudioClip>(ModalOpenResourcePath);

            if (buttonRevealClip == null)
                buttonRevealClip = Resources.Load<AudioClip>(ButtonRevealResourcePath);
        }

        private void CaptureAnimationDefaults()
        {
            if (titleText != null && !hasCapturedTitleBaseScale)
            {
                titleBaseScale = titleText.rectTransform.localScale;
                hasCapturedTitleBaseScale = true;
            }

            if (titleText != null && !hasCapturedTitleBaseAnchoredPosition)
            {
                titleBaseAnchoredPosition = titleText.rectTransform.anchoredPosition;
                hasCapturedTitleBaseAnchoredPosition = true;
            }

            if (retryButton != null && !hasCapturedRetryButtonBaseScale)
            {
                retryButtonBaseScale = retryButton.transform.localScale;
                hasCapturedRetryButtonBaseScale = true;
            }

            if (menuButton != null && !hasCapturedMenuButtonBaseScale)
            {
                menuButtonBaseScale = menuButton.transform.localScale;
                hasCapturedMenuButtonBaseScale = true;
            }

            if (retryButton != null && retryButtonCanvasGroup == null)
                retryButtonCanvasGroup = retryButton.GetComponent<CanvasGroup>() ?? retryButton.gameObject.AddComponent<CanvasGroup>();

            if (menuButton != null && menuButtonCanvasGroup == null)
                menuButtonCanvasGroup = menuButton.GetComponent<CanvasGroup>() ?? menuButton.gameObject.AddComponent<CanvasGroup>();
        }

        private void ValidateRequiredReferences()
        {
            if (titleText == null)
                GameLog.Warning("[GameOverModalController] titleText is not assigned.");

            if (finalScoreHeaderText == null)
                GameLog.Warning("[GameOverModalController] finalScoreHeaderText is not assigned.");
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

        private void HandleAudioSettingsChanged()
        {
            if (gameOverAudioSource != null)
                gameOverAudioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
        }

        private void StopIntroSequence()
        {
            if (introSequenceCoroutine != null)
            {
                StopCoroutine(introSequenceCoroutine);
                introSequenceCoroutine = null;
            }

            if (introSequenceCompleted)
                RestoreFinalIntroState();
        }

        private void ResetIntroVisuals()
        {
            CaptureAnimationDefaults();

            if (titleText != null)
            {
                RectTransform titleRect = titleText.rectTransform;
                titleText.alpha = 0f;
                titleRect.localScale = titleBaseScale * titleStartScale;
                titleRect.anchoredPosition = titleBaseAnchoredPosition + titleStartOffset;
                titleRect.localRotation = Quaternion.Euler(0f, 0f, -titleWobbleAngle);
            }

            SetButtonIntroState(retryButton, retryButtonCanvasGroup, retryButtonBaseScale, false);
            SetButtonIntroState(menuButton, menuButtonCanvasGroup, menuButtonBaseScale, false);
        }

        private void RestoreFinalIntroState()
        {
            if (titleText != null)
            {
                RectTransform titleRect = titleText.rectTransform;
                titleText.alpha = 1f;
                titleRect.localScale = titleBaseScale;
                titleRect.anchoredPosition = titleBaseAnchoredPosition;
                titleRect.localRotation = Quaternion.identity;
            }

            SetButtonIntroState(retryButton, retryButtonCanvasGroup, retryButtonBaseScale, true);
            SetButtonIntroState(menuButton, menuButtonCanvasGroup, menuButtonBaseScale, true);
        }

        private IEnumerator PlayIntroSequence()
        {
            float modalShowDuration = modalAnimator != null ? modalAnimator.showDuration : 0f;
            if (modalShowDuration > 0f)
                yield return new WaitForSecondsRealtime(modalShowDuration);

            PlaySadTrumpet();

            if (titleRevealDelay > 0f)
                yield return new WaitForSecondsRealtime(titleRevealDelay);

            yield return PlayTitleDropAnimation();
            yield return PlayButtonBangAnimation(retryButton, retryButtonCanvasGroup, retryButtonBaseScale);

            if (buttonRevealStagger > 0f)
                yield return new WaitForSecondsRealtime(buttonRevealStagger);

            yield return PlayButtonBangAnimation(menuButton, menuButtonCanvasGroup, menuButtonBaseScale);
            introSequenceCompleted = true;
            introSequenceCoroutine = null;
        }

        private IEnumerator PlayTitleDropAnimation()
        {
            if (titleText == null)
                yield break;

            RectTransform titleRect = titleText.rectTransform;
            titleText.alpha = 1f;

            float dropDuration = Mathf.Max(0.01f, titleDropDuration);
            float elapsed = 0f;

            while (elapsed < dropDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dropDuration);
                float moveEase = EaseOutBounce01(t);
                float scaleEase = EaseOutBackStrong01(t);
                float rotationEase = EaseOutBackStrong01(t);
                float scale = Mathf.LerpUnclamped(titleStartScale, titleOvershootScale, scaleEase);

                titleRect.anchoredPosition = Vector2.LerpUnclamped(titleBaseAnchoredPosition + titleStartOffset, titleBaseAnchoredPosition, moveEase);
                titleRect.localScale = titleBaseScale * scale;
                titleRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(-titleWobbleAngle, titleWobbleAngle * 0.35f, rotationEase));
                yield return null;
            }

            float wobbleDuration = Mathf.Max(0.01f, titleWobbleDuration);
            elapsed = 0f;

            while (elapsed < wobbleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / wobbleDuration);
                float damp = 1f - t;
                float angle = Mathf.Sin(t * Mathf.PI * 2.5f) * titleWobbleAngle * 0.35f * damp;
                float scale = Mathf.LerpUnclamped(titleOvershootScale, 1f, EaseOutCubic01(t));

                titleRect.anchoredPosition = titleBaseAnchoredPosition;
                titleRect.localScale = titleBaseScale * scale;
                titleRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            titleRect.anchoredPosition = titleBaseAnchoredPosition;
            titleRect.localScale = titleBaseScale;
            titleRect.localRotation = Quaternion.identity;
        }

        private IEnumerator PlayButtonBangAnimation(Button button, CanvasGroup canvasGroup, Vector3 baseScale)
        {
            if (button == null)
                yield break;

            SetCanvasGroupVisible(canvasGroup, true);
            button.transform.localScale = baseScale * buttonBangStartScale;
            PlayButtonRevealClip();

            float duration = Mathf.Max(0.01f, buttonBangDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBackStrong01(t);
                float scale = Mathf.LerpUnclamped(buttonBangStartScale, buttonBangOvershootScale, eased);
                button.transform.localScale = baseScale * scale;
                yield return null;
            }

            button.transform.localScale = baseScale;
        }

        private static void SetButtonIntroState(Button button, CanvasGroup canvasGroup, Vector3 baseScale, bool isVisible)
        {
            if (button == null)
                return;

            button.transform.localScale = baseScale;
            SetCanvasGroupVisible(canvasGroup, isVisible);
        }

        private static void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool isVisible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        private void PlaySadTrumpet()
        {
            EnsureAudioReady();
            if (gameOverAudioSource == null || sadTrumpetClip == null)
                return;

            gameOverAudioSource.Stop();
            gameOverAudioSource.PlayOneShot(sadTrumpetClip);
        }

        private void PlayModalOpenClip()
        {
            EnsureAudioReady();
            if (gameOverAudioSource == null || modalOpenClip == null)
                return;

            gameOverAudioSource.PlayOneShot(modalOpenClip);
        }

        private void PlayButtonRevealClip()
        {
            EnsureAudioReady();
            if (gameOverAudioSource == null || buttonRevealClip == null)
                return;

            gameOverAudioSource.PlayOneShot(buttonRevealClip);
        }

        private void StartCloseTransition(System.Action onClosed)
        {
            EnsureInitialized();
            EnsureModalRoot();

            CancelPendingCloseTransition();

            float delay = 0f;
            if (modalRoot != null)
            {
                StopIntroSequence();
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

        private static float EaseOutCubic01(float t)
        {
            t = Mathf.Clamp01(t);
            float x = 1f - t;
            return 1f - (x * x * x);
        }

        private static float EaseOutBackStrong01(float t)
        {
            t = Mathf.Clamp01(t);
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + (c3 * x * x * x) + (c1 * x * x);
        }

        private static float EaseOutBounce01(float t)
        {
            t = Mathf.Clamp01(t);
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
                return n1 * t * t;

            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return (n1 * t * t) + 0.75f;
            }

            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return (n1 * t * t) + 0.9375f;
            }

            t -= 2.625f / d1;
            return (n1 * t * t) + 0.984375f;
        }
    }
}
