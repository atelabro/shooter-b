using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShooterB
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Score Display")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI multiplierText;

        [Header("Lives Display")]
        public TextMeshProUGUI livesText;
        public Transform livesContainer;
        public Image lifeIconPrefab;
        public Sprite lifeIconSprite;
        public Sprite lifeOffSprite;
        public float lifeIconSize = 80f;
        public float lifeIconSpacing = 8f;
        public int maxDisplayedLives = 5;
        public TextMeshProUGUI livesOverflowText;
        public float livesOverflowLeftPadding = 0f;

        [Header("Buttons")]
        public Button pauseButton;
        public Button menuButton;

        [Header("Ammo Display")]
        public ShooterController shooterController;
        public Image selectedWeaponIconImage;
        public Transform ammoContainer;
        public Image ammoBulletIconPrefab;
        public float ammoIconSize = 56f;
        public float ammoIconSpacing = 10f;
        public float ammoContainerWidth = 900f;

        private readonly List<Image> ammoBulletIcons = new List<Image>();
        private readonly List<Image> lifeIcons = new List<Image>();
        private int lastKnownMaxAmmo = -1;
        private Constants.WeaponType? lastWeaponType = null;
        private Sprite lastAmmoSprite = null;
        private int lastKnownLifeSlots = -1;
        private TextMeshProUGUI runtimeLivesOverflowText;
        private bool suppressValidationBuild = false;

        private void Start()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);

            if (shooterController == null)
                shooterController = FindObjectOfType<ShooterController>();

            if (ammoContainer == null)
            {
                GameObject containerObject = GameObject.Find("AmmoContainer");
                if (containerObject != null)
                    ammoContainer = containerObject.transform;
            }

            ResolveLivesContainer();
            BuildLivesIcons();
            BuildAmmoIcons();
            SubscribeToEvents();
            UpdateAllUI();

            if (livesText != null)
                livesText.gameObject.SetActive(false);

            Debug.Log("GameHUD initialized");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || suppressValidationBuild)
                return;

            if (livesContainer == null)
                return;

            EditorApplication.delayCall += () =>
            {
                if (this == null || Application.isPlaying || suppressValidationBuild)
                    return;

                suppressValidationBuild = true;
                BuildLivesIconsEditorPreview();
                suppressValidationBuild = false;
            };
        }
#endif

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnMultiplierChanged += UpdateMultiplier;
            GameManager.Instance.OnLivesChanged += UpdateLives;
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= UpdateScore;
                GameManager.Instance.OnMultiplierChanged -= UpdateMultiplier;
                GameManager.Instance.OnLivesChanged -= UpdateLives;
            }
        }

        private void UpdateAllUI()
        {
            UpdateScore(GameManager.Instance.Score);
            UpdateMultiplier(GameManager.Instance.Multiplier);
            UpdateLives(GameManager.Instance.Lives);
            UpdateHighScore();
            UpdateAmmoDisplay();
            UpdateSelectedWeaponIcon();
        }

        private void Update()
        {
            UpdateAmmoDisplay();
            UpdateSelectedWeaponIcon();
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
            EnsureAmmoContainerWidth();

            lastKnownMaxAmmo = maxAmmo;
            lastWeaponType = shooterController.GetActiveWeaponType();
            lastAmmoSprite = ammoSprite;

            for (int i = 0; i < maxAmmo; i++)
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
            {
                BuildAmmoIcons();
            }

            if (ammoBulletIcons.Count == 0)
                return;

            int currentAmmo = shooterController.GetCurrentAmmo();

            int spentAmmo = Mathf.Max(0, maxAmmo - currentAmmo);
            for (int i = 0; i < ammoBulletIcons.Count; i++)
            {
                Image icon = ammoBulletIcons[i];
                if (icon == null)
                    continue;

                Color c = icon.color;
                c.a = i >= spentAmmo ? 1f : 0f;
                icon.color = c;
            }
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

        private void ResolveLivesContainer()
        {
            if (livesContainer != null)
                return;

            Debug.LogWarning("[HUD] LivesContainer is not assigned on GameHUD. Assign it in Inspector.");
        }

        private void BuildLivesIcons()
        {
            if (livesContainer == null)
                return;

            EnsureLivesLayoutGroup();

            if (TryUseExistingLivesIcons())
            {
                EnsureLivesOverflowText();
                return;
            }

            foreach (Transform child in livesContainer)
                Destroy(child.gameObject);

            lifeIcons.Clear();

            int currentLives = GameManager.Instance != null ? GameManager.Instance.Lives : Constants.INITIAL_LIVES;
            int slots = GetDesiredLifeSlotCount(currentLives);
            lastKnownLifeSlots = slots;

            Sprite lifeSprite = GetLifeSprite();

            for (int i = 0; i < slots; i++)
            {
                Image icon = CreateLifeIcon(lifeSprite);
                lifeIcons.Add(icon);
            }

            EnsureLivesOverflowText();
        }

        private bool TryUseExistingLivesIcons()
        {
            lifeIcons.Clear();

            foreach (Transform child in livesContainer)
            {
                if (child == null)
                    continue;

                TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    continue;

                Image icon = child.GetComponent<Image>();
                if (icon == null)
                    continue;

                lifeIcons.Add(icon);
            }

            if (lifeIcons.Count == 0)
                return false;

            lastKnownLifeSlots = lifeIcons.Count;
            return true;
        }

#if UNITY_EDITOR
        private void BuildLivesIconsEditorPreview()
        {
            EnsureLivesLayoutGroup();
            ClearLivesContainerChildrenImmediate();
            lifeIcons.Clear();

            int slots = Mathf.Max(1, Mathf.Min(maxDisplayedLives, Constants.INITIAL_LIVES));
            lastKnownLifeSlots = slots;

            Sprite lifeSprite = GetLifeSprite();
            for (int i = 0; i < slots; i++)
            {
                Image icon = CreateLifeIcon(lifeSprite);
                lifeIcons.Add(icon);
            }

            EnsureLivesOverflowText();
            if (livesOverflowText != null)
            {
                livesOverflowText.gameObject.SetActive(false);
            }
        }

        private void ClearLivesContainerChildrenImmediate()
        {
            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < livesContainer.childCount; i++)
            {
                children.Add(livesContainer.GetChild(i).gameObject);
            }

            foreach (GameObject child in children)
            {
                if (child == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }
#endif

        private void EnsureLivesLayoutGroup()
        {
            HorizontalLayoutGroup layoutGroup = livesContainer.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = livesContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                layoutGroup.spacing = lifeIconSpacing;
                layoutGroup.childControlWidth = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
            }
        }

        private Sprite GetLifeSprite()
        {
            if (lifeIconSprite != null)
                return lifeIconSprite;

            if (lifeIconPrefab != null)
                return lifeIconPrefab.sprite;

            return null;
        }

        private Image CreateLifeIcon(Sprite sprite)
        {
            Image icon;

            if (lifeIconPrefab != null)
            {
                icon = Instantiate(lifeIconPrefab, livesContainer);
            }
            else
            {
                GameObject iconObject = new GameObject("LifeIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(livesContainer, false);
                icon = iconObject.GetComponent<Image>();
            }

            RectTransform rect = icon.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(lifeIconSize, lifeIconSize);

            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.enabled = sprite != null;
            return icon;
        }

        private void EnsureLivesOverflowText()
        {
            if (livesContainer == null)
                return;

            if (livesOverflowText != null)
                return;

            if (runtimeLivesOverflowText == null)
            {
                GameObject overflowObj = new GameObject("LivesOverflowText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
                overflowObj.transform.SetParent(livesContainer, false);
                runtimeLivesOverflowText = overflowObj.GetComponent<TextMeshProUGUI>();
                runtimeLivesOverflowText.fontSize = 42;
                runtimeLivesOverflowText.color = Color.white;
                runtimeLivesOverflowText.alignment = TextAlignmentOptions.Center;
                runtimeLivesOverflowText.raycastTarget = false;

                LayoutElement layoutElement = overflowObj.GetComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
            }

            livesOverflowText = runtimeLivesOverflowText;
            livesOverflowText.transform.SetAsFirstSibling();
            RectTransform overflowRect = livesOverflowText.rectTransform;
            overflowRect.anchorMin = new Vector2(0f, 1f);
            overflowRect.anchorMax = new Vector2(0f, 1f);
            overflowRect.pivot = new Vector2(0f, 1f);
            overflowRect.anchoredPosition = new Vector2(livesOverflowLeftPadding, 0f);
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

        private void UpdateScore(long score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";

                if (GameManager.Instance.IsNewHighScore())
                    scoreText.color = Color.green;
                else
                    scoreText.color = Color.white;
            }
        }

        private void UpdateHighScore()
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"High: {GameManager.Instance.HighScore}";
                highScoreText.color = Color.green;
            }
        }

        private void UpdateMultiplier(int multiplier)
        {
            if (multiplierText != null)
            {
                multiplierText.text = $"x{multiplier}";
                multiplierText.color = Constants.MultiplierColors.GetColor(multiplier);
            }
        }

        private void UpdateLives(int lives)
        {
            if (livesContainer == null)
            {
                ResolveLivesContainer();
                BuildLivesIcons();
            }

            int slots = GetDesiredLifeSlotCount(lives);
            if (slots != lastKnownLifeSlots)
            {
                BuildLivesIcons();
            }

            if (lifeIcons.Count == 0)
                return;

            int shownLives = Mathf.Clamp(lives, 0, lifeIcons.Count);
            Sprite availableSprite = GetLifeSprite();
            Sprite lostSprite = lifeOffSprite != null ? lifeOffSprite : availableSprite;
            bool hasOverflowLives = lives > lifeIcons.Count;
            if (hasOverflowLives)
                shownLives = lifeIcons.Count;
            int lostCount = lifeIcons.Count - shownLives;

            for (int i = 0; i < lifeIcons.Count; i++)
            {
                Image icon = lifeIcons[i];
                if (icon == null)
                    continue;

                bool isLost = i < lostCount;
                icon.sprite = isLost ? lostSprite : availableSprite;
                icon.enabled = icon.sprite != null;
            }

            EnsureLivesOverflowText();
            if (livesOverflowText != null)
            {
                livesOverflowText.gameObject.SetActive(hasOverflowLives);
                if (hasOverflowLives)
                    livesOverflowText.text = lives.ToString();
            }
        }

        private int GetDesiredLifeSlotCount(int lives)
        {
            int baseLives = Mathf.Max(1, Constants.INITIAL_LIVES);
            int maxSlots = Mathf.Max(1, maxDisplayedLives);
            int dynamicSlots = Mathf.Max(baseLives, lives, lastKnownLifeSlots);
            return Mathf.Clamp(dynamicSlots, 1, maxSlots);
        }

        private void OnPauseClicked()
        {
            if (GameManager.Instance.IsPaused)
                GameManager.Instance.ResumeGame();
            else
                GameManager.Instance.PauseGame();

            Debug.Log($"Pause toggled - IsPaused: {GameManager.Instance.IsPaused}");
        }

        private void OnMenuClicked()
        {
            Debug.Log("Menu button clicked");
            SceneController.Instance.ReturnToMenu();
        }
    }
}
