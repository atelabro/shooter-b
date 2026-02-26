using UnityEngine;

namespace ShooterB
{
    public class CampaignGameController : MonoBehaviour
    {
        [Header("Editor Testing")]
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

        private bool isStageComplete = false;

        private void Start()
        {
            EnsureActiveStage();
            SetupCamera();
            ApplyCampaignBackground();
            ResolveModalReferences();
            HideModals();

            GameManager.Instance.OnGameOver += HandleGameOver;

            CampaignDuckSpawner spawner = FindObjectOfType<CampaignDuckSpawner>();
            if (spawner != null)
            {
                spawner.OnAllDucksResolved += HandleStageComplete;
                spawner.StartSpawning();
            }
            else
            {
                Debug.LogWarning("[CampaignGameController] CampaignDuckSpawner not found in scene.");
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= HandleGameOver;
        }

        private void EnsureActiveStage()
        {
            if (CampaignProgressManager.Instance.ActiveStageConfig != null)
                return;

            if (editorFallbackStage == null)
            {
                Debug.LogWarning("[CampaignGameController] No ActiveStageConfig and no editorFallbackStage assigned.");
                return;
            }

            Debug.Log("[CampaignGameController] No ActiveStageConfig found - using editorFallbackStage for testing.");
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
                Debug.LogWarning("[CampaignGameController] Background renderer is missing.");
                return;
            }

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;
            if (stage == null || stage.backgroundSprite == null)
            {
                Debug.LogWarning("[CampaignGameController] No background sprite set on active stage config.");
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
                Debug.LogWarning("[CampaignGameController] PauseModalController not found in scene.");

            if (gameOverModalController == null)
                Debug.LogWarning("[CampaignGameController] GameOverModalController not found in scene.");

            if (stageCompleteModalController == null)
                Debug.LogWarning("[CampaignGameController] StageCompleteModalController not found in scene.");
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

        private void HandleStageComplete()
        {
            if (isStageComplete)
                return;

            isStageComplete = true;
            Time.timeScale = 0f;

            StageConfig stage = CampaignProgressManager.Instance.ActiveStageConfig;

            if (stageCompleteModalController == null)
            {
                Debug.LogWarning("[CampaignGameController] StageCompleteModalController not found.");
                return;
            }

            stageCompleteModalController.Show(stage, GameManager.Instance.Score);
        }

        private void HandleGameOver()
        {
            if (gameOverModalController == null)
            {
                Debug.LogWarning("[CampaignGameController] Cannot show game-over modal. Reference is missing.");
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
