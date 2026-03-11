using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class MenuSettingsModalController : MonoBehaviour
    {
        [Header("UI Refs")]
        public GameObject modalRoot;
        public Button closeButton;
        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;

        [Header("Optional Text Refs")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI masterLabelText;
        public TextMeshProUGUI musicLabelText;
        public TextMeshProUGUI sfxLabelText;
        public TextMeshProUGUI languageLabelText;
        public TextMeshProUGUI closeButtonText;

        private bool isApplyingValues;
        private bool isInitialized;
        private ModalDialogAnimator modalAnimator;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;

            if (AudioSettingsManager.HasInstance)
                AudioSettingsManager.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
        }

        public void Open()
        {
            EnsureInitialized();

            if (modalRoot == null)
            {
                GameLog.Warning("[MenuSettingsModalController] modalRoot is not assigned.");
                return;
            }

            ApplySlidersFromSettings();
            EnsureAnimator();

            if (modalAnimator != null)
                modalAnimator.Show();
            else
                modalRoot.SetActive(true);
        }

        public void Hide()
        {
            EnsureInitialized();

            if (modalRoot != null)
            {
                EnsureAnimator();

                if (modalAnimator != null)
                    modalAnimator.Hide();
                else
                    modalRoot.SetActive(false);
            }
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedTexts();
        }

        private void HandleAudioSettingsChanged()
        {
            ApplySlidersFromSettings();
        }

        private void RegisterListeners()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
            else
            {
                GameLog.Warning("[MenuSettingsModalController] closeButton is not assigned.");
            }

            if (masterSlider != null)
            {
                masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
                masterSlider.onValueChanged.AddListener(HandleMasterChanged);
            }
            else
            {
                GameLog.Warning("[MenuSettingsModalController] masterSlider is not assigned.");
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
                musicSlider.onValueChanged.AddListener(HandleMusicChanged);
            }
            else
            {
                GameLog.Warning("[MenuSettingsModalController] musicSlider is not assigned.");
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
                sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
            }
            else
            {
                GameLog.Warning("[MenuSettingsModalController] sfxSlider is not assigned.");
            }
        }

        private void ApplySlidersFromSettings()
        {
            isApplyingValues = true;

            if (masterSlider != null)
                masterSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.MasterVolume);

            if (musicSlider != null)
                musicSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.MusicVolume);

            if (sfxSlider != null)
                sfxSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.SfxVolume);

            isApplyingValues = false;
        }

        private void HandleMasterChanged(float value)
        {
            if (isApplyingValues)
                return;

            AudioSettingsManager.Instance.SetMaster(value);
        }

        private void HandleMusicChanged(float value)
        {
            if (isApplyingValues)
                return;

            AudioSettingsManager.Instance.SetMusic(value);
        }

        private void HandleSfxChanged(float value)
        {
            if (isApplyingValues)
                return;

            AudioSettingsManager.Instance.SetSfx(value);
        }

        private void RefreshLocalizedTexts()
        {
            if (titleText != null)
                titleText.text = LocalizationManager.Instance.Get("settings.title", "SETTINGS");

            if (masterLabelText != null)
                masterLabelText.text = LocalizationManager.Instance.Get("settings.master", "MASTER");

            if (musicLabelText != null)
                musicLabelText.text = LocalizationManager.Instance.Get("settings.music", "MUSIC");

            if (sfxLabelText != null)
                sfxLabelText.text = LocalizationManager.Instance.Get("settings.sfx", "SFX");

            if (languageLabelText != null)
                languageLabelText.text = LocalizationManager.Instance.Get("settings.language", "Language");

            if (closeButtonText != null)
                closeButtonText.text = LocalizationManager.Instance.Get("common.close", "Close");
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
                return;

            _ = LocalizationManager.Instance;
            _ = AudioSettingsManager.Instance;

            EnsureAnimator();
            RegisterListeners();
            ApplySlidersFromSettings();
            RefreshLocalizedTexts();

            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            AudioSettingsManager.Instance.OnAudioSettingsChanged += HandleAudioSettingsChanged;
            isInitialized = true;
        }

        private void EnsureAnimator()
        {
            if (modalRoot == null)
                return;

            modalAnimator = modalRoot.GetComponent<ModalDialogAnimator>();
            if (modalAnimator == null)
                modalAnimator = modalRoot.AddComponent<ModalDialogAnimator>();

            modalAnimator.modalRoot = modalRoot;

            if (modalAnimator.contentTarget == null)
            {
                Transform panelTransform = modalRoot.transform.Find("Panel");
                if (panelTransform != null)
                    modalAnimator.contentTarget = panelTransform as RectTransform;
            }
        }
    }
}
