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

        private readonly List<Image> ammoBulletIcons = new List<Image>();
        private int lastKnownMaxAmmo = -1;
        private Constants.WeaponType? lastWeaponType = null;
        private Sprite lastAmmoSprite = null;

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
            UpdateAmmoDisplay();
            UpdateSelectedWeaponIcon();
        }

        private void Update()
        {
            UpdateAmmoDisplay();
            UpdateSelectedWeaponIcon();
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
                BuildAmmoIcons();

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
    }
}
