using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShooterB
{
    public class CampaignStageNodeController : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI cityNameText;
        public TextMeshProUGUI briefingPreviewText;
        public GameObject[] starIcons;
        public GameObject lockOverlay;
        public Button stageButton;

        private StageConfig config;

        public void Initialize(StageConfig stageConfig, bool isUnlocked, int starsEarned)
        {
            config = stageConfig;

            if (cityNameText != null)
                cityNameText.text = stageConfig.cityName;

            if (briefingPreviewText != null)
                briefingPreviewText.text = isUnlocked ? stageConfig.briefingText : "???";

            if (lockOverlay != null)
                lockOverlay.SetActive(!isUnlocked);

            if (stageButton != null)
            {
                stageButton.interactable = isUnlocked;
                stageButton.onClick.RemoveAllListeners();
                stageButton.onClick.AddListener(OnStageClicked);
            }

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                    starIcons[i].SetActive(i < starsEarned);
            }
        }

        private void OnStageClicked()
        {
            CampaignProgressManager.Instance.SetActiveStage(config);
            SceneController.Instance.LoadCampaignStage(config);
        }
    }
}
