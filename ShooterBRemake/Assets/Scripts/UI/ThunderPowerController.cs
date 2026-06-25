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
        private const float ControlSize = 118f;
        private const float DropCancelDistance = 70f;

        private RectTransform rectTransform;
        private Image backgroundImage;
        private TextMeshProUGUI iconText;
        private TextMeshProUGUI countText;
        private Canvas canvas;
        private Vector2 homePosition;
        private bool dragging;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            backgroundImage = GetComponent<Image>();
            canvas = GetComponentInParent<Canvas>();
            ConfigureLayout();
            BuildChildren();
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
            SetPositionFromScreen(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            SetPositionFromScreen(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            dragging = false;
            float distanceFromHome = Vector2.Distance(rectTransform.anchoredPosition, homePosition);
            if (distanceFromHome >= DropCancelDistance)
                TryCastThunder();

            ReturnHome();
            Refresh();
        }

        private void ConfigureLayout()
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(ControlSize, ControlSize);
            homePosition = new Vector2(88f, 92f);
            rectTransform.anchoredPosition = homePosition;

            backgroundImage.color = new Color(0.08f, 0.13f, 0.24f, 0.92f);
            backgroundImage.raycastTarget = true;
        }

        private void BuildChildren()
        {
            iconText = CreateText("Icon", new Vector2(0f, 0.2f), new Vector2(1f, 1f), 48f, TextAlignmentOptions.Center);
            iconText.text = "Z";
            iconText.color = new Color(1f, 0.92f, 0.34f, 1f);

            countText = CreateText("Count", Vector2.zero, new Vector2(1f, 0.34f), 24f, TextAlignmentOptions.Center);
            countText.color = Color.white;
        }

        private TextMeshProUGUI CreateText(string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(transform, false);

            RectTransform childRect = obj.GetComponent<RectTransform>();
            childRect.anchorMin = anchorMin;
            childRect.anchorMax = anchorMax;
            childRect.offsetMin = Vector2.zero;
            childRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
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

            PlayThunderEffect();
            spawner.DamageAllActiveDucks(Constants.ZEUS_THUNDER_DAMAGE, Constants.WeaponType.TeslaGun);
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

        private void SetPositionFromScreen(Vector2 screenPosition)
        {
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null)
                return;

            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, uiCamera, out Vector2 localPoint))
                rectTransform.anchoredPosition = localPoint;
        }

        private void ReturnHome()
        {
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
            gameObject.SetActive(count > 0);
            if (countText != null)
                countText.text = $"x{count}";

            if (backgroundImage != null)
                backgroundImage.color = CanDrag()
                    ? new Color(0.08f, 0.13f, 0.24f, 0.92f)
                    : new Color(0.08f, 0.08f, 0.09f, 0.55f);
        }
    }
}
