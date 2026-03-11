using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ShooterB
{
    public class CampaignGameController : MonoBehaviour
    {
        [Header("Editor Testing")]
        public CityConfig editorFallbackCity;
        public StageConfig editorFallbackStage;

        [Header("Camera")]
        public Camera mainCamera;
        public SpriteRenderer backgroundRenderer;
        public bool backgroundFillScreen = false;
        public bool alignBackgroundTopLeft = true;

        [Header("Modals")]
        public PauseModalController pauseModalController;
        public GameOverModalController gameOverModalController;
        public StageCompleteModalController stageCompleteModalController;

        [Header("Game Start Modal")]
        public GameObject gameStartingModalPanel;
        public GameStartingModalController gameStartingModalController;

        [Header("Wave Announcement")]
        public WaveAnnouncementController waveAnnouncementController;

        private bool isStageComplete = false;
        private CampaignDuckSpawner campaignDuckSpawner;

        private void Start()
        {
            Time.timeScale = 1f;
            EnsureActiveStage();
            SetupCamera();
            ApplyCampaignBackground();
            ResolveModalReferences();
            ConfigureGameStartingModal();
            HideModals();

            GameManager.Instance.OnGameOver += HandleGameOver;
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;

            campaignDuckSpawner = FindObjectOfType<CampaignDuckSpawner>();
            if (campaignDuckSpawner != null)
            {
                campaignDuckSpawner.OnAllDucksResolved += HandleStageComplete;
                campaignDuckSpawner.OnWaveStarting += HandleWaveStarting;
                StartCoroutine(BeginStageAfterCountdown());
            }
            else
            {
                GameLog.Warning("[CampaignGameController] CampaignDuckSpawner not found in scene.");
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= HandleGameOver;

            if (campaignDuckSpawner != null)
            {
                campaignDuckSpawner.OnAllDucksResolved -= HandleStageComplete;
                campaignDuckSpawner.OnWaveStarting -= HandleWaveStarting;
            }

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void EnsureActiveStage()
        {
            if (CampaignProgressManager.Instance.ActiveStageConfig != null)
                return;

            if (editorFallbackStage == null)
            {
                GameLog.Warning("[CampaignGameController] No ActiveStageConfig and no editorFallbackStage assigned.");
                return;
            }

            GameLog.Log("[CampaignGameController] No ActiveStageConfig found - using editorFallbackStage for testing.");
            if (editorFallbackCity != null)
                CampaignProgressManager.Instance.SetActiveCampaignLocation(editorFallbackCity, editorFallbackStage);
            else
                CampaignProgressManager.Instance.SetActiveStage(editorFallbackStage);
            GameManager.Instance.InitializeGame(Constants.GameMode.Campaign, editorFallbackStage.startingDifficulty);
        }

        private void SetupCamera()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = Constants.CAMERA_HEIGHT / 2f / 100f;
            }
        }

        private void ApplyCampaignBackground()
        {
            if (backgroundRenderer == null)
            {
                GameObject backgroundObject = GameObject.Find("Background");
                if (backgroundObject != null)
                    backgroundRenderer = backgroundObject.GetComponent<SpriteRenderer>();
            }

            if (backgroundRenderer == null)
            {
                GameLog.Warning("[CampaignGameController] Background renderer is missing.");
                return;
            }

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;
            if (stage == null || stage.backgroundSprite == null)
            {
                GameLog.Warning("[CampaignGameController] No background sprite set on active stage config.");
                return;
            }

            backgroundRenderer.sprite = stage.backgroundSprite;
            backgroundRenderer.color = Color.white;

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
                ? Mathf.Max(widthRatio, heightRatio)
                : Mathf.Min(widthRatio, heightRatio);

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

            if (stageCompleteModalController == null)
                stageCompleteModalController = FindObjectOfType<StageCompleteModalController>(true);

            if (pauseModalController == null)
                GameLog.Warning("[CampaignGameController] PauseModalController not found in scene.");

            if (gameOverModalController == null)
                GameLog.Warning("[CampaignGameController] GameOverModalController not found in scene.");

            if (stageCompleteModalController == null)
                GameLog.Warning("[CampaignGameController] StageCompleteModalController not found in scene.");

            if (gameStartingModalPanel == null)
                GameLog.Warning("[CampaignGameController] gameStartingModalPanel is not assigned.");

            if (gameStartingModalController == null && gameStartingModalPanel != null)
                gameStartingModalController = gameStartingModalPanel.GetComponent<GameStartingModalController>();

            if (gameStartingModalController == null && gameStartingModalPanel != null)
                gameStartingModalController = gameStartingModalPanel.AddComponent<GameStartingModalController>();

            if (gameStartingModalController == null)
                GameLog.Warning("[CampaignGameController] gameStartingModalController is not assigned.");
        }

        private void HideModals()
        {
            if (pauseModalController != null)
                pauseModalController.Hide();

            if (gameOverModalController != null)
                gameOverModalController.Hide();

            if (stageCompleteModalController != null)
                stageCompleteModalController.Hide();
        }

        private void ConfigureGameStartingModal()
        {
            if (gameStartingModalController == null)
                return;

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;
            if (stage == null)
                stage = editorFallbackStage;

            if (stage == null)
                return;

            gameStartingModalController.Configure(
                CampaignLocalizationResolver.GetStageName(stage),
                CampaignLocalizationResolver.GetStageBriefing(stage));
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            ConfigureGameStartingModal();
        }

        private void Update()
        {
            HandleDebugStageCompleteShortcut();
        }

        private void HandleStageComplete()
        {
            if (isStageComplete)
                return;

            isStageComplete = true;
            Time.timeScale = 0f;
            GameManager.Instance.ResetConsecutiveFailedRuns();

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;

            if (stageCompleteModalController == null)
            {
                GameLog.Warning("[CampaignGameController] StageCompleteModalController not found.");
                return;
            }

            stageCompleteModalController.Show(stage, GameManager.Instance.Score);
        }

        private void HandleDebugStageCompleteShortcut()
        {
            if (isStageComplete)
                return;

            int forcedStars = GetDebugStageCompleteStars();
            if (forcedStars <= 0)
                return;

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;
            if (stage == null || stageCompleteModalController == null)
                return;

            isStageComplete = true;
            Time.timeScale = 0f;
            GameManager.Instance.ResetConsecutiveFailedRuns();
            stageCompleteModalController.ShowDebug(stage, forcedStars);
        }

        private static int GetDebugStageCompleteStars()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.yKey.wasPressedThisFrame)
                    return 1;

                if (Keyboard.current.uKey.wasPressedThisFrame)
                    return 2;

                if (Keyboard.current.iKey.wasPressedThisFrame)
                    return 3;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Y))
                return 1;

            if (Input.GetKeyDown(KeyCode.U))
                return 2;

            if (Input.GetKeyDown(KeyCode.I))
                return 3;
#endif

            return 0;
        }

        private void HandleWaveStarting(int waveNumber, float duration, string labelFormatOverride)
        {
            if (waveAnnouncementController != null)
                waveAnnouncementController.ShowWave(waveNumber, duration, labelFormatOverride);
        }

        private IEnumerator BeginStageAfterCountdown()
        {
            if (gameStartingModalController != null)
                yield return StartCoroutine(gameStartingModalController.PlayCountdown());

            if (campaignDuckSpawner != null)
            {
                GameLog.Log("[CampaignGameController] Countdown finished, starting campaign spawner.");
                campaignDuckSpawner.StartSpawning();
            }
        }

        private void HandleGameOver()
        {
            if (gameOverModalController == null)
            {
                GameLog.Warning("[CampaignGameController] Cannot show game-over modal. Reference is missing.");
                return;
            }

            gameOverModalController.Show(
                GameManager.Instance.Score,
                GameManager.Instance.HighScore,
                GameManager.Instance.CurrentGameMode,
                GameManager.Instance.IsNewHighScore()
            );
        }
    }
}
