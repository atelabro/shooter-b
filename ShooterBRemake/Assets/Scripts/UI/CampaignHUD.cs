using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ShooterB
{
    public class CampaignHUD : MonoBehaviour
    {
        [Header("Score Display")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI multiplierText;


        [Header("Lives Display")]
        public LivesContainerController livesContainerController;
        public TextMeshProUGUI livesText;

        [Header("Buttons")]
        public Button pauseButton;
        public PauseModalController pauseModalController;

        [Header("Ammo Display")]
        public ShooterController shooterController;
        public Image selectedWeaponIconImage;
        public Transform ammoContainer;
        public Image ammoBulletIconPrefab;
        public float ammoIconSize = 56f;
        public float ammoIconSpacing = 10f;
        public float ammoContainerWidth = 900f;
        public int maxDisplayedAmmoIcons = 10;

        [Header("Reload Feedback")]
        public Image reloadGlowImage;
        public TextMeshProUGUI reloadingText;
        [Range(0f, 1f)] public float reloadGlowMinAlpha = 0.2f;
        [Range(0f, 1f)] public float reloadGlowMaxAlpha = 0.75f;
        public float reloadGlowPulseSpeed = 8f;
        public float reloadTextFlashSpeed = 12f;

        [Header("Combo Feedback")]
        public GameObject comboPopupPrefab;
        public Transform comboPopupContainer;
        public float comboPopupLifetime = 1f;

        private readonly List<Image> ammoBulletIcons = new List<Image>();
        private int lastKnownMaxAmmo = -1;
        private Constants.WeaponType? lastWeaponType = null;
        private Sprite lastAmmoSprite = null;
        private bool hasLoggedMissingReloadReferences = false;

        private void Start()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);

            if (shooterController == null)
                shooterController = FindObjectOfType<ShooterController>();

            if (livesContainerController == null)
                livesContainerController = FindObjectOfType<LivesContainerController>();

            if (ammoContainer == null)
            {
                GameObject containerObject = GameObject.Find("AmmoContainer");
                if (containerObject != null)
                    ammoContainer = containerObject.transform;
            }

            if (pauseModalController == null)
                pauseModalController = FindObjectOfType<PauseModalController>(true);

            InitializeReloadFeedback();

            BuildAmmoIcons();
            SubscribeToEvents();
            UpdateAllUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnMultiplierChanged += UpdateMultiplier;
            GameManager.Instance.OnLivesChanged += UpdateLives;
            GameManager.Instance.OnComboKill += HandleComboKill;
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= UpdateScore;
                GameManager.Instance.OnMultiplierChanged -= UpdateMultiplier;
                GameManager.Instance.OnLivesChanged -= UpdateLives;
                GameManager.Instance.OnComboKill -= HandleComboKill;
            }
        }

        private void UpdateAllUI()
        {
            UpdateScore(GameManager.Instance.Score);
            UpdateMultiplier(GameManager.Instance.Multiplier);
            UpdateLives(GameManager.Instance.Lives);
            UpdateAmmoDisplay();
            UpdateSelectedWeaponIcon();
            UpdateReloadFeedback();
        }

        private void Update()
        {
            UpdateAmmoDisplay();
            UpdateSelectedWeaponIcon();
            UpdateReloadFeedback();
        }

        private void UpdateScore(long score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }

        private void UpdateMultiplier(int multiplier)
        {
            if (multiplierText != null)
            {
                multiplierText.text = $"x{multiplier}";
                multiplierText.color = Constants.MultiplierColors.GetColor(multiplier);
                multiplierText.gameObject.SetActive(false);
            }
        }

        private void UpdateLives(int lives)
        {
            if (livesContainerController != null)
            {
                livesContainerController.SetLives(lives);
                return;
            }

            if (livesText != null)
                livesText.text = $"Lives: {lives}";
        }

        private void BuildAmmoIcons()
        {
            if (ammoContainer == null || shooterController == null)
                return;

            EnsureAmmoLayoutGroup();

            foreach (Transform child in ammoContainer)
                Destroy(child.gameObject);

            ammoBulletIcons.Clear();

            int maxAmmo = shooterController.GetMaxAmmo();
            Sprite ammoSprite = shooterController.GetActiveWeaponAmmoSprite();
            int displayIconCount = GetDisplayAmmoIconCount(maxAmmo);
            EnsureAmmoContainerWidth();

            lastKnownMaxAmmo = maxAmmo;
            lastWeaponType = shooterController.GetActiveWeaponType();
            lastAmmoSprite = ammoSprite;

            for (int i = 0; i < displayIconCount; i++)
            {
                Image icon = CreateAmmoIcon(ammoSprite);
                ammoBulletIcons.Add(icon);
            }
        }

        private void UpdateAmmoDisplay()
        {
            if (shooterController == null)
                return;

            int maxAmmo = shooterController.GetMaxAmmo();
            Constants.WeaponType? weaponType = shooterController.GetActiveWeaponType();
            Sprite ammoSprite = shooterController.GetActiveWeaponAmmoSprite();

            if (maxAmmo != lastKnownMaxAmmo || weaponType != lastWeaponType || ammoSprite != lastAmmoSprite)
                BuildAmmoIcons();

            if (ammoBulletIcons.Count == 0)
                return;

            int currentAmmo = shooterController.GetCurrentAmmo();
            int spentAmmoDisplay = GetSpentAmmoDisplayCount(maxAmmo, currentAmmo);

            for (int i = 0; i < ammoBulletIcons.Count; i++)
            {
                Image icon = ammoBulletIcons[i];
                if (icon == null)
                    continue;

                Color c = icon.color;
                c.a = i >= spentAmmoDisplay ? 1f : 0f;
                icon.color = c;
            }
        }

        private int GetDisplayAmmoIconCount(int maxAmmo)
        {
            int iconLimit = Mathf.Max(1, maxDisplayedAmmoIcons);
            return Mathf.Min(maxAmmo, iconLimit);
        }

        private int GetSpentAmmoDisplayCount(int maxAmmo, int currentAmmo)
        {
            int spentAmmo = Mathf.Max(0, maxAmmo - currentAmmo);
            int displayCount = GetDisplayAmmoIconCount(maxAmmo);
            if (displayCount <= 0 || maxAmmo <= displayCount)
                return spentAmmo;

            float chunkSize = (float)maxAmmo / displayCount;
            return Mathf.Clamp(Mathf.FloorToInt(spentAmmo / chunkSize), 0, displayCount);
        }

        private void UpdateSelectedWeaponIcon()
        {
            if (selectedWeaponIconImage == null)
                return;

            if (shooterController == null || shooterController.activeWeapon == null)
            {
                selectedWeaponIconImage.enabled = false;
                return;
            }

            Sprite icon = shooterController.activeWeapon.weaponIcon;
            if (icon == null)
            {
                selectedWeaponIconImage.enabled = false;
                return;
            }

            selectedWeaponIconImage.sprite = icon;
            selectedWeaponIconImage.enabled = true;
        }

        private void InitializeReloadFeedback()
        {
            if (reloadGlowImage != null)
                reloadGlowImage.gameObject.SetActive(false);

            if (reloadingText != null)
            {
                reloadingText.text = "reloading";
                reloadingText.gameObject.SetActive(false);
            }

            WarnMissingReloadReferencesIfNeeded();
        }

        private void UpdateReloadFeedback()
        {
            if (shooterController == null)
                return;

            WarnMissingReloadReferencesIfNeeded();

            bool isRefilling = shooterController.IsRefilling();

            if (reloadGlowImage != null)
            {
                reloadGlowImage.gameObject.SetActive(isRefilling);
                if (isRefilling)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * reloadGlowPulseSpeed);
                    float alpha = Mathf.Lerp(reloadGlowMinAlpha, reloadGlowMaxAlpha, pulse);
                    reloadGlowImage.color = new Color(1f, 0f, 0f, alpha);
                }
            }

            if (reloadingText != null)
            {
                reloadingText.gameObject.SetActive(isRefilling);
                if (isRefilling)
                {
                    float flash = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * reloadTextFlashSpeed);
                    float alpha = flash > 0.4f ? 1f : 0.15f;
                    reloadingText.color = new Color(1f, 0f, 0f, alpha);
                }
            }
        }

        private void WarnMissingReloadReferencesIfNeeded()
        {
            if (hasLoggedMissingReloadReferences)
                return;

            if (reloadGlowImage != null && reloadingText != null)
                return;

            Debug.LogWarning("[CampaignHUD] Reload feedback references are not fully assigned. Assign both reloadGlowImage and reloadingText in the inspector.");
            hasLoggedMissingReloadReferences = true;
        }

        private Image CreateAmmoIcon(Sprite sprite)
        {
            Image icon;

            if (ammoBulletIconPrefab != null)
            {
                icon = Instantiate(ammoBulletIconPrefab, ammoContainer);
            }
            else
            {
                GameObject iconObject = new GameObject("AmmoIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(ammoContainer, false);
                icon = iconObject.GetComponent<Image>();
            }

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            if (iconRect != null)
                iconRect.sizeDelta = new Vector2(ammoIconSize, ammoIconSize);

            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.enabled = sprite != null;
            return icon;
        }

        private void EnsureAmmoLayoutGroup()
        {
            HorizontalLayoutGroup layoutGroup = ammoContainer.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = ammoContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

            layoutGroup.childAlignment = TextAnchor.MiddleRight;
            layoutGroup.spacing = ammoIconSpacing;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
        }

        private void EnsureAmmoContainerWidth()
        {
            RectTransform containerRect = ammoContainer as RectTransform;
            if (containerRect == null)
                return;

            Vector2 size = containerRect.sizeDelta;
            size.x = ammoContainerWidth;
            containerRect.sizeDelta = size;
        }

        private void OnPauseClicked()
        {
            if (pauseModalController != null)
                pauseModalController.Show();
            else
                Debug.LogWarning("[CampaignHUD] Pause button pressed but PauseModalController is missing.");
        }

        private void HandleComboKill(Constants.MultiKillType type, int bonusPoints, Vector3 comboWorldPosition)
        {
            if (comboPopupPrefab == null)
                return;

            Transform parent = comboPopupContainer != null ? comboPopupContainer : transform;
            GameObject popup = Instantiate(comboPopupPrefab, parent);
            string comboLabel = GetComboLabel(type);
            ComboPopupController popupController = popup.GetComponent<ComboPopupController>();
            if (popupController != null)
            {
                popupController.Configure(type, comboLabel);
            }
            else
            {
                TextMeshProUGUI popupText = popup.GetComponent<TextMeshProUGUI>();
                if (popupText != null)
                {
                    popupText.text = comboLabel;
                    popupText.color = GetComboColor(type);
                }
            }

            PositionPopupAtWorldPoint(popup, parent, comboWorldPosition);

            Destroy(popup, Mathf.Max(0.1f, comboPopupLifetime));
        }

        private void PositionPopupAtWorldPoint(GameObject popup, Transform parent, Vector3 worldPosition)
        {
            if (popup == null || parent == null)
                return;

            RectTransform popupRect = popup.GetComponent<RectTransform>();
            if (popupRect == null)
                return;

            RectTransform parentRect = parent as RectTransform;
            if (parentRect == null)
            {
                Canvas fallbackCanvas = parent.GetComponentInParent<Canvas>();
                parentRect = fallbackCanvas != null ? fallbackCanvas.transform as RectTransform : transform as RectTransform;
            }

            if (parentRect == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                cam = FindObjectOfType<Camera>();

            Vector2 screenPoint = cam != null
                ? cam.WorldToScreenPoint(worldPosition)
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Camera uiCamera = null;
            Canvas canvas = parentRect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera != null ? canvas.worldCamera : cam;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint))
            {
                popupRect.anchoredPosition = localPoint;
            }
            else
            {
                // Fallback keeps popup away from the screen center when conversion fails.
                popupRect.anchoredPosition = new Vector2(0f, 120f);
            }
        }

        private static string GetComboLabel(Constants.MultiKillType type)
        {
            switch (type)
            {
                case Constants.MultiKillType.DoubleKill:
                    return "DOUBLE KILL";
                case Constants.MultiKillType.TripleKill:
                    return "TRIPLE KILL";
                case Constants.MultiKillType.QuadraKill:
                    return "QUADRA KILL";
                default:
                    return "COMBO";
            }
        }

        private static Color GetComboColor(Constants.MultiKillType type)
        {
            switch (type)
            {
                case Constants.MultiKillType.DoubleKill:
                    return new Color(0.3820755f, 1f, 0.51313657f, 1f);
                case Constants.MultiKillType.TripleKill:
                    return new Color(1f, 0.302f, 0.302f, 1f);
                case Constants.MultiKillType.QuadraKill:
                    return new Color(0.705f, 0.424f, 1f, 1f);
                default:
                    return Color.white;
            }
        }
    }
}
