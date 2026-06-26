using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class ArmoryController : MonoBehaviour
    {
        private const string ZeusPowerIconResourcePath = "SuperPowers/zeus-power";

        [Serializable]
        public class WeaponIconEntry
        {
            public Constants.WeaponType weaponType;
            public Sprite icon;
        }

        [Header("Scene References")]
        public Button quitButton;
        public TextMeshProUGUI sceneTitleText;
        public TextMeshProUGUI weaponsSectionTitleText;
        public TextMeshProUGUI superPowersSectionTitleText;
        public Button weaponsTabButton;
        public Button superPowersTabButton;
        public GameObject weaponsPanel;
        public GameObject superPowersPanel;
        public ScrollRect weaponsScrollRect;
        public RectTransform weaponListContent;
        public RectTransform superPowerListContent;
        public GameObject weaponCardPrefab;
        public GameObject superPowerCardPrefab;
        public WeaponIconEntry[] weaponIcons;

        [Header("Weapon Prefabs")]
        public Weapon rifleWeaponPrefab;
        public Weapon cabirneWeaponPrefab;
        public Weapon piranhaWeaponPrefab;
        public Weapon teslaWeaponPrefab;
        public Weapon mrSulkoWeaponPrefab;
        public Weapon berettaWeaponPrefab;
        public Weapon laserWeaponPrefab;

        private readonly Dictionary<Constants.WeaponType, WeaponCardItemUI> cardsByWeapon = new Dictionary<Constants.WeaponType, WeaponCardItemUI>();
        private readonly Dictionary<Constants.WeaponType, WeaponCardViewModel> modelsByWeapon = new Dictionary<Constants.WeaponType, WeaponCardViewModel>();
        private readonly HashSet<Constants.WeaponType> missingIconWarnings = new HashSet<Constants.WeaponType>();

        private SuperPowerCardItemUI zeusThunderCard;
        private SuperPowerViewModel zeusThunderModel;
        private UnlockWeaponModalUI unlockModal;
        private Constants.WeaponType pendingModalWeapon;
        private ArmoryTab activeTab = ArmoryTab.Weapons;
        private static readonly Color ActiveTabColor = new Color(0.92f, 0.62f, 0.18f, 0.95f);
        private static readonly Color InactiveTabColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        private int CurrentCoins => GameManager.Instance.Coins;

        private enum ArmoryTab
        {
            Weapons,
            SuperPowers
        }

        private void OnEnable()
        {
            GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;
            GameManager.Instance.OnZeusThunderCountChanged += HandleZeusThunderCountChanged;
        }

        private void OnDisable()
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnCoinsChanged -= HandleCoinsChanged;
                gameManager.OnZeusThunderCountChanged -= HandleZeusThunderCountChanged;
            }
        }

        private void Start()
        {
            _ = LocalizationManager.Instance;

            ResolveTextReferences();
            ConfigureBackButton();
            ConfigureTabs();
            EnsureScrollSetup();
            ResolveSuperPowerReferences();
            BuildUnlockModalUI();
            BuildCards();
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            RefreshLocalizedStaticTexts();
            RefreshAllCardStates();
            ShowTab(activeTab);
        }

        private void OnDestroy()
        {
            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void ConfigureBackButton()
        {
            if (quitButton == null)
                return;

            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnBackClicked);
            SetButtonLabel(quitButton, LocalizationManager.Instance.Get("common.back", "Back"));
        }

        private void ResolveTextReferences()
        {
            if (sceneTitleText == null && quitButton != null)
            {
                Transform container = quitButton.transform.parent;
                if (container != null)
                {
                    Transform titleTransform = container.Find("Title");
                    if (titleTransform != null)
                        sceneTitleText = titleTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (sceneTitleText == null)
                sceneTitleText = FindTextInSceneByName("Title");

            if (sceneTitleText == null)
                sceneTitleText = FindTextInSceneByName("SceneTitle");

            if (sceneTitleText == null)
                sceneTitleText = FindTextByCurrentValue("ARMORY");

            if (weaponsSectionTitleText == null)
                weaponsSectionTitleText = FindTextInSceneByName("WeaponsSectionTitle");

            if (superPowersSectionTitleText == null)
                superPowersSectionTitleText = FindTextInSceneByName("SuperPowersSectionTitle");

            if (weaponsTabButton == null)
                weaponsTabButton = FindButtonInSceneByName("WeaponsTabButton");

            if (superPowersTabButton == null)
                superPowersTabButton = FindButtonInSceneByName("SuperPowersTabButton");

            if (weaponsPanel == null)
                weaponsPanel = FindGameObjectInSceneByName("WeaponsPanel");

            if (superPowersPanel == null)
                superPowersPanel = FindGameObjectInSceneByName("SuperPowersPanel");
        }

        private void ConfigureTabs()
        {
            if (weaponsTabButton != null)
            {
                weaponsTabButton.onClick.RemoveAllListeners();
                weaponsTabButton.onClick.AddListener(() => ShowTab(ArmoryTab.Weapons));
            }

            if (superPowersTabButton != null)
            {
                superPowersTabButton.onClick.RemoveAllListeners();
                superPowersTabButton.onClick.AddListener(() => ShowTab(ArmoryTab.SuperPowers));
            }
        }

        private void EnsureScrollSetup()
        {
            if (weaponsScrollRect == null)
                weaponsScrollRect = FindObjectOfType<ScrollRect>(true);

            if (weaponsScrollRect != null && weaponsScrollRect.content != null)
            {
                if (weaponsPanel == null)
                    weaponsPanel = weaponsScrollRect.gameObject;

                weaponListContent = weaponsScrollRect.content;
                EnsureContentLayout(weaponListContent);
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameLog.Warning("[ARMORY] Canvas missing; cannot initialize weapon list.");
                return;
            }

            GameObject scrollObj = new GameObject("WeaponsScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObj.transform.SetParent(canvas.transform, false);

            RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0.08f, 0.14f);
            scrollRectTransform.anchorMax = new Vector2(0.92f, 0.88f);
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;

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
            viewportImage.color = new Color(1f, 1f, 1f, 0.03f);
            Mask viewportMask = viewportObj.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentObj = new GameObject("WeaponsListContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(20f, 0f);
            contentRect.offsetMax = new Vector2(-20f, 0f);

            EnsureContentLayout(contentRect);

            ScrollRect createdScroll = scrollObj.GetComponent<ScrollRect>();
            createdScroll.horizontal = false;
            createdScroll.vertical = true;
            createdScroll.viewport = viewportRect;
            createdScroll.content = contentRect;
            createdScroll.movementType = ScrollRect.MovementType.Clamped;

            weaponsScrollRect = createdScroll;
            weaponListContent = contentRect;
            if (weaponsPanel == null)
                weaponsPanel = scrollObj;
        }

        private void ResolveSuperPowerReferences()
        {
            if (superPowerListContent == null)
            {
                GameObject contentObject = GameObject.Find("SuperPowerListContent");
                if (contentObject != null)
                    superPowerListContent = contentObject.GetComponent<RectTransform>();
            }

            if (superPowerCardPrefab == null)
                GameLog.Warning("[ARMORY] superPowerCardPrefab is not assigned. Super power cards will not be shown.");

            if (superPowerListContent == null)
                GameLog.Warning("[ARMORY] superPowerListContent is not assigned. Super power cards will not be shown.");

            if (superPowersPanel == null && superPowerListContent != null)
                superPowersPanel = superPowerListContent.gameObject;
        }

        private static void EnsureContentLayout(RectTransform content)
        {
            if (content == null)
                return;

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 18f;
            layout.padding = new RectOffset(0, 0, 8, 8);
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

        private void BuildUnlockModalUI()
        {
            if (weaponListContent == null)
                return;

            Canvas canvas = weaponListContent.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            GameObject rootObj = new GameObject("UnlockWeaponModal", typeof(RectTransform), typeof(Image), typeof(UnlockWeaponModalUI));
            rootObj.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = rootObj.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image rootBg = rootObj.GetComponent<Image>();
            rootBg.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(rootObj.transform, false);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0.2f);
            panelRect.anchorMax = new Vector2(0.8f, 0.8f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = panelObj.GetComponent<Image>();
            panelBg.color = new Color(0.13f, 0.15f, 0.2f, 1f);

            TextMeshProUGUI title = CreateTmpText(panelObj.transform, "Title", new Vector2(0.08f, 0.79f), new Vector2(0.7f, 0.95f), 56f, TextAlignmentOptions.Left);
            TextMeshProUGUI desc = CreateTmpText(panelObj.transform, "Description", new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.77f), 33f, TextAlignmentOptions.TopLeft);
            TextMeshProUGUI cost = CreateTmpText(panelObj.transform, "Cost", new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.57f), 38f, TextAlignmentOptions.Left);
            TextMeshProUGUI status = CreateTmpText(panelObj.transform, "Status", new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.46f), 30f, TextAlignmentOptions.Left);

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(panelObj.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.74f, 0.78f);
            iconRect.anchorMax = new Vector2(0.92f, 0.95f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image icon = iconObj.GetComponent<Image>();
            icon.preserveAspect = true;

            Button unlockButton = CreateModalButton(
                panelObj.transform,
                "UnlockButton",
                LocalizationManager.Instance.Get("armory.modal.button.unlock", "Unlock"),
                new Vector2(0.48f, 0.08f),
                new Vector2(0.86f, 0.24f));
            Button closeButton = CreateModalButton(
                panelObj.transform,
                "CloseButton",
                LocalizationManager.Instance.Get("armory.modal.button.close", "Close"),
                new Vector2(0.1f, 0.08f),
                new Vector2(0.42f, 0.24f));

            unlockModal = rootObj.GetComponent<UnlockWeaponModalUI>();
            unlockModal.titleText = title;
            unlockModal.descriptionText = desc;
            unlockModal.costText = cost;
            unlockModal.statusText = status;
            unlockModal.iconImage = icon;
            unlockModal.unlockButton = unlockButton;
            unlockModal.closeButton = closeButton;
            unlockModal.Hide();
        }

        private void BuildCards()
        {
            if (weaponListContent == null)
                return;

            for (int i = weaponListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(weaponListContent.GetChild(i).gameObject);
            }

            if (superPowerListContent != null && superPowerListContent != weaponListContent)
            {
                for (int i = superPowerListContent.childCount - 1; i >= 0; i--)
                    Destroy(superPowerListContent.GetChild(i).gameObject);
            }

            cardsByWeapon.Clear();
            modelsByWeapon.Clear();
            zeusThunderCard = null;

            IReadOnlyList<Constants.WeaponType> ordered = ArmoryUIDataSource.GetOrderedWeapons();
            for (int i = 0; i < ordered.Count; i++)
            {
                Constants.WeaponType type = ordered[i];
                WeaponCardViewModel model = ArmoryUIDataSource.BuildCardModel(type, GetWeaponPrefabByType(type), GetWeaponIcon(type));
                modelsByWeapon[type] = model;

                WeaponCardItemUI card = CreateCardUI(model);
                cardsByWeapon[type] = card;
            }

            zeusThunderCard = CreateSuperPowerCardUI();
        }

        private WeaponCardItemUI CreateCardUI(WeaponCardViewModel model)
        {
            if (weaponCardPrefab == null)
            {
                GameLog.Error("[ARMORY] Weapon card prefab is not assigned.");
                return null;
            }

            GameObject rowObj = Instantiate(weaponCardPrefab, weaponListContent);
            rowObj.name = $"{model.weaponType}Card";

            WeaponCardItemUI itemUI = rowObj.GetComponent<WeaponCardItemUI>();
            if (itemUI == null)
            {
                GameLog.Error("[ARMORY] Weapon card prefab is missing WeaponCardItemUI.");
                Destroy(rowObj);
                return null;
            }

            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            if (rowRect != null)
                rowRect.sizeDelta = new Vector2(0f, 250f);

            LayoutElement layout = rowObj.GetComponent<LayoutElement>();
            if (layout != null)
                layout.preferredHeight = 250f;

            Button rowButton = rowObj.GetComponent<Button>();
            if (rowButton != null)
            {
                rowButton.onClick.RemoveAllListeners();
                rowButton.onClick.AddListener(() => OnCardPressed(model.weaponType));
            }

            if (itemUI.unlockButton != null)
            {
                itemUI.unlockButton.onClick.RemoveAllListeners();
                itemUI.unlockButton.onClick.AddListener(() => OnCardPressed(model.weaponType));
            }

            return itemUI;
        }

        private SuperPowerCardItemUI CreateSuperPowerCardUI()
        {
            if (superPowerListContent == null || superPowerCardPrefab == null)
                return null;

            GameObject rowObj = Instantiate(superPowerCardPrefab, superPowerListContent);
            rowObj.name = "ZeusThunderCard";
            SuperPowerCardItemUI itemUI = rowObj.GetComponent<SuperPowerCardItemUI>();
            if (itemUI == null)
            {
                GameLog.Error("[ARMORY] Super power card prefab is missing SuperPowerCardItemUI.");
                Destroy(rowObj);
                return null;
            }

            if (itemUI.buyButton != null)
            {
                itemUI.buyButton.onClick.RemoveAllListeners();
                itemUI.buyButton.onClick.AddListener(OnZeusThunderPressed);
            }

            Button rowButton = rowObj.GetComponent<Button>();
            if (rowButton != null)
            {
                rowButton.onClick.RemoveAllListeners();
                rowButton.onClick.AddListener(OnZeusThunderPressed);
            }

            return itemUI;
        }

        private static TextMeshProUGUI CreateTmpText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions align)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        private static Button CreateModalButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = btnObj.GetComponent<Image>();
            bg.color = new Color(0.22f, 0.45f, 0.75f, 1f);

            TextMeshProUGUI text = CreateTmpText(btnObj.transform, "Text", Vector2.zero, Vector2.one, 28f, TextAlignmentOptions.Center);
            text.text = label;

            return btnObj.GetComponent<Button>();
        }

        private void RefreshAllCardStates()
        {
            foreach (KeyValuePair<Constants.WeaponType, WeaponCardItemUI> pair in cardsByWeapon)
            {
                Constants.WeaponType type = pair.Key;
                WeaponCardItemUI card = pair.Value;
                if (card == null || !modelsByWeapon.ContainsKey(type))
                    continue;

                bool isLocked = !GameManager.Instance.IsWeaponUnlocked(type);
                bool canAfford = !isLocked || CurrentCoins >= modelsByWeapon[type].cost;
                WeaponCardVisualState visualState = new WeaponCardVisualState
                {
                    isSelected = !isLocked && GameManager.Instance.SelectedWeaponType == type,
                    isLocked = isLocked,
                    canAfford = canAfford
                };

                card.Bind(modelsByWeapon[type], visualState);
            }

            RefreshZeusThunderCard();
        }

        private void OnCardPressed(Constants.WeaponType weaponType)
        {
            bool isUnlocked = GameManager.Instance.IsWeaponUnlocked(weaponType);
            if (!isUnlocked)
            {
                OpenUnlockModal(weaponType);
                return;
            }

            GameManager.Instance.SetSelectedWeapon(weaponType);
            RefreshAllCardStates();
        }

        private void OpenUnlockModal(Constants.WeaponType weaponType)
        {
            if (unlockModal == null || !modelsByWeapon.ContainsKey(weaponType))
                return;

            pendingModalWeapon = weaponType;
            WeaponCardViewModel model = modelsByWeapon[weaponType];
            int currentCoins = CurrentCoins;
            UnlockWeaponModalUI.UnlockModalState state = currentCoins >= model.cost
                ? UnlockWeaponModalUI.UnlockModalState.CanUnlock
                : UnlockWeaponModalUI.UnlockModalState.InsufficientCoins;

            unlockModal.Configure(model, state, currentCoins, OnUnlockConfirmedUiOnly, CloseUnlockModal);
            unlockModal.Show();
        }

        private void OnUnlockConfirmedUiOnly()
        {
            if (!modelsByWeapon.ContainsKey(pendingModalWeapon))
                return;

            WeaponCardViewModel model = modelsByWeapon[pendingModalWeapon];
            if (!GameManager.Instance.TrySpendCoins(model.cost))
            {
                OpenUnlockModal(pendingModalWeapon);
                return;
            }

            GameManager.Instance.UnlockWeapon(pendingModalWeapon);
            GameManager.Instance.SetSelectedWeapon(pendingModalWeapon);
            CloseUnlockModal();
            RefreshAllCardStates();
        }

        private void HandleCoinsChanged(int coins)
        {
            RefreshAllCardStates();
        }

        private void HandleZeusThunderCountChanged(int count)
        {
            RefreshZeusThunderCard();
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedStaticTexts();
            RefreshLocalizedModels();
            RefreshAllCardStates();
            RefreshModalIfVisible();
        }

        private void RefreshLocalizedStaticTexts()
        {
            if (sceneTitleText == null)
                ResolveTextReferences();

            if (sceneTitleText != null)
                sceneTitleText.text = LocalizationManager.Instance.Get("armory.scene.title", "ARMORY");

            if (weaponsSectionTitleText != null)
                weaponsSectionTitleText.text = LocalizationManager.Instance.Get("armory.section.weapons", "Weapons");

            if (superPowersSectionTitleText != null)
                superPowersSectionTitleText.text = LocalizationManager.Instance.Get("armory.section.super_weapons", "Super Powers");

            SetButtonLabel(weaponsTabButton, LocalizationManager.Instance.Get("armory.tab.weapons", "Weapons"));
            SetButtonLabel(superPowersTabButton, LocalizationManager.Instance.Get("armory.tab.super_weapons", "Super Powers"));
            SetButtonLabel(quitButton, LocalizationManager.Instance.Get("common.back", "Back"));
        }

        private void RefreshLocalizedModels()
        {
            IReadOnlyList<Constants.WeaponType> ordered = ArmoryUIDataSource.GetOrderedWeapons();
            for (int i = 0; i < ordered.Count; i++)
            {
                Constants.WeaponType weaponType = ordered[i];
                modelsByWeapon[weaponType] = ArmoryUIDataSource.BuildCardModel(
                    weaponType,
                    GetWeaponPrefabByType(weaponType),
                    GetWeaponIcon(weaponType));
            }

            zeusThunderModel = BuildZeusThunderModel();
        }

        private void OnZeusThunderPressed()
        {
            if (!GameManager.Instance.BuyZeusThunderCharge())
                return;

            RefreshZeusThunderCard();
        }

        private void RefreshZeusThunderCard()
        {
            if (zeusThunderCard == null)
                return;

            zeusThunderModel = BuildZeusThunderModel();
            zeusThunderCard.Bind(zeusThunderModel, CurrentCoins >= Constants.ZEUS_THUNDER_COST);
        }

        private SuperPowerViewModel BuildZeusThunderModel()
        {
            return new SuperPowerViewModel
            {
                displayName = LocalizationManager.Instance.Get("superweapon.zeus.name", "Zeus Thunder"),
                description = LocalizationManager.Instance.Get(
                    "superweapon.zeus.description",
                    "Drag Zeus into battle to call thunder from above and strike every duck on screen."),
                cost = Constants.ZEUS_THUNDER_COST,
                ownedCount = GameManager.Instance.ZeusThunderCount,
                icon = Resources.Load<Sprite>(ZeusPowerIconResourcePath) ?? GetWeaponIcon(Constants.WeaponType.TeslaGun)
            };
        }

        private void RefreshModalIfVisible()
        {
            if (unlockModal == null || !unlockModal.gameObject.activeSelf)
                return;

            if (!modelsByWeapon.ContainsKey(pendingModalWeapon))
                return;

            WeaponCardViewModel model = modelsByWeapon[pendingModalWeapon];
            int currentCoins = CurrentCoins;
            UnlockWeaponModalUI.UnlockModalState state = currentCoins >= model.cost
                ? UnlockWeaponModalUI.UnlockModalState.CanUnlock
                : UnlockWeaponModalUI.UnlockModalState.InsufficientCoins;
            unlockModal.Configure(model, state, currentCoins, OnUnlockConfirmedUiOnly, CloseUnlockModal);
        }

        private void CloseUnlockModal()
        {
            if (unlockModal != null)
                unlockModal.Hide();
        }

        private void ShowTab(ArmoryTab tab)
        {
            activeTab = tab;

            if (weaponsPanel != null)
                weaponsPanel.SetActive(tab == ArmoryTab.Weapons);

            if (superPowersPanel != null)
                superPowersPanel.SetActive(tab == ArmoryTab.SuperPowers);

            if (weaponsTabButton != null)
            {
                weaponsTabButton.interactable = true;
                SetTabVisual(weaponsTabButton, tab == ArmoryTab.Weapons);
            }

            if (superPowersTabButton != null)
            {
                superPowersTabButton.interactable = true;
                SetTabVisual(superPowersTabButton, tab == ArmoryTab.SuperPowers);
            }
        }

        private static void SetTabVisual(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = active ? ActiveTabColor : InactiveTabColor;
        }

        private Sprite GetWeaponIcon(Constants.WeaponType weaponType)
        {
            if (weaponIcons != null)
            {
                for (int i = 0; i < weaponIcons.Length; i++)
                {
                    if (weaponIcons[i] != null && weaponIcons[i].weaponType == weaponType && weaponIcons[i].icon != null)
                        return weaponIcons[i].icon;
                }
            }

            Weapon prefab = GetWeaponPrefabByType(weaponType);
            if (prefab != null && prefab.weaponIcon != null)
                return prefab.weaponIcon;

            if (!missingIconWarnings.Contains(weaponType))
            {
                GameLog.Warning($"[ARMORY] No icon found for {weaponType}.");
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
                case Constants.WeaponType.LaserGun:
                    return laserWeaponPrefab;
                default:
                    return null;
            }
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = text;
        }

        private TextMeshProUGUI FindTextInSceneByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == name)
                    return texts[i];
            }

            return null;
        }

        private Button FindButtonInSceneByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            Button[] buttons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == name)
                    return buttons[i];
            }

            return null;
        }

        private GameObject FindGameObjectInSceneByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            Transform[] transforms = FindObjectsOfType<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].gameObject.name == name)
                    return transforms[i].gameObject;
            }

            return null;
        }

        private TextMeshProUGUI FindTextByCurrentValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && string.Equals(texts[i].text, value, StringComparison.OrdinalIgnoreCase))
                    return texts[i];
            }

            return null;
        }

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnFromArmory();
        }
    }
}
