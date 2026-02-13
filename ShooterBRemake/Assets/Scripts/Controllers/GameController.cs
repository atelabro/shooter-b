using UnityEngine;

namespace ShooterB
{
    public class GameController : MonoBehaviour
    {
        [Header("Camera")]
        public Camera mainCamera;

        private void Start()
        {
            SetupCamera();

            if (GameManager.Instance.Score == 0)
            {
                GameManager.Instance.InitializeGame(Constants.GameMode.Normal);
            }

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
