using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class GameStartingModalController : MonoBehaviour
    {
        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Stage Info")]
        public TMP_Text titleText;
        public TMP_Text briefingText;

        [Header("Buttons")]
        public Button startButton;
        public Button plusLivesButton;
        public Button extraActionButton;
        public TMP_Text plusLivesText;
        public TMP_Text extraActionText;

        [Header("Intro Animation")]
        [Min(0f)] public float titleRevealDelay = 0.02f;
        [Min(0.01f)] public float titleBangDuration = 0.2f;
        [Range(0.5f, 1.2f)] public float titleBangStartScale = 0.68f;
        [Range(0.8f, 1.4f)] public float titleBangOvershootScale = 1.12f;
        [Min(1f)] public float briefingTypingCharsPerSecond = 45f;
        [Min(0f)] public float briefingPunctuationPause = 0.08f;
        [Min(0f)] public float buttonRevealStagger = 0.06f;
        [Min(0.01f)] public float buttonBangDuration = 0.18f;
        [Range(0.5f, 1.2f)] public float buttonBangStartScale = 0.78f;
        [Range(0.8f, 1.4f)] public float buttonBangOvershootScale = 1.08f;

        private bool startRequested;
        private bool plusLivesUsedThisStage;
        private bool extraActionUsedThisStage;
        private bool mercyBonusProcessedThisStage;
        private bool adRequestInFlight;
        private ModalDialogAnimator modalAnimator;
        private Coroutine introSequenceCoroutine;
        private string configuredTitleText = string.Empty;
        private string configuredBriefingText = string.Empty;
        private Vector3 titleBaseScale = Vector3.one;
        private bool hasCapturedTitleBaseScale;
        private Vector3 startButtonBaseScale = Vector3.one;
        private bool hasCapturedStartButtonBaseScale;
        private Vector3 plusLivesButtonBaseScale = Vector3.one;
        private bool hasCapturedPlusLivesButtonBaseScale;
        private Vector3 extraActionButtonBaseScale = Vector3.one;
        private bool hasCapturedExtraActionButtonBaseScale;
        private CanvasGroup plusLivesButtonCanvasGroup;
        private CanvasGroup extraActionButtonCanvasGroup;

        public void Configure(string stageName, string stageBriefing)
        {
            EnsureReferences();
            plusLivesUsedThisStage = false;
            extraActionUsedThisStage = false;
            mercyBonusProcessedThisStage = false;
            adRequestInFlight = false;

            configuredTitleText = string.IsNullOrWhiteSpace(stageName)
                ? LocalizationManager.Instance.Get("campaign.starting.default_stage", "Stage")
                : stageName;

            configuredBriefingText = stageBriefing ?? string.Empty;

            if (titleText != null)
                titleText.text = configuredTitleText;

            if (briefingText != null)
                briefingText.text = configuredBriefingText;

            if (plusLivesText != null)
                plusLivesText.text = LocalizationManager.Instance.Get("campaign.starting.plus_lives", "+2 Lives");

            if (extraActionText != null)
                extraActionText.text = LocalizationManager.Instance.Get("campaign.starting.plus_bullets", "+ Bullets");

            if (plusLivesButton != null)
                plusLivesButton.interactable = !adRequestInFlight;

            if (extraActionButton != null)
                extraActionButton.interactable = !adRequestInFlight;
        }

        public IEnumerator PlayCountdown()
        {
            EnsureReferences();
            startRequested = false;
            StopIntroSequence();
            ResetIntroVisuals();

            if (modalRoot != null)
            {
                modalRoot.transform.SetAsLastSibling();
                EnsureAnimator();

                if (modalAnimator != null)
                    modalAnimator.Show();
                else
                    modalRoot.SetActive(true);
            }

            introSequenceCoroutine = StartCoroutine(PlayIntroSequence());

            if (startButton != null)
                startButton.onClick.AddListener(HandleStartClicked);

            if (plusLivesButton != null)
                plusLivesButton.onClick.AddListener(HandlePlusLivesClicked);

            if (extraActionButton != null)
                extraActionButton.onClick.AddListener(HandleExtraActionClicked);

            TryOfferMercyBoost();

            while (!startRequested)
                yield return null;

            if (startButton != null)
                startButton.onClick.RemoveListener(HandleStartClicked);

            if (plusLivesButton != null)
                plusLivesButton.onClick.RemoveListener(HandlePlusLivesClicked);

            if (extraActionButton != null)
                extraActionButton.onClick.RemoveListener(HandleExtraActionClicked);

            if (modalRoot != null)
            {
                StopIntroSequence();
                EnsureAnimator();

                if (modalAnimator != null)
                    modalAnimator.Hide();
                else
                    modalRoot.SetActive(false);
            }
        }

        private void EnsureReferences()
        {
            if (modalRoot == null)
                modalRoot = gameObject;

            EnsureAnimator();

            Image rootImage = modalRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = true;
                rootImage.raycastTarget = true;
            }

            if (titleText != null && !hasCapturedTitleBaseScale)
            {
                titleBaseScale = titleText.rectTransform.localScale;
                hasCapturedTitleBaseScale = true;
            }

            if (plusLivesButton != null && !hasCapturedPlusLivesButtonBaseScale)
            {
                plusLivesButtonBaseScale = plusLivesButton.transform.localScale;
                hasCapturedPlusLivesButtonBaseScale = true;
            }

            if (plusLivesButton != null && plusLivesButtonCanvasGroup == null)
                plusLivesButtonCanvasGroup = plusLivesButton.GetComponent<CanvasGroup>() ?? plusLivesButton.gameObject.AddComponent<CanvasGroup>();

            if (extraActionButton != null && !hasCapturedExtraActionButtonBaseScale)
            {
                extraActionButtonBaseScale = extraActionButton.transform.localScale;
                hasCapturedExtraActionButtonBaseScale = true;
            }

            if (extraActionButton != null && extraActionButtonCanvasGroup == null)
                extraActionButtonCanvasGroup = extraActionButton.GetComponent<CanvasGroup>() ?? extraActionButton.gameObject.AddComponent<CanvasGroup>();

            if (startButton != null && !hasCapturedStartButtonBaseScale)
            {
                startButtonBaseScale = startButton.transform.localScale;
                hasCapturedStartButtonBaseScale = true;
            }

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

        private void HandleStartClicked()
        {
            startRequested = true;
        }

        private void StartIntroSequence()
        {
            StopIntroSequence();
            ResetIntroVisuals();
            introSequenceCoroutine = StartCoroutine(PlayIntroSequence());
        }

        private void StopIntroSequence()
        {
            if (introSequenceCoroutine != null)
            {
                StopCoroutine(introSequenceCoroutine);
                introSequenceCoroutine = null;
            }

            RestoreFinalIntroState();
        }

        private void ResetIntroVisuals()
        {
            if (titleText != null)
            {
                titleText.text = configuredTitleText;
                titleText.alpha = 0f;
                titleText.rectTransform.localScale = titleBaseScale * titleBangStartScale;
            }

            if (briefingText != null)
                briefingText.text = string.Empty;

            SetButtonIntroState(plusLivesButton, plusLivesButtonCanvasGroup, plusLivesButtonBaseScale, false);
            SetButtonIntroState(extraActionButton, extraActionButtonCanvasGroup, extraActionButtonBaseScale, false);
            SetStartButtonIntroState(false);
        }

        private void RestoreFinalIntroState()
        {
            if (titleText != null)
            {
                titleText.text = configuredTitleText;
                titleText.alpha = 1f;
                titleText.rectTransform.localScale = titleBaseScale;
            }

            if (briefingText != null)
                briefingText.text = configuredBriefingText;

            SetButtonIntroState(plusLivesButton, plusLivesButtonCanvasGroup, plusLivesButtonBaseScale, true);
            SetButtonIntroState(extraActionButton, extraActionButtonCanvasGroup, extraActionButtonBaseScale, true);
            SetStartButtonIntroState(true);
        }

        private IEnumerator PlayIntroSequence()
        {
            float modalShowDuration = modalAnimator != null ? modalAnimator.showDuration : 0f;
            if (modalShowDuration > 0f)
                yield return new WaitForSecondsRealtime(modalShowDuration);

            if (titleRevealDelay > 0f)
                yield return new WaitForSecondsRealtime(titleRevealDelay);

            yield return PlayTitleBangAnimation();
            yield return TypeBriefing(configuredBriefingText);
            yield return PlayStartButtonBangAnimation();

            if (buttonRevealStagger > 0f)
                yield return new WaitForSecondsRealtime(buttonRevealStagger);

            yield return PlayButtonsBangAnimation(
                plusLivesButton, plusLivesButtonCanvasGroup, plusLivesButtonBaseScale,
                extraActionButton, extraActionButtonCanvasGroup, extraActionButtonBaseScale);

            introSequenceCoroutine = null;
        }

        private IEnumerator PlayTitleBangAnimation()
        {
            if (titleText == null)
                yield break;

            titleText.text = configuredTitleText;
            titleText.alpha = 1f;

            float duration = Mathf.Max(0.01f, titleBangDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBackStrong01(t);
                float scale = Mathf.LerpUnclamped(titleBangStartScale, titleBangOvershootScale, eased);
                titleText.rectTransform.localScale = titleBaseScale * scale;
                yield return null;
            }

            titleText.rectTransform.localScale = titleBaseScale;
        }

        private IEnumerator PlayButtonBangAnimation(Button button, CanvasGroup canvasGroup, Vector3 baseScale)
        {
            if (button == null)
                yield break;

            SetCanvasGroupVisible(canvasGroup, true);
            button.transform.localScale = baseScale * buttonBangStartScale;

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

        private IEnumerator PlayStartButtonBangAnimation()
        {
            if (startButton == null)
                yield break;

            SetStartButtonIntroState(true);
            startButton.transform.localScale = startButtonBaseScale * buttonBangStartScale;

            float duration = Mathf.Max(0.01f, buttonBangDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBackStrong01(t);
                float scale = Mathf.LerpUnclamped(buttonBangStartScale, buttonBangOvershootScale, eased);
                startButton.transform.localScale = startButtonBaseScale * scale;
                yield return null;
            }

            startButton.transform.localScale = startButtonBaseScale;
        }

        private IEnumerator PlayButtonsBangAnimation(
            Button firstButton, CanvasGroup firstCanvasGroup, Vector3 firstBaseScale,
            Button secondButton, CanvasGroup secondCanvasGroup, Vector3 secondBaseScale)
        {
            if (firstButton == null && secondButton == null)
                yield break;

            if (firstButton != null)
            {
                SetCanvasGroupVisible(firstCanvasGroup, true);
                firstButton.transform.localScale = firstBaseScale * buttonBangStartScale;
            }

            if (secondButton != null)
            {
                SetCanvasGroupVisible(secondCanvasGroup, true);
                secondButton.transform.localScale = secondBaseScale * buttonBangStartScale;
            }

            float duration = Mathf.Max(0.01f, buttonBangDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBackStrong01(t);
                float scale = Mathf.LerpUnclamped(buttonBangStartScale, buttonBangOvershootScale, eased);

                if (firstButton != null)
                    firstButton.transform.localScale = firstBaseScale * scale;

                if (secondButton != null)
                    secondButton.transform.localScale = secondBaseScale * scale;

                yield return null;
            }

            if (firstButton != null)
                firstButton.transform.localScale = firstBaseScale;

            if (secondButton != null)
                secondButton.transform.localScale = secondBaseScale;
        }

        private IEnumerator TypeBriefing(string fullText)
        {
            if (briefingText == null)
                yield break;

            briefingText.text = string.Empty;

            if (string.IsNullOrEmpty(fullText))
                yield break;

            float secondsPerCharacter = 1f / Mathf.Max(1f, briefingTypingCharsPerSecond);
            for (int i = 0; i < fullText.Length; i++)
            {
                briefingText.text += fullText[i];
                yield return new WaitForSecondsRealtime(secondsPerCharacter);

                if (IsPunctuation(fullText[i]))
                    yield return new WaitForSecondsRealtime(briefingPunctuationPause);
            }
        }

        private static bool IsPunctuation(char c)
        {
            return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':';
        }

        private static float EaseOutBackStrong01(float t)
        {
            t = Mathf.Clamp01(t);
            float c1 = 1.7f;
            float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + (c3 * x * x * x) + (c1 * x * x);
        }

        private void SetButtonIntroState(Button button, CanvasGroup canvasGroup, Vector3 baseScale, bool visible)
        {
            if (button == null)
                return;

            SetCanvasGroupVisible(canvasGroup, visible);
            button.transform.localScale = visible ? baseScale : baseScale * buttonBangStartScale;
        }

        private void SetStartButtonIntroState(bool visible)
        {
            if (startButton == null)
                return;

            startButton.transform.localScale = visible
                ? startButtonBaseScale
                : startButtonBaseScale * buttonBangStartScale;
        }

        private static void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void HandlePlusLivesClicked()
        {
            if (plusLivesUsedThisStage || adRequestInFlight)
                return;

            adRequestInFlight = true;
            RefreshAdButtonsState();
            PauseGameAudioForAd();
            RewardedAdService.Instance.ShowRewardedAd(
                RewardedAdPlacement.CampaignStartPlusLives,
                "campaign_start_plus_lives",
                adResult =>
                {
                    ResumeGameAudioAfterAd();
                    adRequestInFlight = false;

                    if (adResult != RewardedAdResult.Completed)
                    {
                        RefreshAdButtonsState();
                        return;
                    }

                    if (GameManager.Instance != null)
                        GameManager.Instance.AddBonusLives(2);

                    plusLivesUsedThisStage = true;
                    RefreshAdButtonsState();
                });
        }

        private void HandleExtraActionClicked()
        {
            if (extraActionUsedThisStage || adRequestInFlight)
                return;

            adRequestInFlight = true;
            RefreshAdButtonsState();
            PauseGameAudioForAd();
            RewardedAdService.Instance.ShowRewardedAd(
                RewardedAdPlacement.CampaignStartPlusBullets,
                "campaign_start_plus_bullets",
                adResult =>
                {
                    ResumeGameAudioAfterAd();
                    adRequestInFlight = false;

                    if (adResult != RewardedAdResult.Completed)
                    {
                        RefreshAdButtonsState();
                        return;
                    }

                    ShooterController shooterController = FindObjectOfType<ShooterController>();
                    if (shooterController != null)
                        shooterController.ApplyConfiguredStageAmmoBonusToAllWeapons();

                    extraActionUsedThisStage = true;
                    RefreshAdButtonsState();
                });
        }

        private void TryOfferMercyBoost()
        {
            if (mercyBonusProcessedThisStage)
                return;

            GameManager manager = GameManager.Instance;
            if (manager == null || manager.ConsecutiveFailedRuns < 2)
                return;

            mercyBonusProcessedThisStage = true;
            adRequestInFlight = true;
            RefreshAdButtonsState();
            PauseGameAudioForAd();
            RewardedAdService.Instance.ShowRewardedAd(
                RewardedAdPlacement.CampaignStartPlusLives,
                "campaign_start_mercy_boost",
                adResult =>
                {
                    ResumeGameAudioAfterAd();
                    adRequestInFlight = false;

                    if (adResult == RewardedAdResult.Completed)
                    {
                        manager.AddBonusLives(2);
                        manager.ResetConsecutiveFailedRuns();
                        plusLivesUsedThisStage = true;
                        RefreshAdButtonsState();
                        return;
                    }

                    RefreshAdButtonsState();
                });
        }

        private void RefreshAdButtonsState()
        {
            if (plusLivesButton != null)
                plusLivesButton.interactable = !plusLivesUsedThisStage && !adRequestInFlight;

            if (extraActionButton != null)
                extraActionButton.interactable = !extraActionUsedThisStage && !adRequestInFlight;
        }

        private static void PauseGameAudioForAd()
        {
            AudioListener.pause = true;
        }

        private static void ResumeGameAudioAfterAd()
        {
            AudioListener.pause = false;
        }
    }
}
