using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class AchievementsSceneController : MonoBehaviour
    {
        private enum AchievementsTab
        {
            Achievements,
            Daily,
            Trophies
        }

        [Header("UI References")]
        public Transform contentRoot;
        public Transform dailyListRoot;
        public Transform achievementsListRoot;
        public Transform generalTitleMarker;
        public AchievementListItemUI rowPrefab;
        public AchievementListItemUI dailyRowPrefab;
        public TextMeshProUGUI sceneTitleText;
        public TextMeshProUGUI generalTitleText;
        public TextMeshProUGUI dailyAwardsTitleText;
        public Sprite dailyAdBonusIcon;
        public Button dailyAdBonusButton;
        public TextMeshProUGUI dailyAdBonusButtonText;
        public Button backButton;
        public TextMeshProUGUI backButtonText;

        [Header("Tabs")]
        public RectTransform tabBarRoot;
        public Button achievementsTabButton;
        public Button dailyTabButton;
        public Button trophiesTabButton;
        public GameObject trophiesTabBadgeDot;
        public Color activeTabColor = new Color(0.92f, 0.62f, 0.18f, 0.95f);
        public Color inactiveTabColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        public Color tabTextColor = Color.white;

        private readonly Dictionary<AchievementManager.AchievementId, AchievementListItemUI> rowsById =
            new Dictionary<AchievementManager.AchievementId, AchievementListItemUI>();
        private readonly List<AchievementListItemUI> dailyRows = new List<AchievementListItemUI>();
        private readonly List<GameObject> generatedTrophyObjects = new List<GameObject>();
        private DailyAwardsManager dailyAwardsManager;
        private DuckTrophyManager duckTrophyManager;
        private Transform trophiesRoot;
        private AchievementsTab currentTab = AchievementsTab.Daily;
        private const string DailyGeneratedPrefix = "DailyAchievement_";
        private const string AchievementGeneratedPrefix = "Achievement_";
        private const string TrophyGeneratedPrefix = "Trophy_";
        private Coroutine dailyAdStatusCoroutine;
        private Coroutine trophyRewardPopupCoroutine;
        private bool suppressNextTrophyClaimRebuild;

        private void Start()
        {
            ResolveReferences();
            GameLog.Log("[AchievementsSceneController] Start");

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
                backButton.onClick.AddListener(OnBackClicked);
            }

            AchievementManager.Instance.OnAchievementUnlocked += HandleAchievementUnlocked;
            AchievementManager.Instance.OnAchievementProgressChanged += HandleAchievementProgressChanged;
            dailyAwardsManager = DailyAwardsManager.Instance;
            dailyAwardsManager.OnDailyObjectiveProgressChanged += HandleDailyObjectiveProgressChanged;
            dailyAwardsManager.OnDailyObjectiveCompleted += HandleDailyObjectiveCompleted;
            dailyAwardsManager.OnDailySetCompleted += HandleDailySetCompleted;
            dailyAwardsManager.OnDailyAdWatchBonusClaimed += HandleDailyAdWatchBonusClaimed;
            duckTrophyManager = DuckTrophyManager.Instance;
            duckTrophyManager.OnDuckDiscovered += HandleDuckTrophyChanged;
            duckTrophyManager.OnDuckKillCountChanged += HandleDuckTrophyKillCountChanged;
            duckTrophyManager.OnCityRewardClaimed += HandleDuckTrophyCityRewardClaimed;
            duckTrophyManager.OnClaimableBadgeChanged += HandleDuckTrophyBadgeChanged;
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            RefreshLocalizedStaticTexts();
            EnsureTabBar();
            BuildAchievementRows();
            ShowTab(AchievementsTab.Daily);
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
                AchievementManager.Instance.OnAchievementProgressChanged -= HandleAchievementProgressChanged;
            }

            if (dailyAwardsManager != null)
            {
                dailyAwardsManager.OnDailyObjectiveProgressChanged -= HandleDailyObjectiveProgressChanged;
                dailyAwardsManager.OnDailyObjectiveCompleted -= HandleDailyObjectiveCompleted;
                dailyAwardsManager.OnDailySetCompleted -= HandleDailySetCompleted;
                dailyAwardsManager.OnDailyAdWatchBonusClaimed -= HandleDailyAdWatchBonusClaimed;
            }

            if (duckTrophyManager != null)
            {
                duckTrophyManager.OnDuckDiscovered -= HandleDuckTrophyChanged;
                duckTrophyManager.OnDuckKillCountChanged -= HandleDuckTrophyKillCountChanged;
                duckTrophyManager.OnCityRewardClaimed -= HandleDuckTrophyCityRewardClaimed;
                duckTrophyManager.OnClaimableBadgeChanged -= HandleDuckTrophyBadgeChanged;
            }

            if (LocalizationManager.HasInstance)
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void ResolveReferences()
        {
            if (rowPrefab == null)
                rowPrefab = GetComponentInChildren<AchievementListItemUI>(true);

            if (contentRoot == null && rowPrefab != null)
                contentRoot = rowPrefab.transform.parent;

            if (achievementsListRoot == null)
                achievementsListRoot = contentRoot;

            if (dailyListRoot == null && contentRoot != null)
            {
                Transform foundDailySection = contentRoot.Find("DailyAwardSection");
                if (foundDailySection != null)
                    dailyListRoot = foundDailySection;
            }

            if (generalTitleMarker == null && achievementsListRoot != null)
            {
                Transform foundGeneral = achievementsListRoot.Find("GeneralTitle");
                if (foundGeneral != null)
                    generalTitleMarker = foundGeneral;
            }

            if (generalTitleText == null && generalTitleMarker != null)
                generalTitleText = generalTitleMarker.GetComponent<TextMeshProUGUI>();

            if (sceneTitleText == null)
            {
                GameObject titleObject = GameObject.Find("SceneTitle");
                if (titleObject != null)
                    sceneTitleText = titleObject.GetComponent<TextMeshProUGUI>();
            }

            if (dailyRowPrefab == null)
            {
                if (dailyListRoot != null)
                {
                    AchievementListItemUI[] dailyCandidates = dailyListRoot.GetComponentsInChildren<AchievementListItemUI>(true);
                    for (int i = 0; i < dailyCandidates.Length; i++)
                    {
                        AchievementListItemUI candidate = dailyCandidates[i];
                        if (candidate != null)
                        {
                            dailyRowPrefab = candidate;
                            GameLog.Log($"[AchievementsSceneController] dailyRowPrefab auto-found in DailyAwardSection: {candidate.gameObject.name}");
                            break;
                        }
                    }
                }

                if (dailyRowPrefab == null)
                {
                    AchievementListItemUI[] allRows = GetComponentsInChildren<AchievementListItemUI>(true);
                    for (int i = 0; i < allRows.Length; i++)
                    {
                        AchievementListItemUI candidate = allRows[i];
                        if (candidate == null || candidate == rowPrefab)
                            continue;

                        if (candidate.gameObject.name.IndexOf("AchievementListItem", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            dailyRowPrefab = candidate;
                            GameLog.Log($"[AchievementsSceneController] dailyRowPrefab fallback auto-found: {candidate.gameObject.name}");
                            break;
                        }
                    }
                }
            }

            if (dailyAwardsTitleText == null && dailyListRoot != null)
            {
                Transform titleTransform = dailyListRoot.Find("DailyAwardsHeader/Daily Awards Title");
                if (titleTransform != null)
                    dailyAwardsTitleText = titleTransform.GetComponent<TextMeshProUGUI>();
            }

            if (dailyAdBonusButton == null && dailyListRoot != null)
            {
                Transform buttonTransform = dailyListRoot.Find("DailyAwardsHeader/GetAdBonusButton");
                if (buttonTransform != null)
                    dailyAdBonusButton = buttonTransform.GetComponent<Button>();
            }

            if (dailyAdBonusButtonText == null && dailyAdBonusButton != null)
                dailyAdBonusButtonText = dailyAdBonusButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (backButtonText == null && backButton != null)
                backButtonText = backButton.GetComponentInChildren<TextMeshProUGUI>(true);

            EnsureDailyAdBonusButton();
            if (dailyAdBonusButton != null)
            {
                dailyAdBonusButton.onClick.RemoveListener(OnDailyAdBonusButtonClicked);
                dailyAdBonusButton.onClick.AddListener(OnDailyAdBonusButtonClicked);
            }

            GameLog.Log($"[AchievementsSceneController] contentRoot={(contentRoot != null ? contentRoot.name : "null")} achievementsListRoot={(achievementsListRoot != null ? achievementsListRoot.name : "null")} dailyListRoot={(dailyListRoot != null ? dailyListRoot.name : "null")} rowPrefab={(rowPrefab != null ? rowPrefab.name : "null")} dailyRowPrefab={(dailyRowPrefab != null ? dailyRowPrefab.name : "null")} generalTitleMarker={(generalTitleMarker != null ? generalTitleMarker.name : "null")}");
        }

        private void BuildAchievementRows()
        {
            if (achievementsListRoot == null || rowPrefab == null)
            {
                GameLog.Warning("[AchievementsSceneController] Missing achievementsListRoot or rowPrefab reference.");
                return;
            }

            rowsById.Clear();
            dailyRows.Clear();
            ClearGeneratedRows(dailyListRoot, DailyGeneratedPrefix);
            ClearGeneratedRows(achievementsListRoot, AchievementGeneratedPrefix);

            rowPrefab.gameObject.SetActive(false);
            if (dailyRowPrefab != null)
                dailyRowPrefab.gameObject.SetActive(false);

            BuildDailyRows();
            BuildTrophyRows();

            AchievementManager manager = AchievementManager.Instance;
            List<AchievementManager.AchievementId> sortedIds =
                Enum.GetValues(typeof(AchievementManager.AchievementId))
                    .Cast<AchievementManager.AchievementId>()
                    .OrderBy(id => manager.GetIsUnlocked(id) ? 1 : 0)
                    .ThenBy(id => manager.GetTitle(id))
                    .ToList();

            foreach (AchievementManager.AchievementId id in sortedIds)
            {
                if (manager.GetTarget(id) <= 0)
                    continue;

                AchievementListItemUI row = Instantiate(rowPrefab, achievementsListRoot);
                row.gameObject.name = $"Achievement_{id}";
                row.gameObject.SetActive(true);
                row.Bind(
                    id,
                    manager.GetTitle(id),
                    manager.GetDescription(id),
                    manager.GetProgress(id),
                    manager.GetTarget(id),
                    manager.GetIsUnlocked(id),
                    manager.GetNormalizedProgress(id),
                    manager.GetCoinReward(id));
                PlaceAchievementAfterGeneralTitle(row.transform, rowsById.Count);
                rowsById[id] = row;
            }

            RefreshDailyAwardsHeader();
            RefreshTrophyBadge();
            ApplyTabVisibility();
        }

        private void BuildDailyRows()
        {
            AchievementListItemUI template = dailyRowPrefab != null ? dailyRowPrefab : rowPrefab;
            Transform targetRoot = dailyListRoot != null ? dailyListRoot : achievementsListRoot;
            if (template == null)
            {
                GameLog.Warning("[AchievementsSceneController] Daily template is null. Skipping daily rows.");
                return;
            }
            if (targetRoot == null)
            {
                GameLog.Warning("[AchievementsSceneController] Daily target root is null. Skipping daily rows.");
                return;
            }

            IReadOnlyList<DailyAwardsManager.DailyObjectiveState> todayObjectives = DailyAwardsManager.Instance.GetTodayObjectives();
            for (int i = 0; i < todayObjectives.Count; i++)
            {
                DailyAwardsManager.DailyObjectiveState state = todayObjectives[i];
                AchievementListItemUI row = Instantiate(template, targetRoot);
                row.gameObject.name = $"DailyAchievement_{state.objectiveId}";
                row.gameObject.SetActive(true);
                row.BindCustom(
                    state.title,
                    state.description,
                    state.progress,
                    state.target,
                    state.isCompleted,
                    state.NormalizedProgress,
                    state.coinReward,
                    GetLockedStatusText());
                PlaceDailyAfterHeader(row.transform, i);
                dailyRows.Add(row);
            }

            GameLog.Log($"[AchievementsSceneController] Built daily rows: {dailyRows.Count}");
        }

        private void BuildTrophyRows()
        {
            EnsureTrophiesRoot();
            if (trophiesRoot == null || duckTrophyManager == null)
                return;

            for (int i = 0; i < generatedTrophyObjects.Count; i++)
            {
                if (generatedTrophyObjects[i] != null)
                    Destroy(generatedTrophyObjects[i]);
            }

            generatedTrophyObjects.Clear();

            for (int i = 0; i < DuckTrophyCatalog.CampaignCityOrder.Length; i++)
            {
                DuckTrophyCity city = DuckTrophyCatalog.CampaignCityOrder[i];
                DuckTrophyManager.CityTrophyState cityState = duckTrophyManager.GetCityState(city);
                if (cityState.totalCount <= 0)
                    continue;

                GameObject section = CreateTrophyCitySection(city, cityState);
                section.transform.SetParent(trophiesRoot, false);
                generatedTrophyObjects.Add(section);
            }

            trophiesRoot.gameObject.SetActive(currentTab == AchievementsTab.Trophies);
        }

        private GameObject CreateTrophyCitySection(DuckTrophyCity city, DuckTrophyManager.CityTrophyState cityState)
        {
            GameObject section = new GameObject($"Trophy_{city}_Section", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            VerticalLayoutGroup sectionLayout = section.GetComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 10f;
            sectionLayout.padding = new RectOffset(0, 0, 12, 18);
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = false;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = section.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            header.transform.SetParent(section.transform, false);
            header.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.16f, 0.95f);
            LayoutElement headerLayoutElement = header.AddComponent<LayoutElement>();
            headerLayoutElement.minHeight = 64f;
            headerLayoutElement.preferredHeight = 64f;

            HorizontalLayoutGroup headerLayout = header.GetComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(18, 12, 8, 8);
            headerLayout.spacing = 12f;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText(header.transform, "Title", 28f, FontStyles.Bold, TextAlignmentOptions.Left);
            title.text = $"{GetLocalizedTrophyCityName(city)} {cityState.discoveredCount}/{cityState.totalCount}";
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 28f;
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;

            Button claimButton = CreateTextButton(header.transform, "ClaimButton");
            TextMeshProUGUI claimText = claimButton.GetComponentInChildren<TextMeshProUGUI>(true);
            Image claimImage = claimButton.GetComponent<Image>();
            if (cityState.rewardClaimed)
            {
                claimText.text = LocalizationManager.Instance.Get("trophies.reward.claimed", "CLAIMED");
                claimButton.interactable = false;
                if (claimImage != null)
                    claimImage.color = inactiveTabColor;
            }
            else if (!cityState.canClaimReward)
            {
                claimText.text = $"{cityState.discoveredCount}/{cityState.totalCount}";
                claimButton.interactable = false;
                if (claimImage != null)
                    claimImage.color = inactiveTabColor;
            }
            else
            {
                string format = LocalizationManager.Instance.Get("trophies.reward.claim_format", "Claim {0}");
                claimText.text = string.Format(format, cityState.rewardCoins);
                claimButton.interactable = cityState.canClaimReward;
                if (claimImage != null)
                    claimImage.color = activeTabColor;
            }

            claimButton.onClick.AddListener(() => ClaimTrophyCityReward(city, claimButton));

            VerticalLayoutGroup list = CreateTrophyList(section.transform);
            IReadOnlyList<DuckTrophyManager.DuckTrophyState> states = duckTrophyManager.GetDuckStatesForCity(city);
            for (int i = 0; i < states.Count; i++)
                CreateTrophyRow(list.transform, states[i]);

            return section;
        }

        private VerticalLayoutGroup CreateTrophyList(Transform parent)
        {
            GameObject listObject = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listObject.transform.SetParent(parent, false);

            VerticalLayoutGroup list = listObject.GetComponent<VerticalLayoutGroup>();
            list.spacing = 10f;
            list.padding = new RectOffset(0, 0, 0, 0);
            list.childAlignment = TextAnchor.UpperCenter;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;

            ContentSizeFitter fitter = listObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return list;
        }

        private void CreateTrophyRow(Transform parent, DuckTrophyManager.DuckTrophyState state)
        {
            bool discovered = state.discovered;
            GameObject row = new GameObject($"Trophy_{state.entry.city}_{state.entry.duckType}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            row.GetComponent<Image>().color = discovered
                ? new Color(0.22f, 0.18f, 0.12f, 0.94f)
                : new Color(0.09f, 0.1f, 0.12f, 0.96f);

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 284f;
            rowLayout.preferredHeight = 300f;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateTrophyPreview(row.transform, state.entry.duckType, discovered);

            GameObject infoObject = new GameObject("Info", typeof(RectTransform), typeof(VerticalLayoutGroup));
            infoObject.transform.SetParent(row.transform, false);
            VerticalLayoutGroup infoLayout = infoObject.GetComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 8f;
            infoLayout.padding = new RectOffset(0, 0, 0, 0);
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = false;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            LayoutElement infoLayoutElement = infoObject.AddComponent<LayoutElement>();
            infoLayoutElement.flexibleWidth = 1f;

            TextMeshProUGUI nameText = CreateText(infoObject.transform, "Name", 28f, FontStyles.Bold, TextAlignmentOptions.Left);
            nameText.text = discovered ? GetLocalizedTrophyDuckName(state.entry.duckType) : "???";
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 18f;
            nameText.fontSizeMax = 28f;
            nameText.maxVisibleLines = 2;

            TextMeshProUGUI classText = CreateText(infoObject.transform, "Class", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
            classText.text = GetLocalizedTrophyClassName(state.entry.trophyClass);
            classText.color = GetClassColor(state.entry.trophyClass);

            TextMeshProUGUI killsText = CreateText(row.transform, "Kills", 22f, FontStyles.Bold, TextAlignmentOptions.Right);
            string killsLabel = LocalizationManager.Instance.Get("trophies.kills", "Kills");
            killsText.text = discovered ? $"{killsLabel}: {state.killCount}" : $"{killsLabel}: --";
            killsText.enableAutoSizing = true;
            killsText.fontSizeMin = 14f;
            killsText.fontSizeMax = 22f;
            LayoutElement killsLayout = killsText.gameObject.AddComponent<LayoutElement>();
            killsLayout.minWidth = 150f;
            killsLayout.preferredWidth = 180f;
        }

        private void HandleAchievementUnlocked(AchievementManager.AchievementId id)
        {
            RefreshAchievementRow(id);
        }

        private void HandleAchievementProgressChanged(AchievementManager.AchievementId id)
        {
            RefreshAchievementRow(id);
        }

        private void RefreshAchievementRow(AchievementManager.AchievementId id)
        {
            if (rowsById.TryGetValue(id, out AchievementListItemUI row) && row != null)
            {
                AchievementManager manager = AchievementManager.Instance;
                row.Bind(
                    id,
                    manager.GetTitle(id),
                    manager.GetDescription(id),
                    manager.GetProgress(id),
                    manager.GetTarget(id),
                    manager.GetIsUnlocked(id),
                    manager.GetNormalizedProgress(id),
                    manager.GetCoinReward(id));
            }
        }

        private void HandleDailyObjectiveProgressChanged(int slotIndex)
        {
            RefreshDailyRow(slotIndex);
            RefreshDailyAwardsHeader();
        }

        private void HandleDailyObjectiveCompleted(int slotIndex)
        {
            RefreshDailyRow(slotIndex);
            RefreshDailyAwardsHeader();
        }

        private void HandleDailySetCompleted()
        {
            IReadOnlyList<DailyAwardsManager.DailyObjectiveState> states = DailyAwardsManager.Instance.GetTodayObjectives();
            for (int i = 0; i < states.Count; i++)
                RefreshDailyRow(i);

            RefreshDailyAwardsHeader();
        }

        private void HandleDailyAdWatchBonusClaimed()
        {
            RefreshDailyAwardsHeader();
        }

        private void RefreshDailyRow(int slotIndex)
        {
            IReadOnlyList<DailyAwardsManager.DailyObjectiveState> states = DailyAwardsManager.Instance.GetTodayObjectives();
            if (slotIndex < 0 || slotIndex >= states.Count)
                return;

            if (slotIndex >= dailyRows.Count || dailyRows[slotIndex] == null)
                return;

            DailyAwardsManager.DailyObjectiveState state = states[slotIndex];
            dailyRows[slotIndex].BindCustom(
                state.title,
                state.description,
                state.progress,
                state.target,
                state.isCompleted,
                state.NormalizedProgress,
                state.coinReward,
                GetLockedStatusText());
        }

        private void ClearGeneratedRows(Transform root, string namePrefix)
        {
            if (root == null || string.IsNullOrEmpty(namePrefix))
                return;

            List<Transform> toDestroy = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;

                if (child.name.StartsWith(namePrefix, StringComparison.Ordinal))
                    toDestroy.Add(child);
            }

            for (int i = 0; i < toDestroy.Count; i++)
                Destroy(toDestroy[i].gameObject);
        }

        private void PlaceDailyAfterHeader(Transform rowTransform, int offset)
        {
            if (dailyListRoot == null || rowTransform == null)
                return;

            int insertIndex = Mathf.Clamp(offset + 1, 0, dailyListRoot.childCount - 1);
            rowTransform.SetSiblingIndex(insertIndex);
        }

        private void PlaceAchievementAfterGeneralTitle(Transform rowTransform, int offset)
        {
            if (achievementsListRoot == null || rowTransform == null)
                return;

            if (generalTitleMarker != null && generalTitleMarker.parent == achievementsListRoot)
            {
                int baseIndex = generalTitleMarker.GetSiblingIndex();
                int insertIndex = Mathf.Clamp(baseIndex + 1 + offset, 0, achievementsListRoot.childCount - 1);
                rowTransform.SetSiblingIndex(insertIndex);
            }
        }

        private void EnsureTabBar()
        {
            if (tabBarRoot != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
                return;

            GameObject tabBar = new GameObject("AchievementsTabBar", typeof(RectTransform), typeof(VerticalLayoutGroup));
            tabBar.transform.SetParent(canvas.transform, false);
            tabBarRoot = tabBar.GetComponent<RectTransform>();
            tabBarRoot.anchorMin = new Vector2(0.05f, 0.48f);
            tabBarRoot.anchorMax = new Vector2(0.2f, 0.76f);
            tabBarRoot.offsetMin = Vector2.zero;
            tabBarRoot.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = tabBar.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            dailyTabButton = CreateTabButton(tabBarRoot, AchievementsTab.Daily);
            achievementsTabButton = CreateTabButton(tabBarRoot, AchievementsTab.Achievements);
            trophiesTabButton = CreateTabButton(tabBarRoot, AchievementsTab.Trophies);
            trophiesTabBadgeDot = CreateBadgeDot(trophiesTabButton.transform);
            AdjustScrollRectForTabs();
            RefreshTabTexts();
            RefreshTrophyBadge();
        }

        private Button CreateTabButton(Transform parent, AchievementsTab tab)
        {
            Button button = CreateTextButton(parent, $"{tab}TabButton");
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            text.fontSize = 22f;
            text.fontSizeMax = 22f;
            text.fontSizeMin = 14f;
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = 48f;
                layout.preferredHeight = 60f;
                layout.minWidth = 132f;
            }
            button.onClick.AddListener(() => ShowTab(tab));
            return button;
        }

        private void ShowTab(AchievementsTab tab)
        {
            currentTab = tab;
            ApplyTabVisibility();
            RefreshTabVisuals();
        }

        private void ApplyTabVisibility()
        {
            bool showDaily = currentTab == AchievementsTab.Daily;
            bool showAchievements = currentTab == AchievementsTab.Achievements;
            bool showTrophies = currentTab == AchievementsTab.Trophies;

            if (dailyListRoot != null)
                dailyListRoot.gameObject.SetActive(showDaily);
            if (generalTitleMarker != null)
                generalTitleMarker.gameObject.SetActive(showAchievements);
            if (trophiesRoot != null)
                trophiesRoot.gameObject.SetActive(showTrophies);

            foreach (KeyValuePair<AchievementManager.AchievementId, AchievementListItemUI> row in rowsById)
            {
                if (row.Value != null)
                    row.Value.gameObject.SetActive(showAchievements);
            }
        }

        private void RefreshTabTexts()
        {
            SetButtonText(achievementsTabButton, LocalizationManager.Instance.Get("achievements.tab.achievements", "Achievements"));
            SetButtonText(dailyTabButton, LocalizationManager.Instance.Get("achievements.tab.daily", "Daily"));
            SetButtonText(trophiesTabButton, LocalizationManager.Instance.Get("achievements.tab.trophies", "Trophies"));
        }

        private void RefreshTabVisuals()
        {
            SetTabVisual(achievementsTabButton, currentTab == AchievementsTab.Achievements);
            SetTabVisual(dailyTabButton, currentTab == AchievementsTab.Daily);
            SetTabVisual(trophiesTabButton, currentTab == AchievementsTab.Trophies);
        }

        private void SetTabVisual(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = active ? activeTabColor : inactiveTabColor;
        }

        private void RefreshTrophyBadge()
        {
            if (trophiesTabBadgeDot != null)
                trophiesTabBadgeDot.SetActive(duckTrophyManager != null && duckTrophyManager.HasAnyClaimableReward());
        }

        private void EnsureTrophiesRoot()
        {
            if (trophiesRoot != null)
                return;

            Transform parent = achievementsListRoot != null ? achievementsListRoot : contentRoot;
            if (parent == null)
                return;

            GameObject root = new GameObject("TrophiesSection", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            root.transform.SetParent(parent, false);
            trophiesRoot = root.transform;

            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(0, 0, 8, 18);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            root.SetActive(false);
        }

        private void CreateTrophyPreview(Transform parent, Constants.DuckType duckType, bool discovered)
        {
            GameObject previewObject = new GameObject("Preview", typeof(RectTransform));
            previewObject.transform.SetParent(parent, false);
            RectTransform previewRect = previewObject.GetComponent<RectTransform>();
            previewRect.sizeDelta = new Vector2(320f, 260f);

            LayoutElement imageLayout = previewObject.AddComponent<LayoutElement>();
            imageLayout.minWidth = 280f;
            imageLayout.preferredWidth = 320f;
            imageLayout.minHeight = 230f;
            imageLayout.preferredHeight = 260f;

            if (!discovered)
            {
                CreateTrophyPreviewImage(previewObject.transform, "UnknownSilhouette", GetTrophyUnknownSilhouetteSprite(), Color.white);
                return;
            }

            if (TryCreatePartTrophyPreview(previewObject.transform, duckType))
                return;

            CreateTrophyPreviewImage(previewObject.transform, "Frame", ResolveTrophySprite(duckType), Color.white);
        }

        private bool TryCreatePartTrophyPreview(Transform parent, Constants.DuckType duckType)
        {
            DuckTrophyCatalog catalog = duckTrophyManager != null ? duckTrophyManager.Catalog : null;
            DuckPartLibrary partLibrary = catalog != null ? catalog.partLibrary : null;
            if (partLibrary == null || !partLibrary.TryGetConfig(duckType, out DuckPartConfig config))
                return false;

            DuckPartSkinConfig visualConfig = config.ToSkinConfig();
            Sprite torsoSprite = visualConfig.torsoSprite;
            Sprite leftWingSprite = visualConfig.leftWingSprite;
            Sprite rightWingSprite = visualConfig.rightWingSprite != null ? visualConfig.rightWingSprite : leftWingSprite;
            if (torsoSprite == null)
                return false;

            RectTransform parentRect = parent as RectTransform;
            Vector2 availableSize = parentRect != null && parentRect.sizeDelta.sqrMagnitude > 0f
                ? parentRect.sizeDelta
                : new Vector2(320f, 260f);

            Vector2 torsoPosition = Vector2.zero;
            Vector2 leftWingPosition = visualConfig.leftWingPivotOffset + visualConfig.leftWingOffset;
            Vector2 rightWingPosition = visualConfig.rightWingPivotOffset + visualConfig.rightWingOffset;

            Bounds previewBounds = CalculatePartPreviewBounds(torsoSprite, torsoPosition);
            if (leftWingSprite != null)
                previewBounds.Encapsulate(CalculatePartPreviewBounds(leftWingSprite, leftWingPosition));
            if (rightWingSprite != null)
                previewBounds.Encapsulate(CalculatePartPreviewBounds(rightWingSprite, rightWingPosition));

            if (previewBounds.size.x <= 0f || previewBounds.size.y <= 0f)
                return false;

            float scale = Mathf.Min(
                availableSize.x * 0.9f / previewBounds.size.x,
                availableSize.y * 0.9f / previewBounds.size.y);
            Vector2 center = previewBounds.center;

            if (rightWingSprite != null)
            {
                Image rightWing = CreateTrophyPartImage(parent, "RightWing", rightWingSprite, rightWingPosition, center, scale);
                if (visualConfig.rightWingSprite == null)
                    rightWing.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            if (torsoSprite != null)
                CreateTrophyPartImage(parent, "Torso", torsoSprite, torsoPosition, center, scale);

            if (leftWingSprite != null)
                CreateTrophyPartImage(parent, "LeftWing", leftWingSprite, leftWingPosition, center, scale);

            return true;
        }

        private static Bounds CalculatePartPreviewBounds(Sprite sprite, Vector2 position)
        {
            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
            Vector2 pivot = sprite.pivot / sprite.pixelsPerUnit;
            Vector2 center = position + (size * 0.5f) - pivot;
            Bounds bounds = new Bounds(center, size);
            return bounds;
        }

        private static Image CreateTrophyPartImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 center, float scale)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            rect.anchoredPosition = (position - center) * scale;
            rect.sizeDelta = (sprite.rect.size / sprite.pixelsPerUnit) * scale;
            return image;
        }

        private static Image CreateTrophyPreviewImage(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        private Sprite ResolveTrophySprite(Constants.DuckType duckType)
        {
            DuckTrophyCatalog catalog = duckTrophyManager != null ? duckTrophyManager.Catalog : null;
            DuckFrameLibrary frameLibrary = catalog != null ? catalog.frameLibrary : null;
            if (frameLibrary == null || frameLibrary.frameSets == null)
                return null;

            for (int i = 0; i < frameLibrary.frameSets.Length; i++)
            {
                DuckFrameSet frameSet = frameLibrary.frameSets[i];
                if (frameSet.duckType == duckType && frameSet.frames != null && frameSet.frames.Length > 0)
                    return frameSet.frames[0];
            }

            return null;
        }

        private static Sprite GetTrophyUnknownSilhouetteSprite()
        {
            Sprite resourceSprite = Resources.Load<Sprite>("DuckTrophyUnknownSilhouette");
            if (resourceSprite != null)
                return resourceSprite;

            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                name = "MissingDuckTrophyUnknownSilhouette"
            };

            texture.SetPixel(0, 0, new Color32(122, 128, 136, 255));
            texture.Apply(false, true);

            Sprite fallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                100f);
            fallbackSprite.name = "MissingDuckTrophyUnknownSilhouetteSprite";
            return fallbackSprite;
        }

        private void ClaimTrophyCityReward(DuckTrophyCity city, Button claimButton)
        {
            if (duckTrophyManager == null)
                return;

            suppressNextTrophyClaimRebuild = true;
            bool claimed = duckTrophyManager.TryClaimCityReward(city);
            suppressNextTrophyClaimRebuild = false;
            if (!claimed)
                return;

            ApplyTrophyClaimedButtonState(claimButton);
            RefreshTrophyBadge();
            ShowTrophyRewardPopup(city);
        }

        private void ApplyTrophyClaimedButtonState(Button claimButton)
        {
            if (claimButton == null)
                return;

            claimButton.interactable = false;

            TextMeshProUGUI claimText = claimButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (claimText != null)
                claimText.text = LocalizationManager.Instance.Get("trophies.reward.claimed", "CLAIMED");

            Image claimImage = claimButton.GetComponent<Image>();
            if (claimImage != null)
                claimImage.color = inactiveTabColor;
        }

        private void ShowTrophyRewardPopup(DuckTrophyCity city)
        {
            if (trophyRewardPopupCoroutine != null)
                StopCoroutine(trophyRewardPopupCoroutine);

            trophyRewardPopupCoroutine = StartCoroutine(ShowTrophyRewardPopupCoroutine(city));
        }

        private System.Collections.IEnumerator ShowTrophyRewardPopupCoroutine(DuckTrophyCity city)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                yield break;

            GameObject popup = new GameObject("TrophyRewardPopup", typeof(RectTransform), typeof(Image));
            popup.transform.SetParent(canvas.transform, false);
            RectTransform rect = popup.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.72f);
            rect.anchorMax = new Vector2(0.7f, 0.86f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            popup.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.12f, 0.94f);

            TextMeshProUGUI text = CreateText(popup.transform, "Text", 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);
            string header = LocalizationManager.Instance.Get("trophies.reward.popup_header", "COLLECTION COMPLETE");
            string coinsSuffix = LocalizationManager.Instance.Get("reward.coins_suffix", "COINS");
            text.text = $"{header}\n{GetLocalizedTrophyCityName(city)}\n+{DuckTrophyCatalog.CityCompletionRewardCoins} {coinsSuffix}";

            yield return new WaitForSecondsRealtime(2.2f);

            if (popup != null)
                Destroy(popup);
            trophyRewardPopupCoroutine = null;
        }

        private Button CreateTextButton(Transform parent, string name)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = inactiveTabColor;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            button.colors = colors;

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Text", 22f, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
            buttonLayout.minHeight = 48f;
            buttonLayout.preferredHeight = 52f;
            buttonLayout.minWidth = 140f;
            return button;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.color = tabTextColor;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private GameObject CreateBadgeDot(Transform parent)
        {
            GameObject dot = new GameObject("BadgeDot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(parent, false);
            RectTransform rect = dot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(18f, 18f);
            rect.anchoredPosition = new Vector2(-8f, -8f);
            dot.GetComponent<Image>().color = new Color32(220, 45, 45, 255);
            dot.SetActive(false);
            return dot;
        }

        private void SetButtonText(Button button, string text)
        {
            if (button == null)
                return;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = text;
        }

        private void AdjustScrollRectForTabs()
        {
            ScrollRect scrollRect = contentRoot != null ? contentRoot.GetComponentInParent<ScrollRect>() : null;
            if (scrollRect == null)
                return;

            RectTransform rect = scrollRect.GetComponent<RectTransform>();
            if (rect != null)
            {
                if (rect.anchorMin.x < 0.21f)
                    rect.anchorMin = new Vector2(0.21f, rect.anchorMin.y);

                if (rect.anchorMax.y > 0.78f)
                    rect.anchorMax = new Vector2(rect.anchorMax.x, 0.77f);
            }
        }

        private string GetLocalizedTrophyCityName(DuckTrophyCity city)
        {
            return LocalizationManager.Instance.Get(
                DuckTrophyCatalog.GetCityLocalizationKey(city),
                DuckTrophyCatalog.GetCityDisplayName(city));
        }

        private string GetLocalizedTrophyClassName(DuckTrophyClass trophyClass)
        {
            string key = $"trophies.class.{trophyClass.ToString().ToLowerInvariant()}";
            return LocalizationManager.Instance.Get(key, trophyClass.ToString());
        }

        private string GetLocalizedTrophyDuckName(Constants.DuckType duckType)
        {
            string key = GetTrophyDuckNameKey(duckType);
            string fallback = Constants.GetDuckDisplayName(duckType);
            return string.IsNullOrEmpty(key) ? fallback : LocalizationManager.Instance.Get(key, fallback);
        }

        private static string GetTrophyDuckNameKey(Constants.DuckType duckType)
        {
            switch (duckType)
            {
                case Constants.DuckType.MK_PHALARX: return "trophies.duck.mk_phalarx";
                case Constants.DuckType.MK_ARCHER: return "trophies.duck.mk_archer";
                case Constants.DuckType.MK_VOJVODA: return "trophies.duck.mk_vojvoda";
                case Constants.DuckType.MK_SAMUIL_GUARD: return "trophies.duck.mk_samuil_guard";
                case Constants.DuckType.MK_SAMUIL_ELITE: return "trophies.duck.mk_samuil_elite";
                case Constants.DuckType.MK_SAMUIL_BOSS_DUCK: return "trophies.duck.mk_samuil_boss";
                case Constants.DuckType.FRENCH_REVOLUTIONARY: return "trophies.duck.french_revolutionary";
                case Constants.DuckType.FRENCH_NAPOLEON: return "trophies.duck.french_napoleon";
                case Constants.DuckType.FRENCH_MUSKETEER: return "trophies.duck.french_musketeer";
                case Constants.DuckType.FRENCH_ARTIST: return "trophies.duck.french_artist";
                case Constants.DuckType.FRENCH_MUSKETEER_BOSS_DUCK: return "trophies.duck.french_musketeer_boss";
                case Constants.DuckType.BRITISH_REDCOAT: return "trophies.duck.british_redcoat";
                case Constants.DuckType.BRITISH_POLICE: return "trophies.duck.british_police";
                case Constants.DuckType.BRITISH_PUNK: return "trophies.duck.british_punk";
                case Constants.DuckType.BRITISH_SHERLOCK_BOSS_DUCK: return "trophies.duck.british_sherlock_boss";
                case Constants.DuckType.USA_POLICE: return "trophies.duck.usa_police";
                case Constants.DuckType.USA_WORKER: return "trophies.duck.usa_worker";
                case Constants.DuckType.USA_BUSINESS: return "trophies.duck.usa_business";
                case Constants.DuckType.USA_SWAT: return "trophies.duck.usa_swat";
                case Constants.DuckType.USA_BOSS_DUCK: return "trophies.duck.usa_boss";
                case Constants.DuckType.USA_HOLLYWOOD: return "trophies.duck.usa_hollywood";
                case Constants.DuckType.USA_LEO: return "trophies.duck.usa_leo";
                case Constants.DuckType.USA_TOM: return "trophies.duck.usa_tom";
                case Constants.DuckType.USA_MARINE: return "trophies.duck.usa_marine";
                case Constants.DuckType.USA_ADMIRAL: return "trophies.duck.usa_admiral";
                case Constants.DuckType.USA_ADMIRAL_BOSS_DUCK: return "trophies.duck.usa_admiral_boss";
                case Constants.DuckType.JAPANESE_SAMURAI: return "trophies.duck.japanese_samurai";
                case Constants.DuckType.JAPANESE_STRAW_DUCK: return "trophies.duck.japanese_straw";
                case Constants.DuckType.JAPANESE_KIMONO_DUCK: return "trophies.duck.japanese_kimono";
                case Constants.DuckType.JAPANESE_SAMURAI_BOSS_DUCK: return "trophies.duck.japanese_samurai_boss";
                case Constants.DuckType.KYOTO_KIMONO_DUCK: return "trophies.duck.kyoto_kimono";
                case Constants.DuckType.JAPANESE_MONK_DUCK: return "trophies.duck.japanese_monk";
                case Constants.DuckType.JAPANESE_TANUKI_DUCK: return "trophies.duck.japanese_tanuki";
                case Constants.DuckType.JAPANESE_YAKUZA_BOSS_DUCK: return "trophies.duck.japanese_yakuza_boss";
                case Constants.DuckType.EGYPT_MUMMY: return "trophies.duck.egypt_mummy";
                case Constants.DuckType.EGYPT_PHARAOH: return "trophies.duck.egypt_pharaoh";
                case Constants.DuckType.EGYPT_RAIDER: return "trophies.duck.egypt_raider";
                case Constants.DuckType.EGYPT_ANUBIS: return "trophies.duck.egypt_anubis";
                case Constants.DuckType.EGYPT_SCARAB: return "trophies.duck.egypt_scarab";
                case Constants.DuckType.EGYPT_SCARAB_BOSS_DUCK: return "trophies.duck.egypt_scarab_boss";
                case Constants.DuckType.BRAZIL_FOOTBALLER_DUCK: return "trophies.duck.brazil_footballer";
                case Constants.DuckType.BRAZIL_LIFEGUARD_DUCK: return "trophies.duck.brazil_lifeguard";
                case Constants.DuckType.BRAZIL_PEACH_ARMY_DUCK: return "trophies.duck.brazil_peach_army";
                case Constants.DuckType.BRAZIL_CARNIVAL_DUCK: return "trophies.duck.brazil_carnival";
                case Constants.DuckType.BRAZIL_LIFEGUARD_BOSS_DUCK: return "trophies.duck.brazil_lifeguard_boss";
                default: return null;
            }
        }

        private static Color GetClassColor(DuckTrophyClass trophyClass)
        {
            switch (trophyClass)
            {
                case DuckTrophyClass.Boss: return new Color(0.96f, 0.28f, 0.24f, 1f);
                case DuckTrophyClass.Elite: return new Color(0.95f, 0.72f, 0.28f, 1f);
                default: return new Color(0.75f, 0.86f, 0.94f, 1f);
            }
        }

        private void HandleDuckTrophyChanged(Constants.DuckType duckType)
        {
            BuildTrophyRows();
        }

        private void HandleDuckTrophyKillCountChanged(Constants.DuckType duckType, int killCount)
        {
            BuildTrophyRows();
        }

        private void HandleDuckTrophyCityRewardClaimed(DuckTrophyCity city)
        {
            if (suppressNextTrophyClaimRebuild)
            {
                RefreshTrophyBadge();
                return;
            }

            BuildTrophyRows();
            RefreshTrophyBadge();
        }

        private void HandleDuckTrophyBadgeChanged()
        {
            RefreshTrophyBadge();
        }

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnFromAchievements();
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedStaticTexts();
            BuildAchievementRows();
            BuildTrophyRows();
        }

        private void RefreshLocalizedStaticTexts()
        {
            if (sceneTitleText != null)
                sceneTitleText.text = LocalizationManager.Instance.Get("achievements.scene.title", "ACHIEVEMENTS");

            if (generalTitleText != null)
                generalTitleText.text = LocalizationManager.Instance.Get("achievements.scene.general", "GENERAL");

            if (backButtonText != null)
                backButtonText.text = LocalizationManager.Instance.Get("common.back", "Back");

            RefreshDailyAdBonusButtonState();
            RefreshTabTexts();
            RefreshTabVisuals();
        }

        private static string GetLockedStatusText()
        {
            return LocalizationManager.Instance.Get("achievements.status.locked", "LOCKED");
        }

        private void RefreshDailyAwardsHeader()
        {
            if (dailyAwardsTitleText == null || dailyAwardsManager == null)
                return;

            string claimedText = LocalizationManager.Instance.Get("achievements.scene.daily_header_claimed", "DAILY AWARDS (CLAIMED)");
            string baseText = LocalizationManager.Instance.Get("achievements.scene.daily_header_base", "DAILY AWARDS");

            if (dailyAwardsManager.IsDailySetBonusGranted())
            {
                dailyAwardsTitleText.text = claimedText;
            }
            else
            {
                int completedCount = dailyAwardsManager.GetCompletedTodayCount();
                int objectiveCount = dailyAwardsManager.GetTodayObjectives().Count;
                objectiveCount = Mathf.Max(1, objectiveCount);
                dailyAwardsTitleText.text = $"{baseText} ({completedCount}/{objectiveCount})";
            }

            RefreshDailyAdBonusButtonState();
        }

        private void OnDailyAdBonusButtonClicked()
        {
            if (dailyAwardsManager == null)
                return;

            bool started = dailyAwardsManager.TryClaimDailyAdWatchBonus("achievements_header", HandleDailyAdBonusRequestCompleted);
            if (!started)
                ShowDailyAdAttemptStatus(dailyAwardsManager.LastAdWatchBonusAttemptResult);
            else if (dailyAdBonusButton != null)
                dailyAdBonusButton.interactable = false;

            RefreshDailyAdBonusButtonState();
        }

        private void HandleDailyAdBonusRequestCompleted(RewardedAdResult result)
        {
            if (result != RewardedAdResult.Completed)
                ShowDailyAdAttemptStatus(result);

            RefreshDailyAdBonusButtonState();
        }

        private void EnsureDailyAdBonusButton()
        {
            if (dailyAdBonusButton != null)
                return;

            Transform headerTransform = dailyListRoot != null ? dailyListRoot.Find("DailyAwardsHeader") : null;
            if (headerTransform == null)
                return;

            GameObject buttonObject = new GameObject("GetAdBonusButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(headerTransform, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.sizeDelta = new Vector2(320f, 46f);
            buttonRect.anchoredPosition = new Vector2(-12f, -12f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.22f, 0.58f, 0.19f, 0.95f);

            dailyAdBonusButton = buttonObject.GetComponent<Button>();
            ColorBlock colors = dailyAdBonusButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.52f, 0.52f, 0.52f, 0.9f);
            dailyAdBonusButton.colors = colors;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            if (dailyAdBonusIcon != null)
            {
                GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(buttonObject.transform, false);

                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(42f, 42f);
                iconRect.anchoredPosition = new Vector2(30f, 0f);

                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = dailyAdBonusIcon;
                iconImage.preserveAspect = true;
                iconImage.color = new Color(1f, 1f, 1f, 1f);
            }

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(dailyAdBonusIcon != null ? 46f : 10f, 4f);
            textRect.offsetMax = new Vector2(-10f, -4f);

            dailyAdBonusButtonText = textObject.GetComponent<TextMeshProUGUI>();
            dailyAdBonusButtonText.alignment = TextAlignmentOptions.Center;
            dailyAdBonusButtonText.fontSize = 24f;
            dailyAdBonusButtonText.fontStyle = FontStyles.Bold;
            dailyAdBonusButtonText.enableWordWrapping = false;
            dailyAdBonusButtonText.color = Color.white;
        }

        private void RefreshDailyAdBonusButtonState()
        {
            if (dailyAdBonusButton == null || dailyAwardsManager == null)
                return;

            bool canClaim = dailyAwardsManager.CanClaimDailyAdWatchBonus();
            bool alreadyClaimed = dailyAwardsManager.IsDailyAdWatchBonusGranted();
            dailyAdBonusButton.gameObject.SetActive(canClaim || alreadyClaimed);

            if (dailyAdBonusButtonText != null)
            {
                if (alreadyClaimed)
                {
                    dailyAdBonusButtonText.text = LocalizationManager.Instance.Get("achievements.daily_ad_bonus.claimed", "CLAIMED");
                }
                else
                {
                    string format = LocalizationManager.Instance.Get("achievements.daily_ad_bonus.get_format", "Get +{0} coins");
                    dailyAdBonusButtonText.text = string.Format(format, dailyAwardsManager.GetDailyAdWatchBonusCoins());
                }
            }

            dailyAdBonusButton.interactable = canClaim;
        }

        private void ShowDailyAdAttemptStatus(RewardedAdResult result)
        {
            if (dailyAdBonusButtonText == null)
                return;

            string message = RewardedAdService.Instance.GetResultMessage(result);
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (dailyAdStatusCoroutine != null)
                StopCoroutine(dailyAdStatusCoroutine);

            dailyAdStatusCoroutine = StartCoroutine(ShowDailyAdAttemptStatusCoroutine(message));
        }

        private System.Collections.IEnumerator ShowDailyAdAttemptStatusCoroutine(string message)
        {
            if (dailyAdBonusButtonText != null)
                dailyAdBonusButtonText.text = message;

            yield return new WaitForSecondsRealtime(1.75f);
            RefreshDailyAdBonusButtonState();
            dailyAdStatusCoroutine = null;
        }
    }
}
