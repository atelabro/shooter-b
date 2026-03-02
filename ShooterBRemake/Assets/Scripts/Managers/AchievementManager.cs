using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public class AchievementManager : MonoBehaviour
    {
        public enum AchievementId
        {
            PiranhaDoubleKill50
        }

        private struct AchievementDefinition
        {
            public AchievementId id;
            public Constants.MultiKillType comboType;
            public Constants.WeaponType weaponType;
            public int targetCount;
            public string title;
            public string description;
        }

        private static AchievementManager instance;
        public static AchievementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("AchievementManager");
                    instance = go.AddComponent<AchievementManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        private readonly Dictionary<AchievementId, AchievementDefinition> definitions = new Dictionary<AchievementId, AchievementDefinition>();
        private readonly Dictionary<AchievementId, int> progress = new Dictionary<AchievementId, int>();
        private readonly HashSet<AchievementId> unlocked = new HashSet<AchievementId>();

        public event Action<AchievementId> OnAchievementUnlocked;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterDefinitions();
            LoadState();
            SubscribeToGameManager();
        }

        private void OnDestroy()
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
                gameManager.OnComboKillDetailed -= HandleComboKillDetailed;
        }

        public int GetProgress(AchievementId id)
        {
            return progress.TryGetValue(id, out int value) ? value : 0;
        }

        public int GetTarget(AchievementId id)
        {
            return definitions.TryGetValue(id, out AchievementDefinition definition) ? definition.targetCount : 0;
        }

        public bool IsUnlocked(AchievementId id)
        {
            return unlocked.Contains(id);
        }

        public bool GetIsUnlocked(AchievementId id)
        {
            return IsUnlocked(id);
        }

        public string GetTitle(AchievementId id)
        {
            return definitions.TryGetValue(id, out AchievementDefinition definition) ? definition.title : id.ToString();
        }

        public string GetDescription(AchievementId id)
        {
            return definitions.TryGetValue(id, out AchievementDefinition definition) ? definition.description : string.Empty;
        }

        public float GetNormalizedProgress(AchievementId id)
        {
            int target = GetTarget(id);
            if (target <= 0)
                return 0f;

            return Mathf.Clamp01((float)GetProgress(id) / target);
        }

        private void SubscribeToGameManager()
        {
            if (GameManager.Instance == null)
                return;

            GameManager.Instance.OnComboKillDetailed -= HandleComboKillDetailed;
            GameManager.Instance.OnComboKillDetailed += HandleComboKillDetailed;
        }

        private void RegisterDefinitions()
        {
            definitions.Clear();
            definitions[AchievementId.PiranhaDoubleKill50] = new AchievementDefinition
            {
                id = AchievementId.PiranhaDoubleKill50,
                comboType = Constants.MultiKillType.DoubleKill,
                weaponType = Constants.WeaponType.PiranhaGun,
                targetCount = 50,
                title = "Predator School",
                description = "Get 50 Double Kills using Piranha Gun."
            };
        }

        private void LoadState()
        {
            progress.Clear();
            unlocked.Clear();

            foreach (KeyValuePair<AchievementId, AchievementDefinition> entry in definitions)
            {
                AchievementId id = entry.Key;
                int value = PlayerPrefs.GetInt(GetProgressKey(id), 0);
                bool isUnlocked = PlayerPrefs.GetInt(GetUnlockedKey(id), 0) == 1;

                progress[id] = Mathf.Max(0, value);
                if (isUnlocked)
                    unlocked.Add(id);
            }
        }

        private void HandleComboKillDetailed(Constants.MultiKillType comboType, Constants.WeaponType weaponType, int bonusPoints, Vector3 position)
        {
            foreach (KeyValuePair<AchievementId, AchievementDefinition> entry in definitions)
            {
                AchievementDefinition definition = entry.Value;
                if (definition.comboType != comboType || definition.weaponType != weaponType)
                    continue;

                IncrementProgress(definition.id, 1);
            }
        }

        private void IncrementProgress(AchievementId id, int amount)
        {
            if (!definitions.TryGetValue(id, out AchievementDefinition definition))
                return;

            if (unlocked.Contains(id))
                return;

            int current = GetProgress(id);
            int next = Mathf.Min(definition.targetCount, current + Mathf.Max(0, amount));
            progress[id] = next;
            PlayerPrefs.SetInt(GetProgressKey(id), next);

            if (next >= definition.targetCount)
            {
                unlocked.Add(id);
                PlayerPrefs.SetInt(GetUnlockedKey(id), 1);
                OnAchievementUnlocked?.Invoke(id);
                Debug.Log($"[Achievement] Unlocked: {definition.title} ({id})");
            }

            PlayerPrefs.Save();
        }

        private static string GetProgressKey(AchievementId id)
        {
            return $"Achievement_{id}_Progress";
        }

        private static string GetUnlockedKey(AchievementId id)
        {
            return $"Achievement_{id}_Unlocked";
        }
    }
}
