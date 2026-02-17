using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class CampaignMapController : MonoBehaviour
    {
        [Header("Stage Data")]
        public StageConfig[] stages;

        [Header("Stage Node UI")]
        public CampaignStageNodeController[] stageNodes;

        [Header("Buttons")]
        public Button backButton;

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            RefreshStageNodes();
        }

        private void RefreshStageNodes()
        {
            for (int i = 0; i < stageNodes.Length; i++)
            {
                if (i >= stages.Length)
                    break;

                StageConfig config = stages[i];
                bool isUnlocked = CampaignProgressManager.Instance.IsStageUnlocked(config);
                int stars = CampaignProgressManager.Instance.GetStarsForStage(config.stageIndex);

                stageNodes[i].Initialize(config, isUnlocked, stars);
            }
        }

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnToMenu();
        }
    }
}
