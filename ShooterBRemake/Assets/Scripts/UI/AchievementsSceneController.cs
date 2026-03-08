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
        public Button dailyAdBonusButton;
        public TextMeshProUGUI dailyAdBonusButtonText;
        public Button backButton;
        public TextMeshProUGUI backButtonText;

        private readonly Dictionary<AchievementManager.AchievementId, AchievementListItemUI> rowsById =
            new Dictionary<AchievementManager.AchievementId, AchievementListItemUI>();
        private readonly List<AchievementListItemUI> dailyRows = new List<AchievementListItemUI>();
        private DailyAwardsManager dailyAwardsManager;
        private const string DailyGeneratedPrefix = "DailyAchievement_";
        private const string AchievementGeneratedPrefix = "Achievement_";
        private Coroutine dailyAdStatusCoroutine;

        private void Start()
        {
            ResolveReferences();
            Debug.Log("[AchievementsSceneController] Start");

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
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            RefreshLocalizedStaticTexts();
            BuildAchievementRows();
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
                            Debug.Log($"[AchievementsSceneController] dailyRowPrefab auto-found in DailyAwardSection: {candidate.gameObject.name}");
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
                            Debug.Log($"[AchievementsSceneController] dailyRowPrefab fallback auto-found: {candidate.gameObject.name}");
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

            Debug.Log($"[AchievementsSceneController] contentRoot={(contentRoot != null ? contentRoot.name : "null")} achievementsListRoot={(achievementsListRoot != null ? achievementsListRoot.name : "null")} dailyListRoot={(dailyListRoot != null ? dailyListRoot.name : "null")} rowPrefab={(rowPrefab != null ? rowPrefab.name : "null")} dailyRowPrefab={(dailyRowPrefab != null ? dailyRowPrefab.name : "null")} generalTitleMarker={(generalTitleMarker != null ? generalTitleMarker.name : "null")}");
        }

        private void BuildAchievementRows()
        {
            if (achievementsListRoot == null || rowPrefab == null)
            {
                Debug.LogWarning("[AchievementsSceneController] Missing achievementsListRoot or rowPrefab reference.");
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
        }

        private void BuildDailyRows()
        {
            AchievementListItemUI template = dailyRowPrefab != null ? dailyRowPrefab : rowPrefab;
            Transform targetRoot = dailyListRoot != null ? dailyListRoot : achievementsListRoot;
            if (template == null)
            {
                Debug.LogWarning("[AchievementsSceneController] Daily template is null. Skipping daily rows.");
                return;
            }
            if (targetRoot == null)
            {
                Debug.LogWarning("[AchievementsSceneController] Daily target root is null. Skipping daily rows.");
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

            Debug.Log($"[AchievementsSceneController] Built daily rows: {dailyRows.Count}");
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

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnToMenu();
        }

        private void HandleLanguageChanged(LocalizationManager.Language language)
        {
            RefreshLocalizedStaticTexts();
            BuildAchievementRows();
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

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 4f);
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
