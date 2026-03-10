using UnityEngine;

namespace ShooterB
{
    public class GameController : MonoBehaviour
    {
        [Header("Camera")]
        public Camera mainCamera;
        public SpriteRenderer backgroundRenderer;
        public bool useBackgroundShade = true;
        [Range(0f, 0.5f)] public float backgroundShade = 0.25f;
        public bool backgroundFillScreen = false;
        public bool alignBackgroundTopLeft = true;

        [Header("Modals")]
        public PauseModalController pauseModalController;
        public GameOverModalController gameOverModalController;

        [Header("Game Start Modal")]
        public GameObject gameStartingModalPanel;
        public GameStartingModalController gameStartingModalController;

        private void Start()
        {
            SetupCamera();

            ApplyBackground();
            ResolveModalReferences();
            ConfigureGameStartingModal();
            HideModals();
            GameManager.Instance.OnGameOver += HandleGameOver;
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;

            GameLog.Log($"GameController started - Score: {GameManager.Instance.Score}, Lives: {GameManager.Instance.Lives}, Difficulty: {GameManager.Instance.Difficulty}");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= HandleGameOver;
            }

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void SetupCamera()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = Constants.CAMERA_HEIGHT / 2f / 100f;

                GameLog.Log($"Camera configured - Orthographic size: {mainCamera.orthographicSize}");
            }
        }

        private void ApplyBackground()
        {
            if (backgroundRenderer == null)
            {
                GameObject backgroundObject = GameObject.Find("Background");
                if (backgroundObject != null)
                    backgroundRenderer = backgroundObject.GetComponent<SpriteRenderer>();
            }

            if (backgroundRenderer == null)
            {
                GameLog.Warning("[GameController] Background renderer is missing.");
                return;
            }

            Sprite background = BackgroundManager.GetBackgroundForMode(GameManager.Instance.CurrentGameMode);
            if (background == null)
            {
                GameLog.Warning("[GameController] Background sprite could not be loaded.");
                return;
            }

            backgroundRenderer.sprite = background;

            if (useBackgroundShade)
            {
                float s = Mathf.Clamp01(backgroundShade);
                float tint = 1f - s;
                backgroundRenderer.color = new Color(tint, tint, tint, 1f);
            }
            else
            {
                backgroundRenderer.color = Color.white;
            }

            FitBackgroundToCamera();
        }

        private void FitBackgroundToCamera()
        {
            if (backgroundRenderer == null || backgroundRenderer.sprite == null || mainCamera == null)
                return;

            Vector2 spriteSize = backgroundRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;

            float widthRatio = cameraWidth / spriteSize.x;
            float heightRatio = cameraHeight / spriteSize.y;
            float scale = backgroundFillScreen
                ? Mathf.Max(widthRatio, heightRatio) // cover (can crop)
                : Mathf.Min(widthRatio, heightRatio); // contain (no crop)

            backgroundRenderer.transform.localScale = new Vector3(scale, scale, 1f);

            if (alignBackgroundTopLeft)
                AlignBackgroundTopLeft();
        }

        private void AlignBackgroundTopLeft()
        {
            if (backgroundRenderer == null || mainCamera == null)
                return;

            Vector3 cameraPosition = mainCamera.transform.position;
            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            Vector2 backgroundSize = backgroundRenderer.bounds.size;

            float x = cameraPosition.x - (cameraWidth * 0.5f) + (backgroundSize.x * 0.5f);
            float y = cameraPosition.y + (cameraHeight * 0.5f) - (backgroundSize.y * 0.5f);

            Vector3 position = backgroundRenderer.transform.position;
            backgroundRenderer.transform.position = new Vector3(x, y, position.z);
        }

        private void ResolveModalReferences()
        {
            if (pauseModalController == null)
                pauseModalController = FindObjectOfType<PauseModalController>(true);

            if (gameOverModalController == null)
                gameOverModalController = FindObjectOfType<GameOverModalController>(true);

            if (gameStartingModalController == null && gameStartingModalPanel != null)
                gameStartingModalController = gameStartingModalPanel.GetComponent<GameStartingModalController>();

            if (gameStartingModalController == null)
                gameStartingModalController = FindObjectOfType<GameStartingModalController>(true);

            if (pauseModalController == null)
                GameLog.Warning("[GameController] PauseModalController not found in scene.");

            if (gameOverModalController == null)
                GameLog.Warning("[GameController] GameOverModalController not found in scene.");

            if (gameStartingModalController == null)
                GameLog.Warning("[GameController] GameStartingModalController not found in scene.");
        }

        private void ConfigureGameStartingModal()
        {
            if (gameStartingModalController == null)
                return;

            StageConfig activeStage = CampaignProgressManager.Instance.ActiveStageConfig;
            if (activeStage == null)
                return;

            gameStartingModalController.Configure(
                CampaignLocalizationResolver.GetStageName(activeStage),
                CampaignLocalizationResolver.GetStageBriefing(activeStage));
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            ConfigureGameStartingModal();
        }

        private void HideModals()
        {
            if (pauseModalController != null)
                pauseModalController.Hide();

            if (gameOverModalController != null)
                gameOverModalController.Hide();

            if (gameStartingModalPanel != null)
                gameStartingModalPanel.SetActive(false);
            else if (gameStartingModalController != null && gameStartingModalController.modalRoot != null)
                gameStartingModalController.modalRoot.SetActive(false);
        }

        private void HandleGameOver()
        {
            if (gameOverModalController == null)
            {
                GameLog.Warning("[GameController] Cannot show game-over modal. Reference is missing.");
                return;
            }

            gameOverModalController.Show(
                GameManager.Instance.Score,
                GameManager.Instance.HighScore,
                GameManager.Instance.CurrentGameMode,
                GameManager.Instance.IsNewHighScore());
        }
    }
}
