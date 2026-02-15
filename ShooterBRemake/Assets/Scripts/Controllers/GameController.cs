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

        private void Start()
        {
            SetupCamera();

            if (GameManager.Instance.Score == 0)
            {
                GameManager.Instance.InitializeGame(Constants.GameMode.Normal);
            }

            ApplyBackground();

            Debug.Log($"GameController started - Score: {GameManager.Instance.Score}, Lives: {GameManager.Instance.Lives}, Difficulty: {GameManager.Instance.Difficulty}");
        }

        private void SetupCamera()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = Constants.CAMERA_HEIGHT / 2f / 100f;

                Debug.Log($"Camera configured - Orthographic size: {mainCamera.orthographicSize}");
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
                Debug.LogWarning("[GameController] Background renderer is missing.");
                return;
            }

            Sprite background = BackgroundManager.GetBackgroundForMode(GameManager.Instance.CurrentGameMode);
            if (background == null)
            {
                Debug.LogWarning("[GameController] Background sprite could not be loaded.");
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
        }

        private void Update()
        {
            // TODO: Implement new Input System for Escape key
            // Temporarily disabled to avoid Input System error
            /*
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (GameManager.Instance.IsPaused)
                    GameManager.Instance.ResumeGame();
                else
                    GameManager.Instance.PauseGame();
            }
            */
        }
    }
}
