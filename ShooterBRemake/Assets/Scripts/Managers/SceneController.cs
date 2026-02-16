using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ShooterB
{
    public class SceneController : MonoBehaviour
    {
        private static SceneController instance;
        public static SceneController Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneController");
                    instance = go.AddComponent<SceneController>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public Constants.SceneType CurrentScene { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(Constants.SceneType sceneType)
        {
            CurrentScene = sceneType;
            string sceneName = GetSceneName(sceneType);
            Debug.Log($"Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        public void LoadGameScene(Constants.GameMode mode)
        {
            CurrentScene = Constants.SceneType.Game;
            Debug.Log($"Loading game scene with mode: {mode}");

            GameManager.Instance.InitializeGame(mode);
            SceneManager.LoadScene(GetSceneName(Constants.SceneType.Game));
        }

        public void ReloadCurrentGameScene()
        {
            Time.timeScale = 1f;
            Constants.GameMode mode = GameManager.Instance.CurrentGameMode;
            Debug.Log($"Reloading game scene with mode: {mode}");

            GameManager.Instance.InitializeGame(mode);
            SceneManager.LoadScene(GetSceneName(Constants.SceneType.Game));
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            Debug.Log("Returning to menu");
            LoadScene(Constants.SceneType.Menu);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private string GetSceneName(Constants.SceneType sceneType)
        {
            switch (sceneType)
            {
                case Constants.SceneType.Splash:
                    return "SplashScene";
                case Constants.SceneType.Menu:
                    return "MenuScene";
                case Constants.SceneType.Game:
                    return "GameScene";
                case Constants.SceneType.Loading:
                    return "LoadingScene";
                default:
                    return "MenuScene";
            }
        }
    }
}
