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
        public class WeaponIconEntry
        {
            public Constants.WeaponType weaponType;
            public Sprite icon;
        }

        [Header("Scene References")]
        public Button quitButton;
        public ScrollRect weaponsScrollRect;
        public RectTransform weaponListContent;
        public GameObject weaponCardPrefab;
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
        private readonly HashSet<Constants.WeaponType> unlockedWeapons = new HashSet<Constants.WeaponType>();
        private readonly HashSet<Constants.WeaponType> missingIconWarnings = new HashSet<Constants.WeaponType>();

        private UnlockWeaponModalUI unlockModal;
        private Constants.WeaponType pendingModalWeapon;
        private int CurrentCoins => GameManager.Instance.Coins;

        private void OnEnable()
        {
            GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;
        }

        private void OnDisable()
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
                gameManager.OnCoinsChanged -= HandleCoinsChanged;
        }

        private void Start()
        {
            ConfigureBackButton();
            EnsureScrollSetup();
            BuildUnlockModalUI();
            BuildCards();
            RefreshAllCardStates();
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

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[ARMORY] Canvas missing; cannot initialize weapon list.");
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

            Button unlockButton = CreateModalButton(panelObj.transform, "UnlockButton", "Unlock", new Vector2(0.48f, 0.08f), new Vector2(0.86f, 0.24f));
            Button closeButton = CreateModalButton(panelObj.transform, "CloseButton", "Close", new Vector2(0.1f, 0.08f), new Vector2(0.42f, 0.24f));

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

            cardsByWeapon.Clear();
            modelsByWeapon.Clear();
            unlockedWeapons.Clear();
            unlockedWeapons.UnionWith(ArmoryUIDataSource.BuildDefaultUnlockedSet());

            IReadOnlyList<Constants.WeaponType> ordered = ArmoryUIDataSource.GetOrderedWeapons();
            for (int i = 0; i < ordered.Count; i++)
            {
                Constants.WeaponType type = ordered[i];
                WeaponCardViewModel model = ArmoryUIDataSource.BuildCardModel(type, GetWeaponIcon(type));
                modelsByWeapon[type] = model;

                WeaponCardItemUI card = CreateCardUI(model);
                cardsByWeapon[type] = card;
            }
        }

        private WeaponCardItemUI CreateCardUI(WeaponCardViewModel model)
        {
            if (weaponCardPrefab == null)
            {
                Debug.LogError("[ARMORY] Weapon card prefab is not assigned.");
                return null;
            }

            GameObject rowObj = Instantiate(weaponCardPrefab, weaponListContent);
            rowObj.name = $"{model.weaponType}Card";

            WeaponCardItemUI itemUI = rowObj.GetComponent<WeaponCardItemUI>();
            if (itemUI == null)
            {
                Debug.LogError("[ARMORY] Weapon card prefab is missing WeaponCardItemUI.");
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

                bool isLocked = !unlockedWeapons.Contains(type);
                bool canAfford = !isLocked || CurrentCoins >= modelsByWeapon[type].cost;
                WeaponCardVisualState visualState = new WeaponCardVisualState
                {
                    isSelected = !isLocked && GameManager.Instance.SelectedWeaponType == type,
                    isLocked = isLocked,
                    canAfford = canAfford
                };

                card.Bind(modelsByWeapon[type], visualState);
            }
        }

        private void OnCardPressed(Constants.WeaponType weaponType)
        {
            bool isUnlocked = unlockedWeapons.Contains(weaponType);
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

            unlockedWeapons.Add(pendingModalWeapon);
            GameManager.Instance.SetSelectedWeapon(pendingModalWeapon);
            CloseUnlockModal();
            RefreshAllCardStates();
        }

        private void HandleCoinsChanged(int coins)
        {
            RefreshAllCardStates();
        }

        private void CloseUnlockModal()
        {
            if (unlockModal != null)
                unlockModal.Hide();
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
                Debug.LogWarning($"[ARMORY] No icon found for {weaponType}.");
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

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnFromArmory();
        }
    }
}
