using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShooterB
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class ThunderPowerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string ZeusPowerIconResourcePath = "SuperPowers/zeus-power";
        private const string ThunderSoundResourcePath = "Audio/thunder-sound-1";
        private const float ControlWidth = 128f;
        private const float ControlHeight = 64f;
        private const float DropCancelDistance = 70f;

        [Header("Prefab References")]
        public Image iconImage;
        public Image dragIconImage;
        public TextMeshProUGUI countText;

        [Header("Layout")]
        public Vector2 homeAnchoredPosition = new Vector2(8f, 8f);

        private RectTransform rectTransform;
        private Image backgroundImage;
        private AudioSource audioSource;
        private AudioClip thunderSoundClip;
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
            GameManager.Instance.OnZeusThunderCountChanged += HandleCountChanged;
            GameManager.Instance.OnPauseStateChanged += HandlePauseStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnZeusThunderCountChanged -= HandleCountChanged;
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
                TryCastThunder();

            HideDragIcon();
            Refresh();
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
            if (iconImage == null)
                iconImage = GetComponentInChildren<Image>(true);

            if (iconImage != null)
            {
                if (iconImage.sprite == null)
                    iconImage.sprite = Resources.Load<Sprite>(ZeusPowerIconResourcePath);

                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            if (dragIconImage == null)
                dragIconImage = FindChildImage("DragIcon");

            if (dragIconImage != null)
            {
                if (dragIconImage.sprite == null && iconImage != null)
                    dragIconImage.sprite = iconImage.sprite;

                if (dragIconImage.sprite == null)
                    dragIconImage.sprite = Resources.Load<Sprite>(ZeusPowerIconResourcePath);

                dragIconImage.preserveAspect = true;
                dragIconImage.raycastTarget = false;
                dragIconImage.gameObject.SetActive(false);

                RectTransform dragIconRect = dragIconImage.rectTransform;
                dragIconHomePosition = dragIconRect.anchoredPosition;
            }

            if (countText == null)
                countText = GetComponentInChildren<TextMeshProUGUI>(true);

            if (countText != null)
                countText.raycastTarget = false;
        }

        private bool CanDrag()
        {
            return GameManager.Instance.ZeusThunderCount > 0 &&
                !GameManager.Instance.IsPaused &&
                !GameManager.Instance.IsGameOver;
        }

        private void TryCastThunder()
        {
            IDuckSpawner spawner = FindActiveSpawner();
            if (spawner == null || spawner.ActiveDuckCount <= 0)
                return;

            if (!GameManager.Instance.TryUseZeusThunderCharge())
                return;

            PlayThunderSound();
            PlayThunderEffect();
            spawner.DamageAllActiveDucks(Constants.ZEUS_THUNDER_DAMAGE, Constants.WeaponType.TeslaGun);
        }

        private void EnsureAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            thunderSoundClip = Resources.Load<AudioClip>(ThunderSoundResourcePath);

            if (thunderSoundClip == null)
                GameLog.Warning($"[ThunderPowerController] Missing thunder SFX clip at Resources/{ThunderSoundResourcePath}.");
        }

        private void PlayThunderSound()
        {
            if (audioSource == null || thunderSoundClip == null)
                return;

            audioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
            audioSource.PlayOneShot(thunderSoundClip);
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

        private void PlayThunderEffect()
        {
            Transform parent = canvas != null ? canvas.transform : transform.parent;
            GameObject flash = new GameObject("ZeusThunderFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            flash.transform.SetParent(parent, false);

            RectTransform flashRect = flash.GetComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            Image flashImage = flash.GetComponent<Image>();
            flashImage.color = new Color(0.55f, 0.75f, 1f, 0.28f);
            flashImage.raycastTarget = false;
            Destroy(flash, 0.16f);

            for (int i = 0; i < 5; i++)
            {
                GameObject bolt = new GameObject("ZeusThunderBolt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bolt.transform.SetParent(parent, false);
                RectTransform boltRect = bolt.GetComponent<RectTransform>();
                boltRect.anchorMin = new Vector2(0.1f + i * 0.2f, 0.5f);
                boltRect.anchorMax = new Vector2(0.1f + i * 0.2f, 1f);
                boltRect.pivot = new Vector2(0.5f, 1f);
                boltRect.sizeDelta = new Vector2(12f, 0f);
                boltRect.anchoredPosition = Vector2.zero;

                Image boltImage = bolt.GetComponent<Image>();
                boltImage.color = new Color(0.75f, 0.9f, 1f, 0.9f);
                boltImage.raycastTarget = false;
                Destroy(bolt, 0.22f);
            }
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

        public void SetHomeAnchoredPosition(Vector2 anchoredPosition)
        {
            homeAnchoredPosition = anchoredPosition;
            homePosition = anchoredPosition;

            if (rectTransform != null && !dragging)
                rectTransform.anchoredPosition = homePosition;
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
            int count = GameManager.Instance.ZeusThunderCount;
            if (countText != null)
                countText.text = $"x{count}";

            bool canDrag = CanDrag();
            if (backgroundImage != null)
                backgroundImage.color = Color.clear;

            if (iconImage != null)
                iconImage.color = canDrag
                    ? Color.white
                    : new Color(0.55f, 0.55f, 0.55f, 0.85f);
        }
    }
}
