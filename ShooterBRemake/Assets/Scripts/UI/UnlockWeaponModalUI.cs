using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class UnlockWeaponModalUI : MonoBehaviour
    {
        public enum UnlockModalState
        {
            CanUnlock,
            InsufficientCoins,
            AlreadyUnlocked
        }

        [Header("UI References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI statusText;
        public Image iconImage;
        public Button unlockButton;
        public Button closeButton;

        private Action onUnlock;
        private Action onClose;

        public void Configure(WeaponCardViewModel model, UnlockModalState state, int currentCoins, Action unlockCallback, Action closeCallback)
        {
            onUnlock = unlockCallback;
            onClose = closeCallback;

            string coinsSuffix = LocalizationManager.Instance.Get("common.coins_suffix", "COINS");

            if (titleText != null)
                titleText.text = model != null
                    ? model.displayName
                    : LocalizationManager.Instance.Get("armory.modal.title_fallback", "Weapon");
            if (descriptionText != null)
                descriptionText.text = model != null ? model.description : string.Empty;
            if (costText != null && model != null)
            {
                string costFormat = LocalizationManager.Instance.Get("armory.modal.cost_format", "Cost: {0} {1}");
                costText.text = string.Format(costFormat, model.cost, coinsSuffix);
            }
            if (iconImage != null && model != null)
            {
                iconImage.sprite = model.icon;
                iconImage.enabled = model.icon != null;
            }

            if (statusText != null)
            {
                switch (state)
                {
                    case UnlockModalState.CanUnlock:
                        statusText.text = string.Format(
                            LocalizationManager.Instance.Get("armory.modal.status.have_format", "You have {0} {1}."),
                            currentCoins,
                            coinsSuffix);
                        statusText.color = new Color(0.75f, 1f, 0.75f, 1f);
                        break;
                    case UnlockModalState.InsufficientCoins:
                        int deficit = Mathf.Max(0, (model != null ? model.cost : 0) - currentCoins);
                        statusText.text = string.Format(
                            LocalizationManager.Instance.Get("armory.modal.status.missing_format", "Missing {0} {1}."),
                            deficit,
                            coinsSuffix);
                        statusText.color = new Color(1f, 0.5f, 0.5f, 1f);
                        break;
                    default:
                        statusText.text = LocalizationManager.Instance.Get(
                            "armory.modal.status.already_unlocked",
                            "This weapon is already unlocked.");
                        statusText.color = new Color(0.7f, 0.9f, 1f, 1f);
                        break;
                }
            }

            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveAllListeners();
                bool canUnlock = state == UnlockModalState.CanUnlock;
                unlockButton.gameObject.SetActive(canUnlock);
                unlockButton.interactable = canUnlock;
                if (canUnlock)
                    unlockButton.onClick.AddListener(OnUnlockPressed);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnClosePressed);
            }

            SetButtonLabel(unlockButton, LocalizationManager.Instance.Get("armory.modal.button.unlock", "Unlock"));
            SetButtonLabel(closeButton, LocalizationManager.Instance.Get("armory.modal.button.close", "Close"));
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnUnlockPressed()
        {
            onUnlock?.Invoke();
        }

        private void OnClosePressed()
        {
            onClose?.Invoke();
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
                return;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = text;
        }
    }
}
