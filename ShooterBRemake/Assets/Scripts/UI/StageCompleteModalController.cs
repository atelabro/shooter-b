using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class StageCompleteModalController : MonoBehaviour
    {
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
        private Coroutine starRevealRoutine;
        private Coroutine closeRoutine;
        private readonly Vector3[] starBaseScales = new Vector3[3];
        private int lastResolvedStars;

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

            if (closeRoutine != null)
                StopCoroutine(closeRoutine);

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
            CaptureStarBaseScales();
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

            if (starIcons == null || starIcons.Length == 0)
                return;

            starRevealRoutine = StartCoroutine(PlayStarRevealSequence());
        }

        private void StopStarRevealSequence()
        {
            if (starRevealRoutine == null)
                return;

            StopCoroutine(starRevealRoutine);
            starRevealRoutine = null;
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

            if (closeRoutine != null)
                StopCoroutine(closeRoutine);

            SetNavigationButtonsInteractable(false);
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
            SetNavigationButtonsInteractable(true);
            onClosed?.Invoke();
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
