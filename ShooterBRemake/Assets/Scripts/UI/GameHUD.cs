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

        private readonly List<Image> ammoBulletIcons = new List<Image>();

        private void Start()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);

            if (shooterController == null)
                shooterController = FindObjectOfType<ShooterController>();

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
            if (ammoContainer == null || ammoBulletIconPrefab == null || shooterController == null)
                return;

            foreach (Transform child in ammoContainer)
                Destroy(child.gameObject);

            ammoBulletIcons.Clear();

            int maxAmmo = shooterController.GetMaxAmmo();
            for (int i = 0; i < maxAmmo; i++)
            {
                Image icon = Instantiate(ammoBulletIconPrefab, ammoContainer);
                ammoBulletIcons.Add(icon);
            }
        }

        private void UpdateAmmoDisplay()
        {
            if (shooterController == null || ammoBulletIcons.Count == 0)
                return;

            int currentAmmo = shooterController.GetCurrentAmmo();

            for (int i = 0; i < ammoBulletIcons.Count; i++)
            {
                Image icon = ammoBulletIcons[i];
                if (icon == null)
                    continue;

                Color c = icon.color;
                c.a = i < currentAmmo ? 1f : 0f;
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
