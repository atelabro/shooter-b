using System.Collections.Generic;
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

        private bool isPointerHeld;
        private PointerEventData _pointerEventData;

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

            _pointerEventData = new PointerEventData(EventSystem.current);
            GameLog.Log($"[INPUT] InputController initialized");
        }

        private void Update()
        {
            HandlePauseInput();

            if (GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver)
            {
                if (isPointerHeld && shooterController != null)
                    shooterController.EndFire();

                isPointerHeld = false;
                return;
            }

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
                    GameLog.Warning("[INPUT] PauseModalController not found for Escape handling.");
                }
            }
        }

        private void HandleInput()
        {
            float distanceFromCamera = Mathf.Abs(gameCamera.transform.position.z - (-5));
            bool hasTouch = Touchscreen.current != null;
            bool hasMouse = Mouse.current != null;

            bool pressedThisFrame = false;
            bool releasedThisFrame = false;
            bool isPressed = false;
            Vector2 screenPosition = Vector2.zero;

            if (hasTouch)
            {
                var touch = Touchscreen.current.primaryTouch;
                pressedThisFrame = touch.press.wasPressedThisFrame;
                releasedThisFrame = touch.press.wasReleasedThisFrame;
                isPressed = touch.press.isPressed;
                screenPosition = touch.position.ReadValue();
            }
            else if (hasMouse)
            {
                pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
                isPressed = Mouse.current.leftButton.isPressed;
                screenPosition = Mouse.current.position.ReadValue();
            }

            if (pressedThisFrame)
            {
                if (IsPointerOverUI(screenPosition))
                {
                    isPointerHeld = false;
                    return;
                }

                isPointerHeld = true;
                TryBeginFire(screenPosition, distanceFromCamera);
            }

            if (releasedThisFrame)
            {
                if (isPointerHeld && shooterController != null)
                    shooterController.EndFire();

                isPointerHeld = false;
                return;
            }

            if (isPressed && isPointerHeld)
            {
                TryUpdateFire(screenPosition, distanceFromCamera);
            }
        }

        private void TryBeginFire(Vector2 screenPosition, float distanceFromCamera)
        {
            if (shooterController == null)
                return;

            Vector2 worldPosition = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
            shooterController.BeginFire(worldPosition);
        }

        private void TryUpdateFire(Vector2 screenPosition, float distanceFromCamera)
        {
            if (shooterController == null)
                return;

            Vector2 worldPosition = gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
            shooterController.UpdateFire(worldPosition);
        }

        private static readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
                return false;

            _pointerEventData.position = screenPosition;

            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(_pointerEventData, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }
    }
}
