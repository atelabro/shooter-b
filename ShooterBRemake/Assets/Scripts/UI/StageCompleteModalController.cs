using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class StageCompleteModalController : MonoBehaviour
    {
        private const string ButtonRevealResourcePath = "Audio/small_drum";

        private sealed class StageCompleteModalActionRunner : MonoBehaviour
        {
        }

        private const float ContinueMapTransitionMinDelaySeconds = 1f;

        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Text")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI stageNameText;
        public TextMeshProUGUI restartButtonText;
        public TextMeshProUGUI backButtonText;
        public TextMeshProUGUI continueButtonText;

        [Header("Star Icons")]
        public Image[] starIcons;
        public Sprite filledStarSprite;
        public Sprite emptyStarSprite;

        [Header("Star Reveal Audio")]
        public AudioClip earnedStarClip;
        public AudioClip emptyStarClip;

        [Header("Star Reveal Timing")]
        [Min(0f)] public float starRevealInitialDelaySeconds = 0.14f;
        [Min(0f)] public float starRevealStepDelaySeconds = 0.34f;
        [Min(0.01f)] public float starPopDurationSeconds = 0.32f;
        [Range(0.2f, 1.5f)] public float starIdleScale = 0.72f;
        [Range(1f, 2.5f)] public float starPopScale = 1.22f;

        [Header("Title Animation")]
        [Min(0f)] public float titleRevealDelay = 0.08f;
        [Min(0.01f)] public float titleDropDuration = 0.65f;
        [Min(0.01f)] public float titleWobbleDuration = 0.35f;
        [Range(0.5f, 1.2f)] public float titleStartScale = 0.72f;
        [Range(0.8f, 1.5f)] public float titleOvershootScale = 1.18f;
        public Vector2 titleStartOffset = new Vector2(0f, 120f);
        public float titleWobbleAngle = 8f;

        [Header("Button Reveal Timing")]
        [Min(0f)] public float buttonRevealInitialDelaySeconds = 0.08f;
        [Min(0f)] public float buttonRevealStepDelaySeconds = 0.06f;
        [Min(0.01f)] public float buttonBangDurationSeconds = 0.18f;
        [Range(0.5f, 1.2f)] public float buttonBangStartScale = 0.78f;
        [Range(0.8f, 1.4f)] public float buttonBangOvershootScale = 1.08f;

        [Header("Buttons")]
        public Button restartButton;
        public Button backButton;
        public Button continueButton;
        public Button menuButton;
        private Button legacyBackFallbackButton;
        private StageConfig lastShownStage;
        private bool shouldShowCityFirstCompletionAdOnContinue;
        private CityConfig cityForOneTimeCompletionAd;
        private bool continueFlowInProgress;
        private bool isInitialized;
        private ModalDialogAnimator modalAnimator;
        private AudioSource starAudioSource;
        private AudioClip buttonRevealClip;
        private Coroutine starRevealRoutine;
        private Coroutine buttonRevealRoutine;
        private Coroutine closeRoutine;
        private static StageCompleteModalActionRunner actionRunner;
        private readonly Vector3[] starBaseScales = new Vector3[3];
        private int lastResolvedStars;
        private Vector3 titleBaseScale = Vector3.one;
        private Vector2 titleBaseAnchoredPosition = Vector2.zero;
        private bool hasCapturedTitleBaseScale;
        private bool hasCapturedTitleBaseAnchoredPosition;
        private Vector3 restartButtonBaseScale = Vector3.one;
        private bool hasCapturedRestartButtonBaseScale;
        private Vector3 backButtonBaseScale = Vector3.one;
        private bool hasCapturedBackButtonBaseScale;
        private Vector3 continueButtonBaseScale = Vector3.one;
        private bool hasCapturedContinueButtonBaseScale;
        private Graphic[] restartButtonGraphics;
        private Graphic[] backButtonGraphics;
        private Graphic[] continueButtonGraphics;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (!isInitialized)
                return;

            if (starRevealRoutine != null)
                StopCoroutine(starRevealRoutine);

            if (buttonRevealRoutine != null)
                StopCoroutine(buttonRevealRoutine);

            CancelPendingCloseTransition();
            StopTitleIntroState(true);

            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartClicked);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);

            if (legacyBackFallbackButton != null && legacyBackFallbackButton != backButton)
                legacyBackFallbackButton.onClick.RemoveListener(OnBackClicked);

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;

            if (AudioSettingsManager.HasInstance)
                AudioSettingsManager.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
        }

        public void Show(StageConfig config, long score)
        {
            int stars = CampaignProgressManager.Instance.CalculateStars(config, score);
            CampaignProgressManager.Instance.SaveStageStars(config.stageIndex, stars);
            ShowResolved(config, stars, true);
        }

        public void ShowDebug(StageConfig config, int forcedStars)
        {
            ShowResolved(config, Mathf.Clamp(forcedStars, 0, GetStarIconCount()), false);
        }

        private void ShowResolved(StageConfig config, int stars, bool evaluateProgression)
        {
            EnsureInitialized();
            EnsureModalRoot();
            lastShownStage = config;
            shouldShowCityFirstCompletionAdOnContinue = false;
            cityForOneTimeCompletionAd = null;
            lastResolvedStars = Mathf.Clamp(stars, 0, GetStarIconCount());

            if (evaluateProgression)
                EvaluateCityCompletionAdEligibility(config);

            if (stageNameText != null)
                stageNameText.text = CampaignLocalizationResolver.GetStageName(config);
            ResetStarIconsForReveal();
            StopTitleIntroState(false);
            ResetTitleIntroVisuals();
            StopButtonRevealSequence(false);
            ResetButtonIntroVisuals();

            if (modalRoot != null)
            {
                EnsureAnimator();

                if (modalAnimator != null)
                    modalAnimator.Show();
                else
                    modalRoot.SetActive(true);
            }

            StartStarRevealSequence();
        }

        private void ApplyStarIcons(int earnedStars)
        {
            if (starIcons == null)
                return;

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] == null)
                    continue;

                bool earned = i < earnedStars;
                starIcons[i].sprite = earned ? filledStarSprite : emptyStarSprite;
                starIcons[i].enabled = earned ? filledStarSprite != null : emptyStarSprite != null;
            }
        }

        public void Hide()
        {
            EnsureInitialized();
            EnsureModalRoot();
            StopTitleIntroState(true);
            StopButtonRevealSequence(true);

            if (modalRoot != null)
            {
                EnsureAnimator();

                if (modalAnimator != null)
                    modalAnimator.Hide();
                else
                    modalRoot.SetActive(false);
            }

            StopStarRevealSequence();
            ResetStarTransforms();
        }

        public void OnRestartClicked()
        {
            StartCloseTransition(() =>
            {
                Time.timeScale = 1f;
                SceneController.Instance.ReloadCurrentGameScene();
            });
        }

        public void OnBackClicked()
        {
            StartCloseTransition(() =>
            {
                Time.timeScale = 1f;
                SceneController.Instance.LoadCampaignMapScene();
            });
        }

        public void OnContinueClicked()
        {
            if (continueFlowInProgress)
                return;

            if (shouldShowCityFirstCompletionAdOnContinue && cityForOneTimeCompletionAd != null)
            {
                continueFlowInProgress = true;
                SetNavigationButtonsInteractable(false);
                Time.timeScale = 1f;
                RewardedAdService.Instance.ShowRewardedAd(
                    RewardedAdPlacement.CityFirstCompletion,
                    "city_first_completion_continue",
                    result =>
                    {
                        CampaignProgressManager.Instance.MarkCityFirstCompletionAdShown(cityForOneTimeCompletionAd);
                        GameLog.Log($"[StageComplete] City first-completion ad attempted for {cityForOneTimeCompletionAd.cityName}. Result={result}");
                        shouldShowCityFirstCompletionAdOnContinue = false;
                        cityForOneTimeCompletionAd = null;
                        continueFlowInProgress = false;
                        SetNavigationButtonsInteractable(true);
                        StartCloseTransition(ContinueAfterAdGate);
                    });
                return;
            }

            StartCloseTransition(ContinueAfterAdGate);
        }

        private void ContinueAfterAdGate()
        {
            StageConfig activeStage = CampaignProgressManager.Instance.ActiveStageConfig;
            StageConfig nextStage = CampaignProgressManager.Instance.GetNextStageInActiveCityRow();
            CityConfig activeCity = CampaignProgressManager.Instance.ActiveCityConfig;
            CityConfig[] allCities = CampaignProgressManager.Instance.CampaignCities;

            if (activeCity == null || allCities == null)
            {
                CampaignProgressManager.Instance.ClearPendingMapFocusTransition();
                SceneController.Instance.LoadCampaignMapScene();
                return;
            }

            if (nextStage == null)
            {
                CityConfig nextCity = FindNextUnlockedCity(activeCity, allCities);
                if (nextCity != null && nextCity.stages != null && nextCity.stages.Length > 0 && nextCity.stages[0] != null && activeStage != null)
                {
                    CampaignProgressManager.Instance.SetPendingMapFocusTransition(
                        activeCity,
                        activeStage,
                        nextCity,
                        nextCity.stages[0],
                        ContinueMapTransitionMinDelaySeconds);
                }
                else
                {
                    CampaignProgressManager.Instance.ClearPendingMapFocusTransition();
                }

                SceneController.Instance.LoadCampaignMapScene();
                return;
            }

            if (!CampaignProgressManager.Instance.IsStageUnlocked(nextStage, activeCity, allCities))
            {
                CampaignProgressManager.Instance.ClearPendingMapFocusTransition();
                SceneController.Instance.LoadCampaignMapScene();
                return;
            }

            CampaignProgressManager.Instance.ClearPendingMapFocusTransition();
            CampaignProgressManager.Instance.SetActiveCampaignLocation(activeCity, nextStage);
            SceneController.Instance.LoadCampaignStage(nextStage);
        }

        public void OnMenuClicked()
        {
            OnBackClicked();
        }

        private void EvaluateCityCompletionAdEligibility(StageConfig completedStage)
        {
            CampaignProgressManager progress = CampaignProgressManager.Instance;
            if (progress == null || completedStage == null)
                return;

            CityConfig completedCity = ResolveCityForStage(completedStage);
            if (completedCity == null)
                return;

            bool cityCompletedNow = progress.IsCityCompleted(completedCity);
            if (!cityCompletedNow)
                return;

            if (progress.HasShownCityFirstCompletionAd(completedCity))
                return;

            shouldShowCityFirstCompletionAdOnContinue = true;
            cityForOneTimeCompletionAd = completedCity;
        }

        private void SetNavigationButtonsInteractable(bool interactable)
        {
            if (restartButton != null)
                restartButton.interactable = interactable;

            if (backButton != null)
                backButton.interactable = interactable;

            if (continueButton != null)
                continueButton.interactable = interactable;

            if (legacyBackFallbackButton != null)
                legacyBackFallbackButton.interactable = interactable;
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
            ResolveButtons();
            ResolveTextReferences();
            EnsureStarAudioReady();
            CaptureTitleAnimationDefaults();
            CaptureStarBaseScales();
            CaptureButtonAnimationDefaults();
            RefreshLocalizedTexts();

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (legacyBackFallbackButton != null && legacyBackFallbackButton != backButton)
                legacyBackFallbackButton.onClick.AddListener(OnBackClicked);

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

        private void ResolveButtons()
        {
            if (continueButton == null && menuButton != null && menuButton.gameObject.name.Contains("Continue"))
            {
                continueButton = menuButton;
                menuButton = null;
            }

            if (backButton == null)
                backButton = FindButtonByName("BackButton");

            if (continueButton == null)
                continueButton = FindButtonByName("ContinueButton");

            legacyBackFallbackButton = menuButton;
        }

        private void ResolveTextReferences()
        {
            if (titleText == null)
                titleText = FindTextByContent("Stage Complete");

            if (restartButtonText == null && restartButton != null)
                restartButtonText = restartButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (backButtonText == null && backButton != null)
                backButtonText = backButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (continueButtonText == null && continueButton != null)
                continueButtonText = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void RefreshLocalizedTexts()
        {
            if (titleText != null)
                titleText.text = LocalizationManager.Instance.Get("campaign.stage_complete.title", "Stage Complete");

            if (restartButtonText != null)
                restartButtonText.text = LocalizationManager.Instance.Get("common.restart", "Restart");

            if (backButtonText != null)
                backButtonText.text = LocalizationManager.Instance.Get("common.back", "Back");

            if (continueButtonText != null)
                continueButtonText.text = LocalizationManager.Instance.Get("common.continue", "Continue");
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedTexts();

            if (stageNameText != null && lastShownStage != null)
                stageNameText.text = CampaignLocalizationResolver.GetStageName(lastShownStage);
        }

        private void HandleAudioSettingsChanged()
        {
            if (starAudioSource != null)
                starAudioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
        }

        private void EnsureStarAudioReady()
        {
            if (starAudioSource == null)
            {
                starAudioSource = GetComponent<AudioSource>();
                if (starAudioSource == null)
                    starAudioSource = gameObject.AddComponent<AudioSource>();

                starAudioSource.playOnAwake = false;
                starAudioSource.loop = false;
            }

            starAudioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();

            if (buttonRevealClip == null)
                buttonRevealClip = Resources.Load<AudioClip>(ButtonRevealResourcePath);
        }

        private void CaptureStarBaseScales()
        {
            if (starIcons == null)
                return;

            int count = Mathf.Min(starIcons.Length, starBaseScales.Length);
            for (int i = 0; i < count; i++)
            {
                if (starIcons[i] == null)
                    continue;

                RectTransform rectTransform = starIcons[i].rectTransform;
                if (rectTransform != null)
                    starBaseScales[i] = rectTransform.localScale;
            }
        }

        private void CaptureTitleAnimationDefaults()
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
        }

        private void CaptureButtonAnimationDefaults()
        {
            if (restartButton != null && !hasCapturedRestartButtonBaseScale)
            {
                restartButtonBaseScale = restartButton.transform.localScale;
                hasCapturedRestartButtonBaseScale = true;
            }

            if (backButton != null && !hasCapturedBackButtonBaseScale)
            {
                backButtonBaseScale = backButton.transform.localScale;
                hasCapturedBackButtonBaseScale = true;
            }

            if (continueButton != null && !hasCapturedContinueButtonBaseScale)
            {
                continueButtonBaseScale = continueButton.transform.localScale;
                hasCapturedContinueButtonBaseScale = true;
            }

            if (restartButton != null && restartButtonGraphics == null)
                restartButtonGraphics = GetButtonGraphics(restartButton);

            if (backButton != null && backButtonGraphics == null)
                backButtonGraphics = GetButtonGraphics(backButton);

            if (continueButton != null && continueButtonGraphics == null)
                continueButtonGraphics = GetButtonGraphics(continueButton);
        }

        private void ResetStarIconsForReveal()
        {
            if (starIcons == null)
                return;

            CaptureStarBaseScales();

            for (int i = 0; i < starIcons.Length; i++)
            {
                Image icon = starIcons[i];
                if (icon == null)
                    continue;

                icon.sprite = emptyStarSprite;
                icon.enabled = emptyStarSprite != null;

                RectTransform rectTransform = icon.rectTransform;
                if (rectTransform != null)
                    rectTransform.localScale = GetBaseStarScale(i) * starIdleScale;
            }
        }

        private void ResetStarTransforms()
        {
            if (starIcons == null)
                return;

            for (int i = 0; i < starIcons.Length; i++)
            {
                Image icon = starIcons[i];
                if (icon == null)
                    continue;

                RectTransform rectTransform = icon.rectTransform;
                if (rectTransform != null)
                    rectTransform.localScale = GetBaseStarScale(i);
            }
        }

        private void StartStarRevealSequence()
        {
            StopStarRevealSequence();
            StopButtonRevealSequence(false);
            StartTitleRevealSequence();

            if (starIcons == null || starIcons.Length == 0)
            {
                StartButtonRevealSequence();
                return;
            }

            starRevealRoutine = StartCoroutine(PlayStarRevealSequence());
        }

        private void StopStarRevealSequence()
        {
            if (starRevealRoutine == null)
                return;

            StopCoroutine(starRevealRoutine);
            starRevealRoutine = null;
        }

        private void StartTitleRevealSequence()
        {
            if (titleText == null)
                return;

            StartCoroutine(PlayTitleRevealSequence());
        }

        private void StopTitleIntroState(bool restoreFinalState)
        {
            StopAllCoroutines();
            starRevealRoutine = null;
            buttonRevealRoutine = null;

            if (restoreFinalState)
                RestoreFinalTitleIntroState();
        }

        private void ResetTitleIntroVisuals()
        {
            CaptureTitleAnimationDefaults();

            if (titleText == null)
                return;

            RectTransform titleRect = titleText.rectTransform;
            titleText.alpha = 0f;
            titleRect.localScale = titleBaseScale * titleStartScale;
            titleRect.anchoredPosition = titleBaseAnchoredPosition + titleStartOffset;
            titleRect.localRotation = Quaternion.Euler(0f, 0f, -titleWobbleAngle);
        }

        private void RestoreFinalTitleIntroState()
        {
            if (titleText == null)
                return;

            RectTransform titleRect = titleText.rectTransform;
            titleText.alpha = 1f;
            titleRect.localScale = titleBaseScale;
            titleRect.anchoredPosition = titleBaseAnchoredPosition;
            titleRect.localRotation = Quaternion.identity;
        }

        private IEnumerator PlayStarRevealSequence()
        {
            if (starRevealInitialDelaySeconds > 0f)
                yield return WaitForUnscaledSeconds(starRevealInitialDelaySeconds);

            int slotCount = GetStarIconCount();
            for (int i = 0; i < slotCount; i++)
            {
                Image icon = starIcons[i];
                if (icon == null)
                    continue;

                bool earned = i < lastResolvedStars;
                icon.sprite = earned ? filledStarSprite : emptyStarSprite;
                icon.enabled = earned ? filledStarSprite != null : emptyStarSprite != null;

                PlayStarClip(earned ? earnedStarClip : emptyStarClip);
                yield return AnimateStarPop(icon.rectTransform, i);

                if (i < slotCount - 1 && starRevealStepDelaySeconds > 0f)
                    yield return WaitForUnscaledSeconds(starRevealStepDelaySeconds);
            }

            starRevealRoutine = null;
            StartButtonRevealSequence();
        }

        private IEnumerator PlayTitleRevealSequence()
        {
            float modalShowDuration = modalAnimator != null ? modalAnimator.showDuration : 0f;
            if (modalShowDuration > 0f)
                yield return WaitForUnscaledSeconds(modalShowDuration);

            if (titleRevealDelay > 0f)
                yield return WaitForUnscaledSeconds(titleRevealDelay);

            yield return PlayTitleDropAnimation();
            RestoreFinalTitleIntroState();
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

        private IEnumerator AnimateStarPop(RectTransform rectTransform, int index)
        {
            if (rectTransform == null)
                yield break;

            Vector3 baseScale = GetBaseStarScale(index);
            float duration = Mathf.Max(0.01f, starPopDurationSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleMultiplier = EvaluateStarPopScale(t);
                rectTransform.localScale = baseScale * scaleMultiplier;
                yield return null;
            }

            rectTransform.localScale = baseScale;
        }

        private void PlayStarClip(AudioClip clip)
        {
            EnsureStarAudioReady();
            if (starAudioSource == null || clip == null)
                return;

            starAudioSource.PlayOneShot(clip);
        }

        private void PlayButtonRevealClip()
        {
            EnsureStarAudioReady();
            if (starAudioSource == null || buttonRevealClip == null)
                return;

            starAudioSource.PlayOneShot(buttonRevealClip);
        }

        private void StartButtonRevealSequence()
        {
            StopButtonRevealSequence(false);
            buttonRevealRoutine = StartCoroutine(PlayButtonRevealSequence());
        }

        private void StopButtonRevealSequence(bool restoreFinalState)
        {
            if (buttonRevealRoutine != null)
            {
                StopCoroutine(buttonRevealRoutine);
                buttonRevealRoutine = null;
            }

            if (restoreFinalState)
                RestoreFinalButtonIntroState();
        }

        private void ResetButtonIntroVisuals()
        {
            CaptureButtonAnimationDefaults();
            SetButtonIntroState(restartButton, restartButtonGraphics, restartButtonBaseScale, false, buttonBangStartScale);
            SetButtonIntroState(backButton, backButtonGraphics, backButtonBaseScale, false, buttonBangStartScale);
            SetButtonIntroState(continueButton, continueButtonGraphics, continueButtonBaseScale, false, buttonBangStartScale);
        }

        private void RestoreFinalButtonIntroState()
        {
            SetButtonIntroState(restartButton, restartButtonGraphics, restartButtonBaseScale, true, buttonBangStartScale);
            SetButtonIntroState(backButton, backButtonGraphics, backButtonBaseScale, true, buttonBangStartScale);
            SetButtonIntroState(continueButton, continueButtonGraphics, continueButtonBaseScale, true, buttonBangStartScale);
        }

        private IEnumerator PlayButtonRevealSequence()
        {
            if (buttonRevealInitialDelaySeconds > 0f)
                yield return WaitForUnscaledSeconds(buttonRevealInitialDelaySeconds);

            yield return PlayButtonBangAnimation(restartButton, restartButtonGraphics, restartButtonBaseScale);

            if (buttonRevealStepDelaySeconds > 0f)
                yield return WaitForUnscaledSeconds(buttonRevealStepDelaySeconds);

            yield return PlayButtonBangAnimation(backButton, backButtonGraphics, backButtonBaseScale);

            if (buttonRevealStepDelaySeconds > 0f)
                yield return WaitForUnscaledSeconds(buttonRevealStepDelaySeconds);

            yield return PlayButtonBangAnimation(continueButton, continueButtonGraphics, continueButtonBaseScale);
            RestoreFinalButtonIntroState();
            buttonRevealRoutine = null;
        }

        private IEnumerator PlayButtonBangAnimation(Button button, IReadOnlyList<Graphic> graphics, Vector3 baseScale)
        {
            if (button == null)
                yield break;

            SetGraphicsVisible(graphics, true);
            button.interactable = true;
            button.transform.localScale = baseScale * buttonBangStartScale;
            PlayButtonRevealClip();

            float duration = Mathf.Max(0.01f, buttonBangDurationSeconds);
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

        private IEnumerator WaitForUnscaledSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private float EvaluateStarPopScale(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.45f)
                return Mathf.LerpUnclamped(starIdleScale, starPopScale, t / 0.45f);

            return Mathf.LerpUnclamped(starPopScale, 1f, (t - 0.45f) / 0.55f);
        }

        private static float EaseOutBackStrong01(float t)
        {
            t = Mathf.Clamp01(t);
            float c1 = 1.7f;
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

        private static float EaseOutCubic01(float t)
        {
            t = Mathf.Clamp01(t);
            float inverse = 1f - t;
            return 1f - (inverse * inverse * inverse);
        }

        private Vector3 GetBaseStarScale(int index)
        {
            if (index < 0 || index >= starBaseScales.Length || starBaseScales[index] == Vector3.zero)
                return Vector3.one;

            return starBaseScales[index];
        }

        private int GetStarIconCount()
        {
            return starIcons == null ? 0 : starIcons.Length;
        }

        private void StartCloseTransition(System.Action onClosed)
        {
            EnsureInitialized();
            EnsureModalRoot();

            CancelPendingCloseTransition();

            SetNavigationButtonsInteractable(false);
            float delay = 0f;
            if (modalRoot != null)
            {
                StopButtonRevealSequence(true);
                EnsureAnimator();
                delay = modalAnimator != null ? modalAnimator.HideWithDelay() : 0f;
                if (modalAnimator == null)
                    modalRoot.SetActive(false);
            }

            closeRoutine = EnsureActionRunner().StartCoroutine(InvokeAfterDelay(delay, () =>
            {
                closeRoutine = null;
                SetNavigationButtonsInteractable(true);
                onClosed?.Invoke();
            }));
        }

        private static StageCompleteModalActionRunner EnsureActionRunner()
        {
            if (actionRunner != null)
                return actionRunner;

            GameObject runnerObject = new GameObject("StageCompleteModalActionRunner");
            DontDestroyOnLoad(runnerObject);
            actionRunner = runnerObject.AddComponent<StageCompleteModalActionRunner>();
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

        private Button FindButtonByName(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
                return null;

            if (modalRoot == null)
                return null;

            Button[] buttons = modalRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == buttonName)
                    return buttons[i];
            }

            return null;
        }

        private static void SetButtonIntroState(Button button, IReadOnlyList<Graphic> graphics, Vector3 baseScale, bool visible, float hiddenScale)
        {
            if (button == null)
                return;

            SetGraphicsVisible(graphics, visible);
            button.interactable = visible;
            button.transform.localScale = visible ? baseScale : baseScale * hiddenScale;
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

        private static CityConfig FindNextUnlockedCity(CityConfig activeCity, CityConfig[] allCities)
        {
            if (activeCity == null || allCities == null)
                return null;

            int activeCityIndex = System.Array.IndexOf(allCities, activeCity);
            if (activeCityIndex < 0)
                return null;

            for (int i = activeCityIndex + 1; i < allCities.Length; i++)
            {
                CityConfig candidateCity = allCities[i];
                if (candidateCity == null || candidateCity.stages == null || candidateCity.stages.Length == 0 || candidateCity.stages[0] == null)
                    continue;

                if (!CampaignProgressManager.Instance.IsCityUnlocked(candidateCity, allCities))
                    continue;

                if (!CampaignProgressManager.Instance.IsStageUnlocked(candidateCity.stages[0], candidateCity, allCities))
                    continue;

                return candidateCity;
            }

            return null;
        }

        private static CityConfig ResolveCityForStage(StageConfig stage)
        {
            if (stage == null)
                return null;

            CampaignProgressManager progress = CampaignProgressManager.Instance;
            if (progress == null)
                return null;

            CityConfig activeCity = progress.ActiveCityConfig;
            if (activeCity != null && activeCity.stages != null && System.Array.IndexOf(activeCity.stages, stage) >= 0)
                return activeCity;

            CityConfig[] allCities = progress.CampaignCities;
            if (allCities == null)
                return null;

            for (int i = 0; i < allCities.Length; i++)
            {
                CityConfig city = allCities[i];
                if (city == null || city.stages == null)
                    continue;

                if (System.Array.IndexOf(city.stages, stage) >= 0)
                    return city;
            }

            return null;
        }
    }
}
