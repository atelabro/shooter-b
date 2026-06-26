using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShooterB
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class ChronosLockPowerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string ChronosIconResourcePath = "SuperPowers/chronos-power";
        private const string FallbackIconResourcePath = "SuperPowers/zeus-power";
        private const string ClockworkAudioResourcePath = "Audio/clockwork";
        private const float ControlWidth = 128f;
        private const float ControlHeight = 64f;
        private const float DropCancelDistance = 70f;

        public Image iconImage;
        public Image dragIconImage;
        public TextMeshProUGUI countText;

        public Vector2 homeAnchoredPosition = new Vector2(144f, 8f);

        private RectTransform rectTransform;
        private Image backgroundImage;
        private AudioSource audioSource;
        private AudioClip clockworkClip;
        private Canvas canvas;
        private Vector2 homePosition;
        private Vector2 dragIconHomePosition;
        private Vector2 dragPointerOffset;
        private bool dragging;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            backgroundImage = GetComponent<Image>();
            canvas = GetComponentInParent<Canvas>();
            ConfigureLayout();
            EnsureChildReferences();
            EnsureAudio();
            Refresh();
        }

        private void OnEnable()
        {
            GameManager.Instance.OnChronosLockCountChanged += HandleCountChanged;
            GameManager.Instance.OnPauseStateChanged += HandlePauseStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnChronosLockCountChanged -= HandleCountChanged;
                GameManager.Instance.OnPauseStateChanged -= HandlePauseStateChanged;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag())
                return;

            dragging = true;
            transform.SetAsLastSibling();
            ShowDragIcon();
            dragPointerOffset = GetDragPointerOffset(eventData.position);
            SetDragIconPositionFromScreen(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            SetDragIconPositionFromScreen(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            dragging = false;
            RectTransform dragIconRect = GetDragIconRectTransform();
            float distanceFromHome = dragIconRect != null
                ? Vector2.Distance(dragIconRect.anchoredPosition, dragIconHomePosition)
                : 0f;

            if (distanceFromHome >= DropCancelDistance)
                TryCastChronosLock();

            HideDragIcon();
            Refresh();
        }

        public void SetHomeAnchoredPosition(Vector2 anchoredPosition)
        {
            homeAnchoredPosition = anchoredPosition;
            homePosition = anchoredPosition;

            if (rectTransform != null && !dragging)
                rectTransform.anchoredPosition = homePosition;
        }

        private void ConfigureLayout()
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.sizeDelta = new Vector2(ControlWidth, ControlHeight);
            homePosition = homeAnchoredPosition;
            rectTransform.anchoredPosition = homePosition;

            if (backgroundImage != null)
            {
                backgroundImage.color = Color.clear;
                backgroundImage.raycastTarget = true;
            }
        }

        private void EnsureChildReferences()
        {
            Sprite icon = LoadSpriteResource(ChronosIconResourcePath) ?? LoadSpriteResource(FallbackIconResourcePath);

            if (iconImage == null)
                iconImage = FindChildImage("Icon");

            if (iconImage == null)
                iconImage = CreateChildImage("Icon", new Vector2(0f, 0f), new Vector2(0.55f, 1f));

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            if (dragIconImage == null)
                dragIconImage = FindChildImage("DragIcon");

            if (dragIconImage == null)
                dragIconImage = CreateChildImage("DragIcon", new Vector2(0f, 0f), new Vector2(0.55f, 1f));

            if (dragIconImage != null)
            {
                dragIconImage.sprite = icon;
                dragIconImage.preserveAspect = true;
                dragIconImage.raycastTarget = false;
                dragIconImage.gameObject.SetActive(false);
                dragIconHomePosition = dragIconImage.rectTransform.anchoredPosition;
            }

            if (countText == null)
                countText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (countText == null)
            {
                GameObject textObj = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(transform, false);
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.55f, 0f);
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                countText = textObj.GetComponent<TextMeshProUGUI>();
                countText.fontSize = 30f;
                countText.alignment = TextAlignmentOptions.MidlineLeft;
                countText.color = Color.white;
            }

            if (countText != null)
                countText.raycastTarget = false;
        }

        private bool CanDrag()
        {
            return GameManager.Instance.ChronosLockCount > 0 &&
                !GameManager.Instance.IsPaused &&
                !GameManager.Instance.IsGameOver;
        }

        private void TryCastChronosLock()
        {
            IDuckSpawner spawner = FindActiveSpawner();
            if (spawner == null || spawner.ActiveDuckCount <= 0)
                return;

            if (!GameManager.Instance.TryUseChronosLockCharge())
                return;

            int frozenCount = spawner.FreezeAllActiveDucks(Constants.CHRONOS_LOCK_DURATION);
            spawner.PauseSpawningFor(Constants.CHRONOS_LOCK_DURATION);
            if (frozenCount > 0)
            {
                PlayClockworkSound(Constants.CHRONOS_LOCK_DURATION);
                PlayChronosEffect();
            }
        }

        private IDuckSpawner FindActiveSpawner()
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(false);
            for (int i = 0; i < behaviours.Length; i++)
            {
                IDuckSpawner spawner = behaviours[i] as IDuckSpawner;
                if (spawner != null && spawner.ActiveDuckCount > 0)
                    return spawner;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                IDuckSpawner spawner = behaviours[i] as IDuckSpawner;
                if (spawner != null)
                    return spawner;
            }

            return null;
        }

        private void PlayChronosEffect()
        {
            Transform parent = canvas != null ? canvas.transform : transform.parent;
            GameObject flash = new GameObject("ChronosLockFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            flash.transform.SetParent(parent, false);

            RectTransform flashRect = flash.GetComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            Image flashImage = flash.GetComponent<Image>();
            flashImage.color = new Color(0.35f, 0.9f, 1f, 0.22f);
            flashImage.raycastTarget = false;
            Destroy(flash, 0.2f);

            StartCoroutine(PlayClockPulse(parent));
        }

        private void EnsureAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            clockworkClip = Resources.Load<AudioClip>(ClockworkAudioResourcePath);

            if (clockworkClip == null)
                GameLog.Warning($"[ChronosLockPowerController] Missing clockwork SFX clip at Resources/{ClockworkAudioResourcePath}.");
        }

        private void PlayClockworkSound(float duration)
        {
            if (audioSource == null || clockworkClip == null || duration <= 0f)
                return;

            StopCoroutine(nameof(StopClockworkSoundAfterDelay));
            audioSource.Stop();
            audioSource.clip = clockworkClip;
            audioSource.loop = true;
            audioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
            audioSource.Play();
            StartCoroutine(StopClockworkSoundAfterDelay(duration));
        }

        private IEnumerator StopClockworkSoundAfterDelay(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.clip = null;
            }
        }

        private IEnumerator PlayClockPulse(Transform parent)
        {
            GameObject pulse = new GameObject("ChronosLockPulse", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pulse.transform.SetParent(parent, false);

            RectTransform pulseRect = pulse.GetComponent<RectTransform>();
            pulseRect.anchorMin = new Vector2(0.5f, 0.5f);
            pulseRect.anchorMax = new Vector2(0.5f, 0.5f);
            pulseRect.pivot = new Vector2(0.5f, 0.5f);
            pulseRect.anchoredPosition = Vector2.zero;
            pulseRect.sizeDelta = new Vector2(220f, 220f);

            Image pulseImage = pulse.GetComponent<Image>();
            pulseImage.sprite = iconImage != null ? iconImage.sprite : null;
            pulseImage.preserveAspect = true;
            pulseImage.raycastTarget = false;

            float duration = 0.45f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(0.6f, 2.4f, t);
                pulse.transform.localScale = new Vector3(scale, scale, 1f);
                pulseImage.color = new Color(0.65f, 0.95f, 1f, Mathf.Lerp(0.75f, 0f, t));
                yield return null;
            }

            Destroy(pulse);
        }

        private void ShowDragIcon()
        {
            if (dragIconImage == null)
                return;

            dragIconImage.color = Color.white;
            dragIconImage.gameObject.SetActive(true);
            dragIconImage.transform.SetAsLastSibling();
        }

        private void HideDragIcon()
        {
            RectTransform dragIconRect = GetDragIconRectTransform();
            if (dragIconRect != null)
                dragIconRect.anchoredPosition = dragIconHomePosition;

            if (dragIconImage != null)
                dragIconImage.gameObject.SetActive(false);
        }

        private void SetDragIconPositionFromScreen(Vector2 screenPosition)
        {
            RectTransform dragIconRect = GetDragIconRectTransform();
            if (dragIconRect == null)
                return;

            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out Vector2 localPoint))
                dragIconRect.anchoredPosition = localPoint + dragPointerOffset;
        }

        private Vector2 GetDragPointerOffset(Vector2 screenPosition)
        {
            RectTransform dragIconRect = GetDragIconRectTransform();
            if (dragIconRect == null)
                return Vector2.zero;

            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out Vector2 localPoint))
                return dragIconRect.anchoredPosition - localPoint;

            return Vector2.zero;
        }

        private RectTransform GetDragIconRectTransform()
        {
            return dragIconImage != null ? dragIconImage.rectTransform : null;
        }

        private Image FindChildImage(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Image CreateChildImage(string childName, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject imageObj = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObj.transform.SetParent(transform, false);
            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = anchorMin;
            imageRect.anchorMax = anchorMax;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            return imageObj.GetComponent<Image>();
        }

        private static Sprite LoadSpriteResource(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        private void HandleCountChanged(int count)
        {
            Refresh();
        }

        private void HandlePauseStateChanged(bool isPaused)
        {
            Refresh();
        }

        private void Refresh()
        {
            int count = GameManager.Instance.ChronosLockCount;
            if (countText != null)
                countText.text = $"x{count}";

            if (backgroundImage != null)
                backgroundImage.color = Color.clear;

            if (iconImage != null)
                iconImage.color = CanDrag()
                    ? Color.white
                    : new Color(0.55f, 0.55f, 0.55f, 0.85f);
        }
    }
}
