using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace ShooterB
{
    public class CampaignMapController : MonoBehaviour
    {
        [Header("City Data")]
        public CityConfig[] cities;

        [Header("City Pins")]
        public CityPinController[] cityPins;
        public CityPinController cityPinPrefab;

        [Header("City Panel")]
        public CityPanelController cityPanel;

        [Header("Top Bar")]
        public TextMeshProUGUI totalStarsText;

        [Header("Buttons")]
        public Button backButton;

        [Header("Map Focus")]
        public float focusZoomScale = 1.25f;
        public float focusDuration = 0.3f;
        [Range(0f, 1f)] public float focusedPinViewportX = 0.26f;
        [Range(0f, 1f)] public float focusedPinViewportY = 0.55f;
        public bool autoOpenLatestCityPanel = true;

        private RectTransform canvasRect;
        private RectTransform backgroundRect;
        private RectTransform pinsContainerRect;
        private Coroutine focusCoroutine;
        private readonly List<CityPinController> runtimePins = new List<CityPinController>();
        private Coroutine delayedPanelOpenCoroutine;

        private void Start()
        {
            ResolveMapReferences();

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (cityPanel != null)
            {
                cityPanel.Initialize(cities);
                cityPanel.OnPanelHidden += ResetMapFocus;
            }

            RefreshPins();
            RefreshTotalStars();
            EnsureActiveStageOnEnter();
            FocusLatestOpenedCityOnEnter();
        }

        private void RefreshPins()
        {
            EnsurePinInstances();
            if (cityPins == null || cityPins.Length == 0)
                return;

            for (int i = 0; i < cityPins.Length; i++)
            {
                if (i >= cities.Length)
                    break;

                CityConfig city = cities[i];
                bool isUnlocked = CampaignProgressManager.Instance.IsCityUnlocked(city, cities);
                CityConfig capturedCity = city;

                cityPins[i].Initialize(city, isUnlocked, () => OnCityPinClicked(capturedCity));
            }
        }

        private void RefreshTotalStars()
        {
            if (totalStarsText == null)
                return;

            int earned = CampaignProgressManager.Instance.GetTotalStars(cities);
            int max = CampaignProgressManager.Instance.GetMaxStars(cities);
            totalStarsText.text = $"{earned} / {max}";
        }

        private void OnCityPinClicked(CityConfig city)
        {
            int cityIndex = System.Array.IndexOf(cities, city);
            if (cityPins != null && cityIndex >= 0 && cityIndex < cityPins.Length && cityPins[cityIndex] != null)
                cityPins[cityIndex].transform.SetAsLastSibling();

            FocusMapOnCity(city);

            if (cityPanel != null)
                cityPanel.Show(city);
        }

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnToMenu();
        }

        private void OnDestroy()
        {
            if (delayedPanelOpenCoroutine != null)
                StopCoroutine(delayedPanelOpenCoroutine);

            if (cityPanel != null)
                cityPanel.OnPanelHidden -= ResetMapFocus;
        }

        private void FocusLatestOpenedCityOnEnter()
        {
            StageConfig latestStage = CampaignProgressManager.Instance.ActiveStageConfig;
            if (latestStage == null)
                return;

            CityConfig latestCity = FindCityForStage(latestStage);
            if (latestCity == null)
                return;

            FocusMapOnCity(latestCity);

            if (autoOpenLatestCityPanel && cityPanel != null)
            {
                if (delayedPanelOpenCoroutine != null)
                    StopCoroutine(delayedPanelOpenCoroutine);

                delayedPanelOpenCoroutine = StartCoroutine(ShowCityPanelAfterFocus(latestCity));
            }
        }

        private void EnsureActiveStageOnEnter()
        {
            if (CampaignProgressManager.Instance.ActiveStageConfig != null)
                return;

            if (cities == null || cities.Length == 0)
                return;

            for (int cityIndex = cities.Length - 1; cityIndex >= 0; cityIndex--)
            {
                CityConfig city = cities[cityIndex];
                if (city == null || city.stages == null || city.stages.Length == 0)
                    continue;

                for (int stageIndex = city.stages.Length - 1; stageIndex >= 0; stageIndex--)
                {
                    StageConfig stage = city.stages[stageIndex];
                    if (stage == null)
                        continue;

                    if (CampaignProgressManager.Instance.IsStageUnlocked(stage, city, cities))
                    {
                        CampaignProgressManager.Instance.SetActiveStage(stage);
                        return;
                    }
                }
            }
        }

        private CityConfig FindCityForStage(StageConfig stage)
        {
            if (stage == null || cities == null)
                return null;

            foreach (CityConfig city in cities)
            {
                if (city == null || city.stages == null)
                    continue;

                foreach (StageConfig cityStage in city.stages)
                {
                    if (cityStage == stage || (cityStage != null && cityStage.stageIndex == stage.stageIndex))
                        return city;
                }
            }

            return null;
        }

        private void ResolveMapReferences()
        {
            canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvasRect == null)
                canvasRect = transform.parent as RectTransform;

            Transform parent = transform.parent;
            if (parent != null)
            {
                Transform background = parent.Find("Background");
                Transform pinsContainer = parent.Find("CityPinsContainer");

                backgroundRect = background as RectTransform;
                pinsContainerRect = pinsContainer as RectTransform;
                if (background != null)
                {
                    Image backgroundImage = background.GetComponent<Image>();
                    if (backgroundImage != null)
                        backgroundImage.raycastTarget = false;
                }
            }

        }

        private void EnsurePinInstances()
        {
            if (cities == null || cities.Length == 0)
                return;

            if (cityPinPrefab == null || pinsContainerRect == null)
                return;

            bool needsSpawn = runtimePins.Count != cities.Length;
            if (!needsSpawn)
            {
                for (int i = 0; i < runtimePins.Count; i++)
                {
                    if (runtimePins[i] == null)
                    {
                        needsSpawn = true;
                        break;
                    }
                }
            }

            if (!needsSpawn)
            {
                cityPins = runtimePins.ToArray();
                return;
            }

            DeactivateLegacyScenePins();
            ClearRuntimePins();
            cityPins = new CityPinController[cities.Length];

            for (int i = 0; i < cities.Length; i++)
            {
                CityPinController pin = Instantiate(cityPinPrefab, pinsContainerRect);
                pin.name = $"Pin_{cities[i].cityName}";
                cityPins[i] = pin;
                runtimePins.Add(pin);
            }
        }

        private void DeactivateLegacyScenePins()
        {
            if (cityPins == null)
                return;

            for (int i = 0; i < cityPins.Length; i++)
            {
                if (cityPins[i] != null)
                    cityPins[i].gameObject.SetActive(false);
            }
        }

        private void ClearRuntimePins()
        {
            for (int i = 0; i < runtimePins.Count; i++)
            {
                if (runtimePins[i] != null)
                    Destroy(runtimePins[i].gameObject);
            }
            runtimePins.Clear();
        }

        private void FocusMapOnCity(CityConfig city)
        {
            if (city == null || canvasRect == null || backgroundRect == null || pinsContainerRect == null)
                return;

            int cityIndex = System.Array.IndexOf(cities, city);
            if (cityIndex < 0 || cityIndex >= cityPins.Length || cityPins[cityIndex] == null)
                return;

            RectTransform pinRect = cityPins[cityIndex].GetComponent<RectTransform>();
            if (pinRect == null)
                return;

            float targetScale = focusZoomScale <= 0f ? 1f : focusZoomScale;
            Vector2 targetMapPosition = ComputeMapAnchoredPositionForPin(pinRect.anchoredPosition, targetScale);
            targetMapPosition = ClampMapAnchoredPositionToBounds(targetMapPosition, targetScale);

            if (focusCoroutine != null)
                StopCoroutine(focusCoroutine);

            focusCoroutine = StartCoroutine(AnimateMapFocus(targetMapPosition, targetScale));
        }

        private void ResetMapFocus()
        {
            if (backgroundRect == null || pinsContainerRect == null)
                return;

            if (focusCoroutine != null)
                StopCoroutine(focusCoroutine);

            focusCoroutine = StartCoroutine(AnimateMapFocus(Vector2.zero, 1f));
        }

        private Vector2 ComputeMapAnchoredPositionForPin(Vector2 pinAnchoredPosition, float scale)
        {
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 desiredPinLocalPosition = new Vector2(
                (focusedPinViewportX - 0.5f) * canvasSize.x,
                (focusedPinViewportY - 0.5f) * canvasSize.y
            );

            return desiredPinLocalPosition - (pinAnchoredPosition * scale);
        }

        private Vector2 ClampMapAnchoredPositionToBounds(Vector2 position, float scale)
        {
            Vector2 canvasSize = canvasRect.rect.size;
            float clampedScale = Mathf.Max(1f, scale);

            float maxOffsetX = (canvasSize.x * clampedScale - canvasSize.x) * 0.5f;
            float maxOffsetY = (canvasSize.y * clampedScale - canvasSize.y) * 0.5f;

            return new Vector2(
                Mathf.Clamp(position.x, -maxOffsetX, maxOffsetX),
                Mathf.Clamp(position.y, -maxOffsetY, maxOffsetY)
            );
        }

        private IEnumerator AnimateMapFocus(Vector2 targetAnchoredPosition, float targetScale)
        {
            Vector2 startBackgroundPosition = backgroundRect.anchoredPosition;
            Vector2 startPinsPosition = pinsContainerRect.anchoredPosition;
            Vector3 startBackgroundScale = backgroundRect.localScale;
            Vector3 startPinsScale = pinsContainerRect.localScale;
            Vector3 endScale = Vector3.one * targetScale;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, focusDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                backgroundRect.anchoredPosition = Vector2.Lerp(startBackgroundPosition, targetAnchoredPosition, eased);
                pinsContainerRect.anchoredPosition = Vector2.Lerp(startPinsPosition, targetAnchoredPosition, eased);
                backgroundRect.localScale = Vector3.Lerp(startBackgroundScale, endScale, eased);
                pinsContainerRect.localScale = Vector3.Lerp(startPinsScale, endScale, eased);

                yield return null;
            }

            backgroundRect.anchoredPosition = targetAnchoredPosition;
            pinsContainerRect.anchoredPosition = targetAnchoredPosition;
            backgroundRect.localScale = endScale;
            pinsContainerRect.localScale = endScale;
            focusCoroutine = null;
        }

        private IEnumerator ShowCityPanelAfterFocus(CityConfig city)
        {
            float delay = Mathf.Max(0.01f, focusDuration);
            yield return new WaitForSecondsRealtime(delay);

            if (cityPanel != null && city != null)
                cityPanel.Show(city);

            delayedPanelOpenCoroutine = null;
        }
    }
}
