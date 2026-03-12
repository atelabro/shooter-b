using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class LanguageDropdownController : MonoBehaviour
    {
        private const string EnglishFlagResourcePath = "Flags/uk_flag";
        private const string MacedonianFlagResourcePath = "Flags/mk_flag";
        private const int PopupSortingOrder = 100;

        [Header("Controls")]
        public Button currentButton;
        public GameObject optionsRoot;
        public Button englishOptionButton;
        public Button macedonianOptionButton;

        [Header("Labels")]
        public TMP_Text currentLabel;
        public TMP_Text englishLabel;
        public TMP_Text macedonianLabel;

        [Header("Flags")]
        public Image currentFlagImage;
        public Image englishFlagImage;
        public Image macedonianFlagImage;
        public Sprite englishFlagSprite;
        public Sprite macedonianFlagSprite;
        public Image arrowIcon;
        public Sprite arrowDownSprite;
        public Sprite arrowUpSprite;

        private bool isInitialized;
        private RectTransform optionsRectTransform;
        private Canvas optionsCanvas;
        private GraphicRaycaster optionsRaycaster;

        public void Initialize()
        {
            if (isInitialized)
                return;

            AutoWireIfNeeded();
            LoadFlagSpritesFromResources();

            if (currentButton != null)
                currentButton.onClick.AddListener(ToggleOptions);

            if (englishOptionButton != null)
                englishOptionButton.onClick.AddListener(() => SelectLanguage(LocalizationManager.Language.English));

            if (macedonianOptionButton != null)
                macedonianOptionButton.onClick.AddListener(() => SelectLanguage(LocalizationManager.Language.Macedonian));

            if (optionsRoot != null)
                optionsRoot.SetActive(false);

            ApplyLanguage(LocalizationManager.Instance.CurrentLanguage);
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            isInitialized = true;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (currentButton != null)
                currentButton.onClick.RemoveListener(ToggleOptions);

            if (englishOptionButton != null)
                englishOptionButton.onClick.RemoveAllListeners();

            if (macedonianOptionButton != null)
                macedonianOptionButton.onClick.RemoveAllListeners();

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void AutoWireIfNeeded()
        {
            if (currentButton == null)
            {
                Transform child = transform.Find("CurrentButton");
                if (child != null)
                    currentButton = child.GetComponent<Button>();
            }

            if (optionsRoot == null)
            {
                Transform child = transform.Find("Options");
                if (child != null)
                    optionsRoot = child.gameObject;
            }

            if (englishOptionButton == null && optionsRoot != null)
            {
                Transform child = optionsRoot.transform.Find("EnglishOption");
                if (child != null)
                    englishOptionButton = child.GetComponent<Button>();
            }

            if (macedonianOptionButton == null && optionsRoot != null)
            {
                Transform child = optionsRoot.transform.Find("MacedonianOption");
                if (child != null)
                    macedonianOptionButton = child.GetComponent<Button>();
            }

            if (currentLabel == null && currentButton != null)
                currentLabel = currentButton.GetComponentInChildren<TMP_Text>(true);

            if (englishLabel == null && englishOptionButton != null)
                englishLabel = englishOptionButton.GetComponentInChildren<TMP_Text>(true);

            if (macedonianLabel == null && macedonianOptionButton != null)
                macedonianLabel = macedonianOptionButton.GetComponentInChildren<TMP_Text>(true);

            if (currentFlagImage == null && currentButton != null)
            {
                Transform child = currentButton.transform.Find("Flag");
                if (child != null)
                    currentFlagImage = child.GetComponent<Image>();
            }

            if (englishFlagImage == null && englishOptionButton != null)
            {
                Transform child = englishOptionButton.transform.Find("Flag");
                if (child != null)
                    englishFlagImage = child.GetComponent<Image>();
            }

            if (macedonianFlagImage == null && macedonianOptionButton != null)
            {
                Transform child = macedonianOptionButton.transform.Find("Flag");
                if (child != null)
                    macedonianFlagImage = child.GetComponent<Image>();
            }

            optionsRectTransform = optionsRoot != null ? optionsRoot.transform as RectTransform : null;
            EnsurePopupCanvas();
        }

        private void ToggleOptions()
        {
            if (optionsRoot == null)
                return;

            bool shouldOpen = !optionsRoot.activeSelf;
            if (shouldOpen)
            {
                OpenOptionsPopup();
            }
            else
            {
                CloseOptionsPopup();
            }

            UpdateArrowState();
        }

        private void SelectLanguage(LocalizationManager.Language language)
        {
            LocalizationManager.Instance.SetLanguage(language);
            CloseOptionsPopup();
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            ApplyLanguage(language);
        }

        private void ApplyLanguage(LocalizationManager.Language language)
        {
            if (currentLabel != null)
                currentLabel.text = LocalizationManager.GetLanguageCode(language);

            if (englishLabel != null)
                englishLabel.text = "EN";

            if (macedonianLabel != null)
                macedonianLabel.text = "MK";

            if (englishFlagImage != null)
                englishFlagImage.sprite = englishFlagSprite;

            if (macedonianFlagImage != null)
                macedonianFlagImage.sprite = macedonianFlagSprite;

            if (currentFlagImage != null)
                currentFlagImage.sprite = language == LocalizationManager.Language.Macedonian ? macedonianFlagSprite : englishFlagSprite;

            UpdateArrowState();
        }

        private void LoadFlagSpritesFromResources()
        {
            Sprite loadedEnglish = Resources.Load<Sprite>(EnglishFlagResourcePath);
            if (loadedEnglish != null)
                englishFlagSprite = loadedEnglish;

            Sprite loadedMacedonian = Resources.Load<Sprite>(MacedonianFlagResourcePath);
            if (loadedMacedonian != null)
                macedonianFlagSprite = loadedMacedonian;
        }

        private void UpdateArrowState()
        {
            if (arrowIcon == null)
                return;

            bool isExpanded = optionsRoot != null && optionsRoot.activeSelf;
            if (isExpanded)
            {
                arrowIcon.sprite = arrowUpSprite != null ? arrowUpSprite : arrowDownSprite;
            }
            else
            {
                arrowIcon.sprite = arrowDownSprite != null ? arrowDownSprite : arrowUpSprite;
            }
        }

        private void OpenOptionsPopup()
        {
            if (optionsRoot == null || optionsRectTransform == null)
                return;

            SetPopupSorting(true);
            optionsRectTransform.SetAsLastSibling();
            optionsRoot.SetActive(true);
        }

        private void CloseOptionsPopup()
        {
            if (optionsRoot == null)
                return;

            optionsRoot.SetActive(false);
            SetPopupSorting(false);
            UpdateArrowState();
        }

        private void EnsurePopupCanvas()
        {
            if (optionsRoot == null)
                return;

            optionsCanvas = optionsRoot.GetComponent<Canvas>();
            if (optionsCanvas == null)
                optionsCanvas = optionsRoot.AddComponent<Canvas>();

            optionsCanvas.overrideSorting = false;
            optionsCanvas.sortingOrder = 0;

            optionsRaycaster = optionsRoot.GetComponent<GraphicRaycaster>();
            if (optionsRaycaster == null)
                optionsRaycaster = optionsRoot.AddComponent<GraphicRaycaster>();

            optionsRaycaster.enabled = false;
        }

        private void SetPopupSorting(bool isOpen)
        {
            if (optionsCanvas == null)
                return;

            optionsCanvas.overrideSorting = isOpen;
            optionsCanvas.sortingOrder = isOpen ? PopupSortingOrder : 0;

            if (optionsRaycaster != null)
                optionsRaycaster.enabled = isOpen;
        }
    }
}
