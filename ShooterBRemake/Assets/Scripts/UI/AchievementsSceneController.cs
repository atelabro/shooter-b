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
        public TextMeshProUGUI dailyAwardsTitleText;
        public Button backButton;

        private readonly Dictionary<AchievementManager.AchievementId, AchievementListItemUI> rowsById =
            new Dictionary<AchievementManager.AchievementId, AchievementListItemUI>();
        private readonly List<AchievementListItemUI> dailyRows = new List<AchievementListItemUI>();
        private DailyAwardsManager dailyAwardsManager;
        private const string DailyGeneratedPrefix = "DailyAchievement_";
        private const string AchievementGeneratedPrefix = "Achievement_";
        private const string DailyAwardsHeaderBase = "DAILY AWARDS";
        private const string DailyAwardsHeaderClaimed = "DAILY AWARDS (CLAIMED)";

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
            }
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
                    "LOCKED");
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
                "LOCKED");
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

        private void RefreshDailyAwardsHeader()
        {
            if (dailyAwardsTitleText == null || dailyAwardsManager == null)
                return;

            if (dailyAwardsManager.IsDailySetBonusGranted())
            {
                dailyAwardsTitleText.text = DailyAwardsHeaderClaimed;
                return;
            }

            int completedCount = dailyAwardsManager.GetCompletedTodayCount();
            int objectiveCount = dailyAwardsManager.GetTodayObjectives().Count;
            objectiveCount = Mathf.Max(1, objectiveCount);
            dailyAwardsTitleText.text = $"{DailyAwardsHeaderBase} ({completedCount}/{objectiveCount})";
        }
    }
}
