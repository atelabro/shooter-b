using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace ShooterB
{
    public class CityPanelController : MonoBehaviour
    {
        public event Action OnPanelHidden;

        [Header("UI Elements")]
        public TextMeshProUGUI cityNameText;
        public TextMeshProUGUI briefingText;
        public Button closeButton;
        [Header("Briefing Animation")]
        [Min(1f)] public float typingCharsPerSecond = 45f;
        [Min(0f)] public float punctuationPause = 0.08f;

        [Header("Stage Cards")]
        public Transform stageListContainer;
        public CampaignStageEntryController stageEntryPrefab;
        [Header("Stage Scroll")]
        public ScrollRect stageScrollRect;
        [Min(0f)] public float autoScrollDelaySeconds = 0.8f;
        [Min(0.01f)] public float autoScrollDurationSeconds = 0.45f;

        private Coroutine briefingTypingCoroutine;
        private Coroutine autoScrollCoroutine;
        private CityConfig[] allCities;
        private CityConfig currentCity;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            gameObject.SetActive(false);
        }

        public void Initialize(CityConfig[] _cities)
        {
            allCities = _cities;
        }

        public void Show(CityConfig city)
        {
            if (city == null)
                return;

            currentCity = city;
            ResolveScrollReferences();
            ClearStageList();
            PopulateStageList(city);
            Canvas.ForceUpdateCanvases();

            if (stageScrollRect != null)
                stageScrollRect.verticalNormalizedPosition = 1f;

            if (cityNameText != null)
                cityNameText.text = CampaignLocalizationResolver.GetCityName(city);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (briefingText != null)
            {
                if (briefingTypingCoroutine != null)
                    StopCoroutine(briefingTypingCoroutine);

                briefingText.text = string.Empty;
                briefingTypingCoroutine = StartCoroutine(TypeBriefing(CampaignLocalizationResolver.GetCityBriefing(city)));
            }

            if (autoScrollCoroutine != null)
                StopCoroutine(autoScrollCoroutine);

            autoScrollCoroutine = StartCoroutine(ScrollToLatestOpenedStageAfterDelay(city));
        }

        public void Hide()
        {
            if (briefingTypingCoroutine != null)
            {
                StopCoroutine(briefingTypingCoroutine);
                briefingTypingCoroutine = null;
            }

            if (autoScrollCoroutine != null)
            {
                StopCoroutine(autoScrollCoroutine);
                autoScrollCoroutine = null;
            }

            gameObject.SetActive(false);
            OnPanelHidden?.Invoke();
        }

        public void RefreshLocalizationIfVisible()
        {
            if (!gameObject.activeInHierarchy || currentCity == null)
                return;

            Show(currentCity);
        }

        public bool IsShowingCity(CityConfig city)
        {
            return city != null && gameObject.activeInHierarchy && currentCity == city;
        }

        private void PopulateStageList(CityConfig city)
        {
            if (stageListContainer == null || stageEntryPrefab == null || city == null || city.stages == null)
                return;

            foreach (StageConfig stage in city.stages)
            {
                bool isUnlocked = CampaignProgressManager.Instance.IsStageUnlocked(stage, city, allCities);
                int stars = CampaignProgressManager.Instance.GetStarsForStage(stage.stageIndex);

                CampaignStageEntryController entry = Instantiate(stageEntryPrefab, stageListContainer);
                StageConfig capturedStage = stage;
                CityConfig capturedCity = city;
                entry.Initialize(stage, isUnlocked, stars, () => OnStageSelected(capturedCity, capturedStage));
            }
        }

        private void OnStageSelected(CityConfig city, StageConfig stage)
        {
            CampaignProgressManager.Instance.SetActiveCampaignLocation(city, stage);
            SceneController.Instance.LoadCampaignStage(stage);
        }

        private void ClearStageList()
        {
            if (stageListContainer == null)
                return;

            foreach (Transform child in stageListContainer)
                Destroy(child.gameObject);
        }

        private void ResolveScrollReferences()
        {
            if (stageScrollRect == null && stageListContainer != null)
                stageScrollRect = stageListContainer.GetComponentInParent<ScrollRect>(true);
        }

        private IEnumerator ScrollToLatestOpenedStageAfterDelay(CityConfig city)
        {
            if (city == null || city.stages == null || city.stages.Length == 0 || stageScrollRect == null)
            {
                autoScrollCoroutine = null;
                yield break;
            }

            if (autoScrollDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(autoScrollDelaySeconds);

            // Ensure layout-driven content height has been calculated.
            yield return null;
            Canvas.ForceUpdateCanvases();

            int targetIndex = GetLatestOpenedStageIndex(city);
            float target = GetVerticalNormalizedPositionForIndex(targetIndex, city.stages.Length);
            float start = 1f;
            stageScrollRect.verticalNormalizedPosition = start;
            float duration = Mathf.Max(0.01f, autoScrollDurationSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                stageScrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, eased);
                yield return null;
            }

            stageScrollRect.verticalNormalizedPosition = target;

            autoScrollCoroutine = null;
        }

        private int GetLatestOpenedStageIndex(CityConfig city)
        {
            StageConfig activeStage = CampaignProgressManager.Instance.ActiveStageConfig;
            bool activeMatchesCity =
                CampaignProgressManager.Instance.ActiveCityConfig == city &&
                activeStage != null;

            if (activeMatchesCity)
            {
                int activeIndex = System.Array.IndexOf(city.stages, activeStage);
                if (activeIndex >= 0)
                    return activeIndex;
            }

            for (int i = city.stages.Length - 1; i >= 0; i--)
            {
                StageConfig stage = city.stages[i];
                if (CampaignProgressManager.Instance.IsStageUnlocked(stage, city, allCities))
                    return i;
            }

            return 0;
        }

        private static float GetVerticalNormalizedPositionForIndex(int index, int total)
        {
            if (total <= 1)
                return 1f;

            float t = Mathf.Clamp01(index / (float)(total - 1));
            return 1f - t;
        }

        private IEnumerator TypeBriefing(string fullText)
        {
            if (briefingText == null)
                yield break;

            briefingText.text = string.Empty;

            if (string.IsNullOrEmpty(fullText))
            {
                briefingTypingCoroutine = null;
                yield break;
            }

            float secondsPerCharacter = 1f / Mathf.Max(1f, typingCharsPerSecond);
            for (int i = 0; i < fullText.Length; i++)
            {
                briefingText.text += fullText[i];
                yield return new WaitForSecondsRealtime(secondsPerCharacter);

                if (IsPunctuation(fullText[i]))
                    yield return new WaitForSecondsRealtime(punctuationPause);
            }

            briefingTypingCoroutine = null;
        }

        private static bool IsPunctuation(char c)
        {
            return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':';
        }
    }
}
