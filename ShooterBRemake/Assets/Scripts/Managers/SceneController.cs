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
        private Constants.SceneType armoryReturnScene = Constants.SceneType.Menu;
        private Constants.SceneType achievementsReturnScene = Constants.SceneType.Menu;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentScene = ResolveActiveSceneType();
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

        public void LoadCampaignMapScene()
        {
            CurrentScene = Constants.SceneType.CampaignMap;
            Debug.Log("Loading campaign map scene");
            SceneManager.LoadScene(GetSceneName(Constants.SceneType.CampaignMap));
        }

        public void LoadArmoryScene()
        {
            Constants.SceneType requestedFrom = ResolveActiveSceneType();
            if (requestedFrom == Constants.SceneType.Splash && CurrentScene != Constants.SceneType.Splash)
                requestedFrom = CurrentScene;

            if (requestedFrom == Constants.SceneType.CampaignMap ||
                requestedFrom == Constants.SceneType.Menu ||
                requestedFrom == Constants.SceneType.Achievements)
            {
                armoryReturnScene = requestedFrom;
            }
            else
            {
                armoryReturnScene = Constants.SceneType.Menu;
            }

            CurrentScene = Constants.SceneType.Armory;
            Debug.Log("Loading armory scene");
            SceneManager.LoadScene(GetSceneName(Constants.SceneType.Armory));
        }

        public void ReturnFromArmory()
        {
            if (armoryReturnScene == Constants.SceneType.CampaignMap)
            {
                LoadCampaignMapScene();
                return;
            }

            if (armoryReturnScene == Constants.SceneType.Achievements)
            {
                LoadAchievementsScene();
                return;
            }

            ReturnToMenu();
        }

        private static Constants.SceneType ResolveActiveSceneType()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            switch (sceneName)
            {
                case "MenuScene":
                    return Constants.SceneType.Menu;
                case "CampaignMapScene":
                    return Constants.SceneType.CampaignMap;
                case "CampaignGameScene":
                    return Constants.SceneType.CampaignGame;
                case "ArmoryScene":
                    return Constants.SceneType.Armory;
                case "AchievementsScene":
                    return Constants.SceneType.Achievements;
                case "GameScene":
                    return Constants.SceneType.Game;
                case "LoadingScene":
                    return Constants.SceneType.Loading;
                case "SplashScene":
                default:
                    return Constants.SceneType.Splash;
            }
        }

        public void LoadAchievementsScene()
        {
            Constants.SceneType requestedFrom = ResolveActiveSceneType();
            if (requestedFrom == Constants.SceneType.Splash && CurrentScene != Constants.SceneType.Splash)
                requestedFrom = CurrentScene;

            if (requestedFrom == Constants.SceneType.CampaignMap ||
                requestedFrom == Constants.SceneType.Menu ||
                requestedFrom == Constants.SceneType.Armory)
            {
                achievementsReturnScene = requestedFrom;
            }
            else
            {
                achievementsReturnScene = Constants.SceneType.Menu;
            }

            CurrentScene = Constants.SceneType.Achievements;
            Debug.Log("Loading achievements scene");
            SceneManager.LoadScene(GetSceneName(Constants.SceneType.Achievements));
        }

        public void ReturnFromAchievements()
        {
            if (achievementsReturnScene == Constants.SceneType.CampaignMap)
            {
                LoadCampaignMapScene();
                return;
            }

            if (achievementsReturnScene == Constants.SceneType.Armory)
            {
                LoadArmoryScene();
                return;
            }

            ReturnToMenu();
        }

        public void LoadCampaignStage(StageConfig config)
        {
            CurrentScene = Constants.SceneType.CampaignGame;
            Debug.Log($"Loading campaign stage: {config.mapName} (index {config.stageIndex})");

            CampaignProgressManager.Instance.SetActiveStage(config);
            GameManager.Instance.InitializeGame(Constants.GameMode.Campaign, config.startingDifficulty);
            SceneManager.LoadScene(GetSceneName(Constants.SceneType.CampaignGame));
        }

        public void ReloadCurrentGameScene()
        {
            Time.timeScale = 1f;
            Constants.GameMode mode = GameManager.Instance.CurrentGameMode;
            Debug.Log($"Reloading game scene with mode: {mode}");

            GameManager.Instance.InitializeGame(mode);
            SceneManager.LoadScene(GetSceneName(CurrentScene));
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
                case Constants.SceneType.CampaignMap:
                    return "CampaignMapScene";
                case Constants.SceneType.Game:
                    return "GameScene";
                case Constants.SceneType.CampaignGame:
                    return "CampaignGameScene";
                case Constants.SceneType.Armory:
                    return "ArmoryScene";
                case Constants.SceneType.Achievements:
                    return "AchievementsScene";
                case Constants.SceneType.Loading:
                    return "LoadingScene";
                default:
                    return "MenuScene";
            }
        }
    }
}
