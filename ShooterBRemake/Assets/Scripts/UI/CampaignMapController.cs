using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ShooterB
{
    public class CampaignMapController : MonoBehaviour
    {
        [Header("City Data")]
        public CityConfig[] cities;

        [Header("City Pins")]
        public CityPinController[] cityPins;
        public CityPinController cityPinPrefab;
        [Range(0.1f, 2f)] public float cityPinScale = 0.7f;

        [Header("City Panel")]
        public CityPanelController cityPanel;

        [Header("Top Bar")]
        public TextMeshProUGUI mapTitleText;
        public TextMeshProUGUI totalStarsText;

        [Header("Buttons")]
        public Button backButton;
        public Button armoryButton;
        public Button achievementsButton;

        [Header("Map Focus")]
        public float focusZoomScale = 1.25f;
        public float focusDuration = 0.3f;
        [Range(0f, 1f)] public float focusedPinViewportX = 0.26f;
        [Range(0f, 1f)] public float focusedPinViewportY = 0.55f;
        public bool autoOpenLatestCityPanel = true;
        [Header("Map Drag")]
        public bool enableMapDrag = true;
        [Min(0f)] public float dragThresholdPixels = 4f;
        [Min(0f)] public float dragSensitivity = 1f;
        [Header("Map Zoom")]
        public bool enableMapZoom = true;
        [Min(0.01f)] public float zoomStep = 0.15f;
        [Min(0.01f)] public float pinchZoomSensitivity = 0.005f;
        [Header("Continue Transition")]
        [Min(0f)] public float continueTransitionMinDelay = 1f;

        private RectTransform canvasRect;
        private RectTransform backgroundRect;
        private RectTransform pinsContainerRect;
        private Coroutine focusCoroutine;
        private readonly List<CityPinController> runtimePins = new List<CityPinController>();
        private Coroutine delayedPanelOpenCoroutine;
        private Coroutine continueTransitionCoroutine;
        private bool isPointerDown;
        private bool isDraggingMap;
        private int activeTouchId = -1;
        private Vector2 pointerDownScreenPosition;
        private Vector2 lastPointerScreenPosition;
        private float currentMapScale = 1f;
        private bool suppressNextPanelHideReset;

        private void Start()
        {
            CampaignProgressManager.Instance.SetCampaignCities(cities);
            ResolveMapReferences();
            ResolveMapTitleTextReference();

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (armoryButton != null)
                armoryButton.onClick.AddListener(OnArmoryClicked);

            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsClicked);

            if (cityPanel != null)
            {
                cityPanel.Initialize(cities);
                cityPanel.OnPanelHidden += OnCityPanelHidden;
            }

            RefreshLocalizedTexts();
            RefreshPins();
            RefreshTotalStars();
            EnsureActiveStageOnEnter();
            if (!TryStartPendingContinueTransitionOnEnter())
                FocusLatestOpenedCityOnEnter();

            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }

        private void Update()
        {
            HandleMapDragInput();
            HandleMapZoomInput();
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

                if (cityPins[i] != null)
                    cityPins[i].transform.localScale = Vector3.one * cityPinScale;

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
            {
                if (cityPanel.IsShowingCity(city))
                    return;

                cityPanel.Show(city);
            }
        }

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnToMenu();
        }

        private void OnArmoryClicked()
        {
            SceneController.Instance.LoadArmoryScene();
        }

        private void OnAchievementsClicked()
        {
            SceneController.Instance.LoadAchievementsScene();
        }

        private void OnDestroy()
        {
            if (delayedPanelOpenCoroutine != null)
                StopCoroutine(delayedPanelOpenCoroutine);

            if (continueTransitionCoroutine != null)
                StopCoroutine(continueTransitionCoroutine);

            if (cityPanel != null)
                cityPanel.OnPanelHidden -= OnCityPanelHidden;

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);

            if (armoryButton != null)
                armoryButton.onClick.RemoveListener(OnArmoryClicked);

            if (achievementsButton != null)
                achievementsButton.onClick.RemoveListener(OnAchievementsClicked);

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
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

        private bool TryStartPendingContinueTransitionOnEnter()
        {
            if (!CampaignProgressManager.Instance.TryConsumePendingMapFocusTransition(
                    out CityConfig fromCity,
                    out StageConfig fromStage,
                    out CityConfig toCity,
                    out StageConfig toStage,
                    out float minDelaySeconds))
            {
                return false;
            }

            if (fromCity == null || fromStage == null || toCity == null || toStage == null)
                return false;

            CampaignProgressManager.Instance.SetActiveCampaignLocation(fromCity, fromStage);
            FocusMapOnCity(fromCity);

            if (continueTransitionCoroutine != null)
                StopCoroutine(continueTransitionCoroutine);

            float delay = Mathf.Max(minDelaySeconds, continueTransitionMinDelay);
            continueTransitionCoroutine = StartCoroutine(AnimateContinueTransition(toCity, toStage, delay));
            return true;
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
                        CampaignProgressManager.Instance.SetActiveCampaignLocation(city, stage);
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
                if (backgroundRect != null && pinsContainerRect != null && backgroundRect.parent == pinsContainerRect.parent)
                    pinsContainerRect.SetSiblingIndex(backgroundRect.GetSiblingIndex() + 1);

                if (background != null)
                {
                    Image backgroundImage = background.GetComponent<Image>();
                    if (backgroundImage != null)
                        backgroundImage.raycastTarget = false;
                }
            }

        }

        private void ResolveMapTitleTextReference()
        {
            if (mapTitleText == null)
                mapTitleText = FindTextInSceneByName("TitleText");
        }

        private TextMeshProUGUI FindTextInSceneByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == name)
                    return texts[i];
            }

            return null;
        }

        private void RefreshLocalizedTexts()
        {
            if (mapTitleText != null)
                mapTitleText.text = LocalizationManager.Instance.Get("campaign.map.title", "D.U.C.K. OPERATIONS");
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedTexts();
            RefreshPins();

            if (cityPanel != null)
                cityPanel.RefreshLocalizationIfVisible();
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
                pin.transform.localScale = Vector3.one * cityPinScale;
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
            Vector2 mapBaseSize = backgroundRect.rect.size;
            float clampedScale = Mathf.Max(0.01f, scale);
            Vector2 scaledMapSize = mapBaseSize * clampedScale;

            float maxOffsetX = Mathf.Max(0f, (scaledMapSize.x - canvasSize.x) * 0.5f);
            float maxOffsetY = Mathf.Max(0f, (scaledMapSize.y - canvasSize.y) * 0.5f);

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
            currentMapScale = targetScale;
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

        private IEnumerator AnimateContinueTransition(CityConfig toCity, StageConfig toStage, float delaySeconds)
        {
            if (delaySeconds > 0f)
                yield return new WaitForSecondsRealtime(delaySeconds);

            if (toCity == null || toStage == null)
            {
                continueTransitionCoroutine = null;
                yield break;
            }

            CampaignProgressManager.Instance.SetActiveCampaignLocation(toCity, toStage);
            FocusMapOnCity(toCity);

            if (autoOpenLatestCityPanel && cityPanel != null)
            {
                if (delayedPanelOpenCoroutine != null)
                    StopCoroutine(delayedPanelOpenCoroutine);

                delayedPanelOpenCoroutine = StartCoroutine(ShowCityPanelAfterFocus(toCity));
            }

            continueTransitionCoroutine = null;
        }

        private void HandleMapDragInput()
        {
            if (!enableMapDrag || canvasRect == null || backgroundRect == null || pinsContainerRect == null)
                return;

            if (GetActiveTouchCount() > 1)
            {
                if (isPointerDown)
                    ResetDragState();

                return;
            }

            if (TryGetPointerDown(out Vector2 downPos, out int pointerId))
            {
                if (cityPanel != null && cityPanel.gameObject.activeInHierarchy && IsPointerOverUI(pointerId))
                    return;

                isPointerDown = true;
                isDraggingMap = false;
                activeTouchId = pointerId;
                pointerDownScreenPosition = downPos;
                lastPointerScreenPosition = downPos;
                return;
            }

            if (!isPointerDown)
                return;

            if (TryGetPointerUp(activeTouchId))
            {
                ResetDragState();
                return;
            }

            if (!TryGetPointerPosition(activeTouchId, out Vector2 currentScreenPos))
                return;

            if (cityPanel != null && cityPanel.gameObject.activeInHierarchy && IsPointerOverUI(activeTouchId))
                return;

            if (!isDraggingMap)
            {
                float threshold = Mathf.Max(0f, dragThresholdPixels);
                if ((currentScreenPos - pointerDownScreenPosition).sqrMagnitude < threshold * threshold)
                    return;

                isDraggingMap = true;

                if (focusCoroutine != null)
                {
                    StopCoroutine(focusCoroutine);
                    focusCoroutine = null;
                }

                if (delayedPanelOpenCoroutine != null)
                {
                    StopCoroutine(delayedPanelOpenCoroutine);
                    delayedPanelOpenCoroutine = null;
                }

                if (cityPanel != null && cityPanel.gameObject.activeInHierarchy)
                {
                    suppressNextPanelHideReset = true;
                    cityPanel.Hide();
                }
            }

            Vector2 screenDelta = currentScreenPos - lastPointerScreenPosition;
            lastPointerScreenPosition = currentScreenPos;

            float canvasScale = canvasRect.lossyScale.x;
            if (canvasScale <= 0f)
                canvasScale = 1f;

            Vector2 anchoredDelta = (screenDelta / canvasScale) * dragSensitivity;
            Vector2 targetPosition = backgroundRect.anchoredPosition + anchoredDelta;
            float currentScale = Mathf.Max(1f, backgroundRect.localScale.x);
            targetPosition = ClampMapAnchoredPositionToBounds(targetPosition, currentScale);

            backgroundRect.anchoredPosition = targetPosition;
            pinsContainerRect.anchoredPosition = targetPosition;
        }

        private void ResetDragState()
        {
            isPointerDown = false;
            isDraggingMap = false;
            activeTouchId = -1;
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    var phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        screenPosition = touch.position.ReadValue();
                        pointerId = touch.touchId.ReadValue();
                        return true;
                    }
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                pointerId = -999;
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began)
                    {
                        screenPosition = touch.position;
                        pointerId = touch.fingerId;
                        return true;
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                pointerId = -999;
                return true;
            }
#endif

            screenPosition = Vector2.zero;
            pointerId = -1;
            return false;
        }

        private static bool TryGetPointerPosition(int pointerId, out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (pointerId == -999)
            {
                if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                {
                    screenPosition = Mouse.current.position.ReadValue();
                    return true;
                }

                screenPosition = Vector2.zero;
                return false;
            }

            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.touchId.ReadValue() != pointerId)
                        continue;

                    var phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                        break;

                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }

            screenPosition = Vector2.zero;
            return false;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (pointerId == -999)
            {
                if (Input.GetMouseButton(0))
                {
                    screenPosition = Input.mousePosition;
                    return true;
                }

                screenPosition = Vector2.zero;
                return false;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == pointerId)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }

            screenPosition = Vector2.zero;
            return false;
#endif
        }

        private static bool TryGetPointerUp(int pointerId)
        {
#if ENABLE_INPUT_SYSTEM
            if (pointerId == -999)
                return Mouse.current == null || Mouse.current.leftButton.wasReleasedThisFrame;

            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.touchId.ReadValue() != pointerId)
                        continue;

                    var phase = touch.phase.ReadValue();
                    return phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                }
            }

            // touch is no longer present
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (pointerId == -999)
                return Input.GetMouseButtonUp(0);

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == pointerId)
                    return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }

            return false;
#endif
        }

        private void HandleMapZoomInput()
        {
            if (!enableMapZoom || canvasRect == null || backgroundRect == null || pinsContainerRect == null)
                return;

            if (cityPanel != null && cityPanel.gameObject.activeInHierarchy)
                return;

            float zoomDelta = 0f;

            if (TryGetPinchZoomDelta(out float pinchDelta))
            {
                zoomDelta = pinchDelta * pinchZoomSensitivity;
            }
            else
            {
                float wheel = GetMouseWheelDelta();
                if (Mathf.Abs(wheel) > Mathf.Epsilon)
                    zoomDelta = wheel * zoomStep;
            }

            if (Mathf.Abs(zoomDelta) <= Mathf.Epsilon)
                return;

            if (focusCoroutine != null)
            {
                StopCoroutine(focusCoroutine);
                focusCoroutine = null;
            }

            if (delayedPanelOpenCoroutine != null)
            {
                StopCoroutine(delayedPanelOpenCoroutine);
                delayedPanelOpenCoroutine = null;
            }

            float maxZoom = Mathf.Max(1f, focusZoomScale);
            currentMapScale = Mathf.Clamp(currentMapScale + zoomDelta, 1f, maxZoom);

            Vector3 zoomScale = Vector3.one * currentMapScale;
            backgroundRect.localScale = zoomScale;
            pinsContainerRect.localScale = zoomScale;

            Vector2 clampedPosition = ClampMapAnchoredPositionToBounds(backgroundRect.anchoredPosition, currentMapScale);
            backgroundRect.anchoredPosition = clampedPosition;
            pinsContainerRect.anchoredPosition = clampedPosition;
        }

        private static float GetMouseWheelDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.scroll.ReadValue().y / 120f;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta.y;
#endif
            return 0f;
        }

        private static bool TryGetPinchZoomDelta(out float pinchDelta)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                Vector2 p0 = default, p1 = default;
                Vector2 d0 = default, d1 = default;
                bool has0 = false, has1 = false;

                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed)
                        continue;

                    if (!has0)
                    {
                        p0 = touch.position.ReadValue();
                        d0 = touch.delta.ReadValue();
                        has0 = true;
                        continue;
                    }

                    p1 = touch.position.ReadValue();
                    d1 = touch.delta.ReadValue();
                    has1 = true;
                    break;
                }

                if (has0 && has1)
                {
                    float prevDistance = Vector2.Distance(p0 - d0, p1 - d1);
                    float currentDistance = Vector2.Distance(p0, p1);
                    pinchDelta = currentDistance - prevDistance;
                    return true;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount >= 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);
                Vector2 prevT0 = t0.position - t0.deltaPosition;
                Vector2 prevT1 = t1.position - t1.deltaPosition;
                float prevDistance = Vector2.Distance(prevT0, prevT1);
                float currentDistance = Vector2.Distance(t0.position, t1.position);
                pinchDelta = currentDistance - prevDistance;
                return true;
            }
#endif
            pinchDelta = 0f;
            return false;
        }

        private static bool IsPointerOverUI(int pointerId)
        {
            if (EventSystem.current == null)
                return false;

            if (pointerId == -999)
                return EventSystem.current.IsPointerOverGameObject();

            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        private static int GetActiveTouchCount()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                int activeTouches = 0;
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.isPressed)
                        activeTouches++;
                }

                return activeTouches;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.touchCount;
#endif
            return 0;
        }

        private void OnCityPanelHidden()
        {
            if (suppressNextPanelHideReset)
            {
                suppressNextPanelHideReset = false;
                return;
            }

            ResetMapFocus();
        }
    }
}
