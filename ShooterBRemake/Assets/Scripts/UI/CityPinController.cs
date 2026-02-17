using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ShooterB
{
    public class CityPinController : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI cityNameText;
        public GameObject lockIcon;
        public Button pinButton;

        private Action onClickCallback;

        public void Initialize(CityConfig config, bool isUnlocked, Action onClick)
        {
            onClickCallback = onClick;

            if (cityNameText != null)
                cityNameText.text = config.cityName;

            if (lockIcon != null)
                lockIcon.SetActive(!isUnlocked);

            if (pinButton != null)
            {
                pinButton.interactable = isUnlocked;
                pinButton.onClick.RemoveAllListeners();
                pinButton.onClick.AddListener(() => onClickCallback?.Invoke());
            }

            GetComponent<RectTransform>().anchoredPosition = config.pinPosition;
        }
    }
}
