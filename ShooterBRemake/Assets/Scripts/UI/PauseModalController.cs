using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace ShooterB
{
    public class PauseModalController : MonoBehaviour
    {
        private const string ModalOpenResourcePath = "Audio/modal_open";
        private const string ButtonRevealResourcePath = "Audio/small_drum";

        private sealed class PauseModalActionRunner : MonoBehaviour
        {
        }

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

        [Header("Intro Animation")]
        [Min(0f)] public float titleRevealDelay = 0.02f;
        [Min(0.01f)] public float titleBangDuration = 0.2f;
        [Range(0.5f, 1.2f)] public float titleBangStartScale = 0.68f;
        [Range(0.8f, 1.4f)] public float titleBangOvershootScale = 1.12f;
        [Min(0f)] public float buttonRevealStagger = 0.06f;
        [Min(0.01f)] public float buttonBangDuration = 0.18f;
        [Range(0.5f, 1.2f)] public float buttonBangStartScale = 0.78f;
        [Range(0.8f, 1.4f)] public float buttonBangOvershootScale = 1.08f;

        private bool isInitialized;
        private ModalDialogAnimator modalAnimator;
        private AudioSource modalAudioSource;
        private AudioClip modalOpenClip;
        private AudioClip buttonRevealClip;
        private Coroutine closeRoutine;
        private Coroutine introSequenceCoroutine;
        private static PauseModalActionRunner actionRunner;
        private Vector3 titleBaseScale = Vector3.one;
        private bool hasCapturedTitleBaseScale;
        private Vector3 resumeButtonBaseScale = Vector3.one;
        private bool hasCapturedResumeButtonBaseScale;
        private Vector3 restartButtonBaseScale = Vector3.one;
        private bool hasCapturedRestartButtonBaseScale;
        private Vector3 menuButtonBaseScale = Vector3.one;
        private bool hasCapturedMenuButtonBaseScale;
        private Graphic[] resumeButtonGraphics;
        private Graphic[] restartButtonGraphics;
        private Graphic[] menuButtonGraphics;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (!isInitialized)
                return;

            CancelPendingCloseTransition();
            StopIntroSequence(true);

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

            CancelPendingCloseTransition();

            EnsureModalRoot();
            StopIntroSequence(false);
            ResetIntroVisuals();

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
            GameManager.Instance.PauseGame();
        }

        public void Hide()
        {
            EnsureInitialized();
            EnsureModalRoot();
            StopIntroSequence(true);

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
            EnsureAudioReady();
            ResolveTextReferences();
            CaptureAnimationDefaults();
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

        private void EnsureAudioReady()
        {
            if (modalAudioSource == null)
            {
                modalAudioSource = GetComponent<AudioSource>();
                if (modalAudioSource == null)
                    modalAudioSource = gameObject.AddComponent<AudioSource>();

                modalAudioSource.playOnAwake = false;
                modalAudioSource.loop = false;
            }

            modalAudioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();

            if (modalOpenClip == null)
                modalOpenClip = Resources.Load<AudioClip>(ModalOpenResourcePath);

            if (buttonRevealClip == null)
                buttonRevealClip = Resources.Load<AudioClip>(ButtonRevealResourcePath);
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

        private void CaptureAnimationDefaults()
        {
            if (pauseTitleText != null && !hasCapturedTitleBaseScale)
            {
                titleBaseScale = pauseTitleText.rectTransform.localScale;
                hasCapturedTitleBaseScale = true;
            }

            if (resumeButton != null && !hasCapturedResumeButtonBaseScale)
            {
                resumeButtonBaseScale = resumeButton.transform.localScale;
                hasCapturedResumeButtonBaseScale = true;
            }

            if (restartButton != null && !hasCapturedRestartButtonBaseScale)
            {
                restartButtonBaseScale = restartButton.transform.localScale;
                hasCapturedRestartButtonBaseScale = true;
            }

            if (menuButton != null && !hasCapturedMenuButtonBaseScale)
            {
                menuButtonBaseScale = menuButton.transform.localScale;
                hasCapturedMenuButtonBaseScale = true;
            }

            if (resumeButton != null && resumeButtonGraphics == null)
                resumeButtonGraphics = GetButtonGraphics(resumeButton);

            if (restartButton != null && restartButtonGraphics == null)
                restartButtonGraphics = GetButtonGraphics(restartButton);

            if (menuButton != null && menuButtonGraphics == null)
                menuButtonGraphics = GetButtonGraphics(menuButton);
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedTexts();
        }

        private void PlayModalOpenClip()
        {
            EnsureAudioReady();
            if (modalAudioSource == null || modalOpenClip == null)
                return;

            modalAudioSource.PlayOneShot(modalOpenClip);
        }

        private void PlayButtonRevealClip()
        {
            EnsureAudioReady();
            if (modalAudioSource == null || buttonRevealClip == null)
                return;

            modalAudioSource.PlayOneShot(buttonRevealClip);
        }

        private void StopIntroSequence(bool restoreFinalState)
        {
            if (introSequenceCoroutine != null)
            {
                StopCoroutine(introSequenceCoroutine);
                introSequenceCoroutine = null;
            }

            if (restoreFinalState)
                RestoreFinalIntroState();
        }

        private void ResetIntroVisuals()
        {
            CaptureAnimationDefaults();

            if (pauseTitleText != null)
            {
                pauseTitleText.alpha = 0f;
                pauseTitleText.rectTransform.localScale = titleBaseScale * titleBangStartScale;
            }

            SetButtonIntroState(resumeButton, resumeButtonGraphics, resumeButtonBaseScale, false);
            SetButtonIntroState(restartButton, restartButtonGraphics, restartButtonBaseScale, false);
            SetButtonIntroState(menuButton, menuButtonGraphics, menuButtonBaseScale, false);
        }

        private void RestoreFinalIntroState()
        {
            if (pauseTitleText != null)
            {
                pauseTitleText.alpha = 1f;
                pauseTitleText.rectTransform.localScale = titleBaseScale;
            }

            SetButtonIntroState(resumeButton, resumeButtonGraphics, resumeButtonBaseScale, true);
            SetButtonIntroState(restartButton, restartButtonGraphics, restartButtonBaseScale, true);
            SetButtonIntroState(menuButton, menuButtonGraphics, menuButtonBaseScale, true);
        }

        private IEnumerator PlayIntroSequence()
        {
            float modalShowDuration = modalAnimator != null ? modalAnimator.showDuration : 0f;
            if (modalShowDuration > 0f)
                yield return new WaitForSecondsRealtime(modalShowDuration);

            if (titleRevealDelay > 0f)
                yield return new WaitForSecondsRealtime(titleRevealDelay);

            yield return PlayTitleBangAnimation();
            yield return PlayButtonBangAnimation(resumeButton, resumeButtonGraphics, resumeButtonBaseScale);

            if (buttonRevealStagger > 0f)
                yield return new WaitForSecondsRealtime(buttonRevealStagger);

            yield return PlayButtonBangAnimation(restartButton, restartButtonGraphics, restartButtonBaseScale);

            if (buttonRevealStagger > 0f)
                yield return new WaitForSecondsRealtime(buttonRevealStagger);

            yield return PlayButtonBangAnimation(menuButton, menuButtonGraphics, menuButtonBaseScale);
            RestoreFinalIntroState();
            introSequenceCoroutine = null;
        }

        private IEnumerator PlayTitleBangAnimation()
        {
            if (pauseTitleText == null)
                yield break;

            pauseTitleText.alpha = 1f;

            float duration = Mathf.Max(0.01f, titleBangDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBackStrong01(t);
                float scale = Mathf.LerpUnclamped(titleBangStartScale, titleBangOvershootScale, eased);
                pauseTitleText.rectTransform.localScale = titleBaseScale * scale;
                yield return null;
            }

            pauseTitleText.rectTransform.localScale = titleBaseScale;
        }

        private IEnumerator PlayButtonBangAnimation(Button button, IReadOnlyList<Graphic> graphics, Vector3 baseScale)
        {
            if (button == null)
                yield break;

            SetGraphicsVisible(graphics, true);
            button.interactable = true;
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

        private static float EaseOutBackStrong01(float t)
        {
            t = Mathf.Clamp01(t);
            float c1 = 1.7f;
            float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + (c3 * x * x * x) + (c1 * x * x);
        }

        private void SetButtonIntroState(Button button, IReadOnlyList<Graphic> graphics, Vector3 baseScale, bool visible)
        {
            if (button == null)
                return;

            SetGraphicsVisible(graphics, visible);
            button.interactable = visible;
            button.transform.localScale = visible ? baseScale : baseScale * buttonBangStartScale;
        }

        private static void SetGraphicsVisible(IReadOnlyList<Graphic> graphics, bool visible)
        {
            if (graphics == null)
                return;

            float alpha = visible ? 1f : 0f;
            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                    continue;

                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
        }

        private static Graphic[] GetButtonGraphics(Button button)
        {
            if (button == null)
                return null;

            return button.GetComponentsInChildren<Graphic>(true);
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
                StopIntroSequence(true);
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

        private static PauseModalActionRunner EnsureActionRunner()
        {
            if (actionRunner != null)
                return actionRunner;

            GameObject runnerObject = new GameObject("PauseModalActionRunner");
            DontDestroyOnLoad(runnerObject);
            actionRunner = runnerObject.AddComponent<PauseModalActionRunner>();
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
