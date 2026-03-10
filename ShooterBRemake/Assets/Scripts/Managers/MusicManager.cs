using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShooterB
{
    public class MusicManager : MonoBehaviour
    {
        private const string CampaignSceneName = "CampaignGameScene";
        private const string CampaignClipResourcePath = "Audio/London Duck Hunt Loop";

        private static MusicManager instance;
        public static MusicManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("MusicManager");
                    instance = go.AddComponent<MusicManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        private AudioSource musicSource;
        private AudioClip campaignClip;
        private bool hasLoggedMissingCampaignClip;
        private GameManager cachedGameManager;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSource();
            EnsureCampaignClipLoaded();

            SceneManager.sceneLoaded += HandleSceneLoaded;
            cachedGameManager = GameManager.Instance;
            cachedGameManager.OnPauseStateChanged += HandlePauseStateChanged;
            AudioSettingsManager.Instance.OnAudioSettingsChanged += HandleAudioSettingsChanged;

            ApplySceneMusicPolicy(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (cachedGameManager != null)
                cachedGameManager.OnPauseStateChanged -= HandlePauseStateChanged;

            if (AudioSettingsManager.HasInstance)
                AudioSettingsManager.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
        }

        public void PlayCampaignBgm()
        {
            EnsureAudioSource();
            EnsureCampaignClipLoaded();

            if (campaignClip == null)
            {
                if (!hasLoggedMissingCampaignClip)
                {
                    hasLoggedMissingCampaignClip = true;
                    GameLog.Warning($"[MusicManager] Missing campaign music clip at Resources/{CampaignClipResourcePath}.");
                }
                return;
            }

            if (musicSource.clip != campaignClip)
                musicSource.clip = campaignClip;

            musicSource.loop = true;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        public void StopCampaignBgm()
        {
            if (musicSource == null)
                return;

            if (musicSource.isPlaying || musicSource.time > 0f)
                musicSource.Stop();
        }

        public void SetPaused(bool paused)
        {
            if (musicSource == null)
                return;

            if (paused)
            {
                if (musicSource.isPlaying)
                    musicSource.Pause();
            }
            else
            {
                if (IsCampaignScene(SceneManager.GetActiveScene().name))
                    musicSource.UnPause();
            }
        }

        private void EnsureAudioSource()
        {
            if (musicSource != null)
                return;

            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = AudioSettingsManager.Instance.GetEffectiveMusicVolume() * 0.45f;
        }

        private void EnsureCampaignClipLoaded()
        {
            if (campaignClip != null)
                return;

            campaignClip = Resources.Load<AudioClip>(CampaignClipResourcePath);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneMusicPolicy(scene.name);
        }

        private void HandlePauseStateChanged(bool isPaused)
        {
            if (!IsCampaignScene(SceneManager.GetActiveScene().name))
                return;

            SetPaused(isPaused);
        }

        private void HandleAudioSettingsChanged()
        {
            if (musicSource == null)
                return;

            musicSource.volume = AudioSettingsManager.Instance.GetEffectiveMusicVolume() * 0.45f;
        }

        private void ApplySceneMusicPolicy(string sceneName)
        {
            if (IsCampaignScene(sceneName))
            {
                PlayCampaignBgm();
                if (cachedGameManager != null && cachedGameManager.IsPaused)
                    SetPaused(true);
                return;
            }

            StopCampaignBgm();
        }

        private static bool IsCampaignScene(string sceneName)
        {
            return sceneName == CampaignSceneName;
        }
    }
}
