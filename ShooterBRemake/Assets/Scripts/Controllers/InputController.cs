using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterB
{
    public class InputController : MonoBehaviour
    {
        [Header("References")]
        public Camera gameCamera;
        public ShooterController shooterController;

        private void Start()
        {
            if (gameCamera == null)
                gameCamera = Camera.main;

            if (shooterController == null)
            {
                shooterController = FindObjectOfType<ShooterController>();
            }

            Debug.Log($"[INPUT] InputController initialized");
        }

        private void Update()
        {
            if (GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver)
                return;

            HandleInput();
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

            if (shouldShoot && shooterController != null)
            {
                shooterController.Shoot(worldPosition);
            }
        }
    }
}
