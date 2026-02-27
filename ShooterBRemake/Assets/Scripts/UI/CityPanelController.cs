using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ShooterB
{
    public class CityPanelController : MonoBehaviour
    {
        public event Action OnPanelHidden;

        [Header("UI Elements")]
        public TextMeshProUGUI cityNameText;
        public TextMeshProUGUI briefingText;
        public Button closeButton;

        [Header("Stage Cards")]
        public Transform stageListContainer;
        public CampaignStageEntryController stageEntryPrefab;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            gameObject.SetActive(false);
        }

        public void Initialize(CityConfig[] _cities)
        {
        }

        public void Show(CityConfig city)
        {
            if (city == null)
                return;

            ClearStageList();
            PopulateStageList(city);

            if (cityNameText != null)
                cityNameText.text = city.cityName;

            if (briefingText != null)
                briefingText.text = city.briefingText;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            OnPanelHidden?.Invoke();
        }

        private void PopulateStageList(CityConfig city)
        {
            if (stageListContainer == null || stageEntryPrefab == null)
                return;

            foreach (StageConfig stage in city.stages)
            {
                bool isUnlocked = CampaignProgressManager.Instance.IsStageUnlocked(stage, city);
                int stars = CampaignProgressManager.Instance.GetStarsForStage(stage.stageIndex);

                CampaignStageEntryController entry = Instantiate(stageEntryPrefab, stageListContainer);
                StageConfig capturedStage = stage;
                entry.Initialize(stage, isUnlocked, stars, () => OnStageSelected(capturedStage));
            }
        }

        private void OnStageSelected(StageConfig stage)
        {
            CampaignProgressManager.Instance.SetActiveStage(stage);
            SceneController.Instance.LoadCampaignStage(stage);
        }

        private void ClearStageList()
        {
            if (stageListContainer == null)
                return;

            foreach (Transform child in stageListContainer)
                Destroy(child.gameObject);
        }
    }
}
