using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShooterB
{
    public class UIClickSoundManager : MonoBehaviour
    {
        private const string ClickClipResourcePath = "Audio/click";

        private static UIClickSoundManager instance;
        public static UIClickSoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("UIClickSoundManager");
                    instance = go.AddComponent<UIClickSoundManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        [SerializeField] private float rescanIntervalSeconds = 0.75f;

        private AudioSource sfxSource;
        private AudioClip clickClip;
        private bool hasLoggedMissingClickClip;
        private float nextRescanAt;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioReady();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            AttachEmittersToAllButtonsInScene();
            nextRescanAt = Time.unscaledTime + Mathf.Max(0.2f, rescanIntervalSeconds);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRescanAt)
                return;

            nextRescanAt = Time.unscaledTime + Mathf.Max(0.2f, rescanIntervalSeconds);
            AttachEmittersToAllButtonsInScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void PlayClick()
        {
            EnsureAudioReady();
            if (sfxSource == null || clickClip == null)
                return;

            sfxSource.PlayOneShot(clickClip);
        }

        private void EnsureAudioReady()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.GetComponent<AudioSource>();
                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();

                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.volume = 1f;
            }

            if (clickClip != null)
                return;

            clickClip = Resources.Load<AudioClip>(ClickClipResourcePath);
            if (clickClip == null && !hasLoggedMissingClickClip)
            {
                hasLoggedMissingClickClip = true;
                Debug.LogWarning($"[UIClickSoundManager] Missing click SFX clip at Resources/{ClickClipResourcePath}.");
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachEmittersToAllButtonsInScene();
        }

        private static void AttachEmittersToAllButtonsInScene()
        {
            Button[] buttons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                if (button.gameObject.GetComponent<UIClickSoundEmitter>() == null)
                    button.gameObject.AddComponent<UIClickSoundEmitter>();
            }
        }
    }

    public class UIClickSoundEmitter : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!CanPlayClick())
                return;

            UIClickSoundManager.Instance.PlayClick();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!CanPlayClick())
                return;

            UIClickSoundManager.Instance.PlayClick();
        }

        private bool CanPlayClick()
        {
            if (button == null)
                button = GetComponent<Button>();

            return button != null && button.IsActive() && button.IsInteractable();
        }
    }
}
