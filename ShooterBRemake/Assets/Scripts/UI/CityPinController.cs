using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ShooterB
{
    public class CityPinController : MonoBehaviour
    {
        [Header("UI Elements")]
        public Image pinImage;
        public Image lockedOverlay;
        public TextMeshProUGUI cityNameText;
        public GameObject lockIcon;
        public Button pinButton;
        [Range(0f, 1f)] public float lockedBrightness = 0.55f;

        private Action onClickCallback;

        private void Awake()
        {
            EnsureReferences();
            EnsureCityNameLabel();
        }

        public void Initialize(CityConfig config, bool isUnlocked, Action onClick)
        {
            onClickCallback = onClick;

            if (cityNameText != null)
                cityNameText.text = config.cityName.ToUpperInvariant();

            if (pinImage != null)
            {
                Sprite sprite = null;
                if (!string.IsNullOrWhiteSpace(config.pinSpriteResourcePath))
                    sprite = Resources.Load<Sprite>(config.pinSpriteResourcePath);

                if (sprite != null)
                {
                    pinImage.sprite = sprite;
                    pinImage.preserveAspect = true;
                }
                else
                {
                    Debug.LogWarning($"[CityPinController] Missing pin sprite for city '{config.cityName}'. Path: '{config.pinSpriteResourcePath}'");
                }
            }

            if (lockIcon != null)
                lockIcon.SetActive(!isUnlocked);

            if (pinButton != null)
            {
                pinButton.interactable = isUnlocked;
                pinButton.onClick.RemoveAllListeners();
                pinButton.onClick.AddListener(() => onClickCallback?.Invoke());
            }

            if (pinImage != null)
            {
                pinImage.color = isUnlocked
                    ? Color.white
                    : new Color(lockedBrightness, lockedBrightness, lockedBrightness, 1f);
            }

            if (lockedOverlay != null)
                lockedOverlay.gameObject.SetActive(!isUnlocked);

            GetComponent<RectTransform>().anchoredPosition = config.pinPosition;
        }

        private void EnsureReferences()
        {
            if (pinImage == null)
            {
                Transform pinImageTransform = transform.Find("PinImage");
                pinImage = pinImageTransform != null
                    ? pinImageTransform.GetComponent<Image>()
                    : GetComponent<Image>();
            }

            if (lockedOverlay == null)
            {
                Transform lockedOverlayTransform = transform.Find("LockedOverlay");
                if (lockedOverlayTransform != null)
                    lockedOverlay = lockedOverlayTransform.GetComponent<Image>();
            }

            if (pinButton == null)
                pinButton = GetComponent<Button>();

            if (lockIcon == null)
            {
                Transform lockTransform = transform.Find("LockIcon");
                if (lockTransform != null)
                    lockIcon = lockTransform.gameObject;
            }
        }

        private void EnsureCityNameLabel()
        {
            if (cityNameText != null)
                return;

            cityNameText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (cityNameText != null)
                return;

            GameObject go = new GameObject("CityNameText", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(50f, 18f);

            cityNameText = go.AddComponent<TextMeshProUGUI>();
            cityNameText.alignment = TextAlignmentOptions.Center;
            cityNameText.enableWordWrapping = false;
            cityNameText.enableAutoSizing = true;
            cityNameText.fontSizeMin = 6f;
            cityNameText.fontSizeMax = 12f;
            cityNameText.color = new Color(1f, 0.87f, 0.2f, 1f);
            cityNameText.raycastTarget = false;
        }
    }
}
