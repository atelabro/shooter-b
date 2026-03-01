using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class ArmoryController : MonoBehaviour
    {
        [Serializable]
        public class WeaponListItem
        {
            public Constants.WeaponType weaponType;
            public Button button;
            public TextMeshProUGUI weaponNameText;
            public Image weaponImage;
            public GameObject selectedCheckmark;
        }

        [Serializable]
        public class WeaponIconEntry
        {
            public Constants.WeaponType weaponType;
            public Sprite icon;
        }

        private static readonly Constants.WeaponType[] SelectableWeapons =
        {
            Constants.WeaponType.Rifle,
            Constants.WeaponType.Cabirne,
            Constants.WeaponType.Beretta,
            Constants.WeaponType.PiranhaGun,
            Constants.WeaponType.TeslaGun,
            Constants.WeaponType.MrSulko
        };

        [Header("UI Elements")]
        public Button quitButton;
        public ScrollRect weaponsScrollRect;
        public RectTransform weaponListContent;
        public Button weaponRowTemplate;
        public WeaponListItem[] weaponListItems;
        public WeaponIconEntry[] weaponIcons;
        public Weapon rifleWeaponPrefab;
        public Weapon cabirneWeaponPrefab;
        public Weapon piranhaWeaponPrefab;
        public Weapon teslaWeaponPrefab;
        public Weapon mrSulkoWeaponPrefab;
        public Weapon berettaWeaponPrefab;

        [Header("Selection Colors")]
        public Color selectedButtonColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        public Color normalButtonColor = Color.white;

        private readonly List<WeaponListItem> activeWeaponItems = new List<WeaponListItem>();
        private readonly HashSet<Constants.WeaponType> missingIconWarnings = new HashSet<Constants.WeaponType>();

        private void Start()
        {
            ConfigureBackButton();
            EnsureScrollSetup();
            BuildWeaponList();
            RefreshSelectionUI();
        }

        private void ConfigureBackButton()
        {
            if (quitButton == null)
                return;

            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnBackClicked);
            SetButtonLabel(quitButton, "BACK");
        }

        private void EnsureScrollSetup()
        {
            if (weaponsScrollRect == null)
                weaponsScrollRect = FindObjectOfType<ScrollRect>(true);

            if (weaponsScrollRect != null && weaponsScrollRect.content != null)
            {
                weaponListContent = weaponsScrollRect.content;
                EnsureContentLayout(weaponListContent);
                return;
            }

            RectTransform discoveredContent = ResolveExistingListContent();
            if (discoveredContent != null)
            {
                weaponListContent = discoveredContent;
                EnsureContentLayout(weaponListContent);
                return;
            }

            CreateRuntimeScrollView();
        }

        private RectTransform ResolveExistingListContent()
        {
            GameObject contentObject = GameObject.Find("WeaponsListContent");
            if (contentObject != null)
                return contentObject.GetComponent<RectTransform>();

            if (weaponsScrollRect != null && weaponsScrollRect.content != null)
                return weaponsScrollRect.content;

            return null;
        }

        private void CreateRuntimeScrollView()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[ARMORY] Canvas missing; cannot create runtime weapon list.");
                return;
            }

            GameObject scrollObj = new GameObject("WeaponsScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObj.transform.SetParent(canvas.transform, false);

            RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchoredPosition = new Vector2(0f, 120f);
            scrollRectTransform.sizeDelta = new Vector2(760f, 520f);

            Image scrollImage = scrollObj.GetComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0.25f);

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(scrollObj.transform, false);

            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportObj.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            Mask viewportMask = viewportObj.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentObj = new GameObject("WeaponsListContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(16f, 0f);
            contentRect.offsetMax = new Vector2(-16f, 0f);

            EnsureContentLayout(contentRect);

            ScrollRect createdScroll = scrollObj.GetComponent<ScrollRect>();
            createdScroll.horizontal = false;
            createdScroll.vertical = true;
            createdScroll.viewport = viewportRect;
            createdScroll.content = contentRect;
            createdScroll.movementType = ScrollRect.MovementType.Clamped;

            weaponsScrollRect = createdScroll;
            weaponListContent = contentRect;
        }

        private static void EnsureContentLayout(RectTransform content)
        {
            if (content == null)
                return;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 18f;
            layout.padding = new RectOffset(0, 0, 6, 6);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void BuildWeaponList()
        {
            activeWeaponItems.Clear();

            if (weaponListItems != null && weaponListItems.Length > 0)
            {
                foreach (WeaponListItem item in weaponListItems)
                {
                    if (item == null || item.button == null)
                        continue;

                    RegisterWeaponItem(item);
                }
            }

            if (activeWeaponItems.Count == 0)
            {
                BuildWeaponListFromTemplate();
                return;
            }

            EnsureAllSelectableWeaponsPresent();
        }

        private void BuildWeaponListFromTemplate()
        {
            if (weaponListContent == null)
            {
                Debug.LogWarning("[ARMORY] Weapon list content is missing; cannot build weapon cards.");
                return;
            }

            if (weaponRowTemplate == null)
                weaponRowTemplate = FindTemplateButton();

            if (weaponRowTemplate == null)
                weaponRowTemplate = CreateFallbackTemplateButton();

            if (weaponRowTemplate == null)
            {
                Debug.LogWarning("[ARMORY] No weapon row template available.");
                return;
            }

            if (weaponRowTemplate != null && weaponRowTemplate.transform.parent == weaponListContent)
                weaponRowTemplate.gameObject.SetActive(false);

            for (int i = 0; i < SelectableWeapons.Length; i++)
            {
                Constants.WeaponType type = SelectableWeapons[i];
                CreateAndRegisterWeaponItem(type, weaponRowTemplate);
            }
        }

        private void EnsureAllSelectableWeaponsPresent()
        {
            HashSet<Constants.WeaponType> existingTypes = new HashSet<Constants.WeaponType>();
            for (int i = 0; i < activeWeaponItems.Count; i++)
            {
                WeaponListItem item = activeWeaponItems[i];
                if (item != null)
                    existingTypes.Add(item.weaponType);
            }

            if (weaponRowTemplate == null)
                weaponRowTemplate = FindTemplateButton();
            if (weaponRowTemplate == null)
                weaponRowTemplate = CreateFallbackTemplateButton();
            if (weaponRowTemplate == null)
                return;

            if (weaponRowTemplate.transform.parent == weaponListContent)
                weaponRowTemplate.gameObject.SetActive(false);

            for (int i = 0; i < SelectableWeapons.Length; i++)
            {
                Constants.WeaponType type = SelectableWeapons[i];
                if (existingTypes.Contains(type))
                    continue;

                CreateAndRegisterWeaponItem(type, weaponRowTemplate);
            }
        }

        private void CreateAndRegisterWeaponItem(Constants.WeaponType type, Button template)
        {
            if (template == null || weaponListContent == null)
                return;

            Button button = Instantiate(template, weaponListContent);
            button.transform.SetParent(weaponListContent, false);
            button.gameObject.SetActive(true);
            button.gameObject.name = $"{type}Item";

            TextMeshProUGUI label = ResolveOrCreateLabel(button);
            Image iconImage = ResolveOrCreateWeaponImage(button.gameObject);

            RegisterWeaponItem(new WeaponListItem
            {
                weaponType = type,
                button = button,
                weaponNameText = label,
                weaponImage = iconImage
            });
        }

        private Button FindTemplateButton()
        {
            if (weaponListContent == null)
                return null;

            Button[] buttons = weaponListContent.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button != null && button != quitButton)
                    return button;
            }

            return null;
        }

        private Button CreateFallbackTemplateButton()
        {
            if (weaponListContent == null)
                return null;

            GameObject buttonObj = new GameObject("WeaponItemTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObj.transform.SetParent(weaponListContent, false);

            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 96f);

            LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 96f;

            Image panelImage = buttonObj.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            ResolveOrCreateWeaponImage(buttonObj);
            ResolveOrCreateLabel(buttonObj.GetComponent<Button>());
            ResolveOrCreateCheckmark(buttonObj);

            return buttonObj.GetComponent<Button>();
        }

        private void RegisterWeaponItem(WeaponListItem item)
        {
            if (item == null || item.button == null)
                return;

            if (weaponListContent != null && item.button.transform.parent != weaponListContent)
                item.button.transform.SetParent(weaponListContent, false);

            item.weaponNameText = item.weaponNameText != null ? item.weaponNameText : ResolveOrCreateLabel(item.button);
            item.weaponImage = item.weaponImage != null ? item.weaponImage : ResolveOrCreateWeaponImage(item.button.gameObject);
            item.selectedCheckmark = item.selectedCheckmark != null ? item.selectedCheckmark : ResolveOrCreateCheckmark(item.button.gameObject);

            SetButtonLabel(item.button, GetWeaponDisplayName(item.weaponType), item.weaponNameText);
            ApplyWeaponImage(item);

            item.button.onClick.RemoveAllListeners();
            Constants.WeaponType capturedType = item.weaponType;
            item.button.onClick.AddListener(() => OnWeaponSelected(capturedType));

            activeWeaponItems.Add(item);
        }

        private void OnWeaponSelected(Constants.WeaponType weaponType)
        {
            GameManager.Instance.SetSelectedWeapon(weaponType);
            RefreshSelectionUI();
        }

        private void RefreshSelectionUI()
        {
            Constants.WeaponType selectedType = GameManager.Instance.SelectedWeaponType;

            foreach (WeaponListItem item in activeWeaponItems)
            {
                if (item == null || item.button == null)
                    continue;

                bool isSelected = item.weaponType == selectedType;

                if (item.button.targetGraphic != null)
                    item.button.targetGraphic.color = isSelected ? selectedButtonColor : normalButtonColor;

                if (item.selectedCheckmark != null)
                    item.selectedCheckmark.SetActive(isSelected);
            }
        }

        private TextMeshProUGUI ResolveOrCreateLabel(Button button)
        {
            if (button == null)
                return null;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                return label;

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(button.transform, false);

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(110f, 0f);
            labelRect.offsetMax = new Vector2(-90f, 0f);

            TextMeshProUGUI createdLabel = labelObj.GetComponent<TextMeshProUGUI>();
            createdLabel.alignment = TextAlignmentOptions.MidlineLeft;
            createdLabel.fontSize = 42f;
            createdLabel.text = "WEAPON";
            return createdLabel;
        }

        private Image ResolveOrCreateWeaponImage(GameObject rowObject)
        {
            if (rowObject == null)
                return null;

            Transform existing = rowObject.transform.Find("WeaponImage");
            if (existing != null)
                return existing.GetComponent<Image>();

            GameObject imageObj = new GameObject("WeaponImage", typeof(RectTransform), typeof(Image));
            imageObj.transform.SetParent(rowObject.transform, false);

            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0f, 0.5f);
            imageRect.anchorMax = new Vector2(0f, 0.5f);
            imageRect.pivot = new Vector2(0f, 0.5f);
            imageRect.sizeDelta = new Vector2(72f, 72f);
            imageRect.anchoredPosition = new Vector2(18f, 0f);

            Image image = imageObj.GetComponent<Image>();
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, 0.35f);
            return image;
        }

        private static GameObject ResolveOrCreateCheckmark(GameObject rowObject)
        {
            if (rowObject == null)
                return null;

            Transform existing = rowObject.transform.Find("Checkmark");
            if (existing != null)
                return existing.gameObject;

            GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(TextMeshProUGUI));
            checkObj.transform.SetParent(rowObject.transform, false);

            RectTransform checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(1f, 0.5f);
            checkRect.anchorMax = new Vector2(1f, 0.5f);
            checkRect.pivot = new Vector2(1f, 0.5f);
            checkRect.sizeDelta = new Vector2(64f, 64f);
            checkRect.anchoredPosition = new Vector2(-16f, 0f);

            TextMeshProUGUI checkText = checkObj.GetComponent<TextMeshProUGUI>();
            checkText.text = "V";
            checkText.alignment = TextAlignmentOptions.Center;
            checkText.fontSize = 46f;
            checkText.color = new Color(0.15f, 1f, 0.25f, 1f);

            return checkObj;
        }

        private void ApplyWeaponImage(WeaponListItem item)
        {
            if (item == null || item.weaponImage == null)
                return;

            Sprite icon = GetWeaponIcon(item.weaponType);
            item.weaponImage.sprite = icon;
            item.weaponImage.enabled = icon != null;
        }

        private Sprite GetWeaponIcon(Constants.WeaponType weaponType)
        {
            if (weaponIcons == null)
                return GetWeaponIconFromPrefabs(weaponType);

            for (int i = 0; i < weaponIcons.Length; i++)
            {
                if (weaponIcons[i] != null && weaponIcons[i].weaponType == weaponType)
                    return weaponIcons[i].icon;
            }

            return GetWeaponIconFromPrefabs(weaponType);
        }

        private Sprite GetWeaponIconFromPrefabs(Constants.WeaponType weaponType)
        {
            Weapon weaponPrefab = GetWeaponPrefabByType(weaponType);
            if (weaponPrefab != null && weaponPrefab.weaponIcon != null)
                return weaponPrefab.weaponIcon;

            if (!missingIconWarnings.Contains(weaponType))
            {
                Debug.LogWarning($"[ARMORY] No icon found for {weaponType}. Assign via weaponIcons or weapon prefab icon.");
                missingIconWarnings.Add(weaponType);
            }

            return null;
        }

        private Weapon GetWeaponPrefabByType(Constants.WeaponType weaponType)
        {
            switch (weaponType)
            {
                case Constants.WeaponType.Rifle:
                    return rifleWeaponPrefab;
                case Constants.WeaponType.Cabirne:
                    return cabirneWeaponPrefab;
                case Constants.WeaponType.PiranhaGun:
                    return piranhaWeaponPrefab;
                case Constants.WeaponType.TeslaGun:
                    return teslaWeaponPrefab;
                case Constants.WeaponType.MrSulko:
                    return mrSulkoWeaponPrefab;
                case Constants.WeaponType.Beretta:
                    return berettaWeaponPrefab;
                default:
                    return null;
            }
        }

        private static void SetButtonLabel(Button button, string text, TextMeshProUGUI label = null)
        {
            if (button == null)
                return;

            if (label == null)
                label = button.GetComponentInChildren<TextMeshProUGUI>(true);

            if (label != null)
                label.text = text;
        }

        private static string GetWeaponDisplayName(Constants.WeaponType weaponType)
        {
            switch (weaponType)
            {
                case Constants.WeaponType.PiranhaGun:
                    return "PIRANHA";
                case Constants.WeaponType.TeslaGun:
                    return "TESLA";
                case Constants.WeaponType.MrSulko:
                    return "MR SULKO";
                case Constants.WeaponType.Cabirne:
                    return "CABIRNE";
                case Constants.WeaponType.Beretta:
                    return "BERETTA";
                case Constants.WeaponType.Rifle:
                default:
                    return "RIFLE";
            }
        }

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnToMenu();
        }
    }
}
