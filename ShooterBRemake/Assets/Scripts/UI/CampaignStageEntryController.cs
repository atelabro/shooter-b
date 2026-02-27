using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ShooterB
{
    public class CampaignStageEntryController : MonoBehaviour
    {
        [Header("UI Elements")]
        public Image backgroundImage;
        public TextMeshProUGUI mapNameText;
        public GameObject[] starIcons;
        public Sprite starFilledSprite;
        public Sprite starEmptySprite;
        public GameObject lockOverlay;
        public GameObject lockIcon;
        public Button entryButton;

        private Action onClickCallback;

        public void Initialize(StageConfig stage, bool isUnlocked, int starsEarned, Action onClick)
        {
            onClickCallback = onClick;

            if (backgroundImage != null && stage.backgroundSprite != null)
                backgroundImage.sprite = stage.backgroundSprite;

            if (mapNameText != null)
                mapNameText.text = stage.mapName;

            if (lockOverlay != null)
                lockOverlay.SetActive(!isUnlocked);

            if (lockIcon != null)
                lockIcon.SetActive(!isUnlocked);

            if (entryButton != null)
            {
                entryButton.interactable = isUnlocked;
                entryButton.onClick.RemoveAllListeners();
                entryButton.onClick.AddListener(() => onClickCallback?.Invoke());
            }

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] == null)
                    continue;

                starIcons[i].SetActive(true);

                Image starImage = starIcons[i].GetComponent<Image>();
                if (starImage == null)
                    continue;

                bool isFilled = i < starsEarned;
                Sprite target = isFilled ? starFilledSprite : starEmptySprite;
                if (target != null)
                    starImage.sprite = target;
            }
        }
    }
}
