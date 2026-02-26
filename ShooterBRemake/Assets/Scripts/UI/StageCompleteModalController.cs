using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class StageCompleteModalController : MonoBehaviour
    {
        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Text")]
        public TextMeshProUGUI stageNameText;

        [Header("Star Icons")]
        public Image[] starIcons;
        public Sprite filledStarSprite;
        public Sprite emptyStarSprite;

        [Header("Buttons")]
        public Button restartButton;
        public Button menuButton;

        private void Start()
        {
            EnsureModalRoot();

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);
        }

        public void Show(StageConfig config, long score)
        {
            EnsureModalRoot();

            int stars = CampaignProgressManager.Instance.CalculateStars(config, score);

            CampaignProgressManager.Instance.SaveStageStars(config.stageIndex, stars);

            if (stageNameText != null)
                stageNameText.text = config.mapName;

            ApplyStarIcons(stars);

            if (modalRoot != null)
                modalRoot.SetActive(true);
        }

        private void ApplyStarIcons(int earnedStars)
        {
            if (starIcons == null)
                return;

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] == null)
                    continue;

                bool earned = i < earnedStars;
                starIcons[i].sprite = earned ? filledStarSprite : emptyStarSprite;
                starIcons[i].enabled = earned ? filledStarSprite != null : emptyStarSprite != null;
            }
        }

        public void Hide()
        {
            EnsureModalRoot();

            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        public void OnRestartClicked()
        {
            SceneController.Instance.ReloadCurrentGameScene();
        }

        public void OnMenuClicked()
        {
            Time.timeScale = 1f;
            SceneController.Instance.LoadCampaignMapScene();
        }

        private void EnsureModalRoot()
        {
            if (modalRoot == null)
                modalRoot = gameObject;
        }
    }
}
