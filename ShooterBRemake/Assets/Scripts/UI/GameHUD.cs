using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
        private int lastKnownMaxAmmo = -1;
        private Constants.WeaponType? lastWeaponType = null;
        private Sprite lastAmmoSprite = null;

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

            BuildAmmoIcons();
            SubscribeToEvents();
            UpdateAllUI();

            Debug.Log("GameHUD initialized");
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
            if (livesText != null)
            {
                livesText.text = $"Lives: {lives}";
            }
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
