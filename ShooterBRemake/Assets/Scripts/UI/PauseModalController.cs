using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class PauseModalController : MonoBehaviour
    {
        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Buttons")]
        public Button resumeButton;
        public Button restartButton;
        public Button menuButton;

        private void Start()
        {
            if (modalRoot == null)
                modalRoot = gameObject;

            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);

            Hide();
        }

        public void Show()
        {
            if (GameManager.Instance.IsGameOver)
                return;

            if (modalRoot != null)
                modalRoot.SetActive(true);

            GameManager.Instance.PauseGame();
        }

        public void Hide()
        {
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (GameManager.Instance.IsGameOver)
                return;

            if (GameManager.Instance.IsPaused)
                OnResumeClicked();
            else
                Show();
        }

        public void OnResumeClicked()
        {
            GameManager.Instance.ResumeGame();
            Hide();
        }

        public void OnRestartClicked()
        {
            SceneController.Instance.ReloadCurrentGameScene();
        }

        public void OnMenuClicked()
        {
            SceneController.Instance.ReturnToMenu();
        }
    }
}
