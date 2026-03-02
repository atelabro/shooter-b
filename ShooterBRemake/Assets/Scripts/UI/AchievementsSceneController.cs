using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class AchievementsSceneController : MonoBehaviour
    {
        [Header("UI References")]
        public Transform contentRoot;
        public AchievementListItemUI rowPrefab;
        public Button backButton;

        private readonly Dictionary<AchievementManager.AchievementId, AchievementListItemUI> rowsById =
            new Dictionary<AchievementManager.AchievementId, AchievementListItemUI>();

        private void Start()
        {
            ResolveReferences();

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
                backButton.onClick.AddListener(OnBackClicked);
            }

            AchievementManager.Instance.OnAchievementUnlocked += HandleAchievementUnlocked;
            AchievementManager.Instance.OnAchievementProgressChanged += HandleAchievementProgressChanged;
            BuildAchievementRows();
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
                AchievementManager.Instance.OnAchievementProgressChanged -= HandleAchievementProgressChanged;
            }
        }

        private void ResolveReferences()
        {
            if (rowPrefab == null)
                rowPrefab = GetComponentInChildren<AchievementListItemUI>(true);

            if (contentRoot == null && rowPrefab != null)
                contentRoot = rowPrefab.transform.parent;
        }

        private void BuildAchievementRows()
        {
            if (contentRoot == null || rowPrefab == null)
            {
                Debug.LogWarning("[AchievementsSceneController] Missing contentRoot or rowPrefab reference.");
                return;
            }

            rowsById.Clear();

            List<Transform> toDestroy = new List<Transform>();
            for (int i = 0; i < contentRoot.childCount; i++)
            {
                Transform child = contentRoot.GetChild(i);
                if (child == rowPrefab.transform)
                    continue;

                toDestroy.Add(child);
            }

            for (int i = 0; i < toDestroy.Count; i++)
                Destroy(toDestroy[i].gameObject);

            rowPrefab.gameObject.SetActive(false);

            AchievementManager manager = AchievementManager.Instance;
            List<AchievementManager.AchievementId> sortedIds =
                Enum.GetValues(typeof(AchievementManager.AchievementId))
                    .Cast<AchievementManager.AchievementId>()
                    .OrderBy(id => manager.GetIsUnlocked(id) ? 1 : 0)
                    .ThenBy(id => manager.GetTitle(id))
                    .ToList();

            foreach (AchievementManager.AchievementId id in sortedIds)
            {
                AchievementListItemUI row = Instantiate(rowPrefab, contentRoot);
                row.gameObject.name = $"Achievement_{id}";
                row.gameObject.SetActive(true);
                row.Bind(
                    id,
                    manager.GetTitle(id),
                    manager.GetDescription(id),
                    manager.GetProgress(id),
                    manager.GetTarget(id),
                    manager.GetIsUnlocked(id),
                    manager.GetNormalizedProgress(id));
                rowsById[id] = row;
            }
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
                    manager.GetNormalizedProgress(id));
            }
        }

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnToMenu();
        }
    }
}
