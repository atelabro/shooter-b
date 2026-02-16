using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace ShooterB
{
    public class InputController : MonoBehaviour
    {
        [Header("References")]
        public Camera gameCamera;
        public ShooterController shooterController;
        public PauseModalController pauseModalController;

        private void Start()
        {
            if (gameCamera == null)
                gameCamera = Camera.main;

            if (shooterController == null)
            {
                shooterController = FindObjectOfType<ShooterController>();
            }

            if (pauseModalController == null)
            {
                pauseModalController = FindObjectOfType<PauseModalController>(true);
            }

            Debug.Log($"[INPUT] InputController initialized");
        }

        private void Update()
        {
            HandlePauseInput();

            if (GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver)
                return;

            HandleInput();
        }

        private void HandlePauseInput()
        {
            if (GameManager.Instance.IsGameOver || Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (pauseModalController == null)
                    pauseModalController = FindObjectOfType<PauseModalController>(true);

                if (pauseModalController != null)
                {
                    pauseModalController.Toggle();
                }
                else
                {
                    Debug.LogWarning("[INPUT] PauseModalController not found for Escape handling.");
                }
            }
        }

        private void HandleInput()
        {
            Vector2 worldPosition = Vector2.zero;
            bool shouldShoot = false;

            float distanceFromCamera = Mathf.Abs(gameCamera.transform.position.z - (-5));

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                    worldPosition = gameCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, distanceFromCamera));
                    shouldShoot = true;
                }
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                worldPosition = gameCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceFromCamera));
                shouldShoot = true;
            }

            if (shouldShoot && !IsPointerOverUI() && shooterController != null)
            {
                shooterController.Shoot(worldPosition);
            }
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
                return false;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                return EventSystem.current.IsPointerOverGameObject(touchId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
