using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ShooterB
{
    public class CampaignStageEntryController : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI mapNameText;
        public GameObject[] starIcons;
        public GameObject lockOverlay;
        public Button entryButton;

        private Action onClickCallback;

        public void Initialize(StageConfig stage, CityConfig city, bool isUnlocked, int starsEarned, Action onClick)
        {
            onClickCallback = onClick;

            if (mapNameText != null)
                mapNameText.text = stage.mapName;

            if (lockOverlay != null)
                lockOverlay.SetActive(!isUnlocked);

            if (entryButton != null)
            {
                entryButton.interactable = isUnlocked;
                entryButton.onClick.RemoveAllListeners();
                entryButton.onClick.AddListener(() => onClickCallback?.Invoke());
            }

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                    starIcons[i].SetActive(i < starsEarned);
            }
        }
    }
}
