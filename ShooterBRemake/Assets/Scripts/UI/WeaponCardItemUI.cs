using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class WeaponCardItemUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI fireTypeText;
        public TextMeshProUGUI fireRateText;
        public TextMeshProUGUI reloadText;
        public TextMeshProUGUI travelSpeedText;
        public TextMeshProUGUI chainLightningText;
        public TextMeshProUGUI aoeText;
        public Image iconImage;
        public Image backgroundImage;
        public GameObject selectedBadge;
        public GameObject lockedOverlay;
        public Button unlockButton;

        [Header("State Colors")]
        public Color selectedBackgroundColor = new Color(0.22f, 0.18f, 0.12f, 0.94f);
        public Color unlockedBackgroundColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        public Color lockedBackgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.95f);
        public Color canAffordCostColor = new Color(1f, 0.9f, 0.35f, 1f);
        public Color cannotAffordCostColor = new Color(1f, 0.45f, 0.45f, 1f);
        public Color normalIconColor = Color.white;
        public Color lockedIconColor = new Color(0.42f, 0.42f, 0.42f, 0.68f);

        public Constants.WeaponType WeaponType { get; private set; }

        public void Bind(WeaponCardViewModel model, WeaponCardVisualState state)
        {
            if (model == null)
                return;

            WeaponType = model.weaponType;

            if (titleText != null)
                titleText.text = model.displayName;
            if (descriptionText != null)
                descriptionText.text = model.description;
            if (fireTypeText != null)
                fireTypeText.text = string.Format(
                    LocalizationManager.Instance.Get("armory.card.fire_format", "Fire: {0}"),
                    model.fireTypeLabel);
            if (fireRateText != null)
                fireRateText.text = string.Format(
                    LocalizationManager.Instance.Get("armory.card.rate_format", "Rate: {0}"),
                    model.fireRateLabel);
            if (reloadText != null)
            {
                reloadText.gameObject.SetActive(true);
                reloadText.text = string.Format(
                    LocalizationManager.Instance.Get("armory.card.reload_format", "Reload: {0}"),
                    model.reloadLabel);
            }
            if (travelSpeedText != null)
            {
                travelSpeedText.gameObject.SetActive(true);
                travelSpeedText.text = string.Format(
                    LocalizationManager.Instance.Get("armory.card.travel_format", "Travel: {0}"),
                    model.travelSpeedLabel);
            }
            if (chainLightningText != null)
            {
                chainLightningText.gameObject.SetActive(true);
                chainLightningText.text = string.Format(
                    LocalizationManager.Instance.Get("armory.card.bullets_format", "Bullets: {0}"),
                    model.bulletsLabel);
            }
            if (aoeText != null)
            {
                aoeText.gameObject.SetActive(true);
                aoeText.text = string.Format(
                    LocalizationManager.Instance.Get("armory.card.aoe_format", "AoE: {0}"),
                    model.aoeLabel);
            }

            if (costText != null)
            {
                bool showCostText = !state.isSelected || state.isLocked;
                costText.gameObject.SetActive(showCostText);

                if (state.isLocked)
                {
                    string coinsSuffix = LocalizationManager.Instance.Get("common.coins_suffix", "COINS");
                    string costFormat = LocalizationManager.Instance.Get("armory.card.locked_cost_format", "{0} {1}");
                    costText.text = string.Format(costFormat, model.cost, coinsSuffix);
                    costText.color = state.canAfford ? canAffordCostColor : cannotAffordCostColor;
                }
                else if (showCostText)
                {
                    costText.text = model.weaponType == Constants.WeaponType.PiranhaGun
                        ? LocalizationManager.Instance.Get("armory.card.state.starter", "Starter")
                        : LocalizationManager.Instance.Get("armory.card.state.unlocked", "Unlocked");
                    costText.color = new Color(0.5f, 1f, 0.5f, 1f);
                }
            }

            if (iconImage != null)
            {
                iconImage.sprite = model.icon;
                iconImage.enabled = model.icon != null;
                iconImage.color = state.isLocked ? lockedIconColor : normalIconColor;
            }

            if (backgroundImage != null)
            {
                if (state.isSelected && !state.isLocked)
                    backgroundImage.color = selectedBackgroundColor;
                else
                    backgroundImage.color = state.isLocked ? lockedBackgroundColor : unlockedBackgroundColor;
            }

            if (selectedBadge != null)
                selectedBadge.SetActive(state.isSelected && !state.isLocked);
            if (lockedOverlay != null)
                lockedOverlay.SetActive(state.isLocked);
            if (unlockButton != null)
                unlockButton.gameObject.SetActive(state.isLocked);

            SetOptionalLocalizedChildText(
                selectedBadge,
                "armory.card.selected",
                "SELECTED");
            SetOptionalLocalizedChildText(
                lockedOverlay,
                "achievements.status.locked",
                "LOCKED");
            SetButtonLabel(
                unlockButton,
                LocalizationManager.Instance.Get("armory.card.unlock_action", "View Unlock"));
        }

        private static void SetOptionalLocalizedChildText(GameObject root, string key, string fallback)
        {
            if (root == null)
                return;

            TextMeshProUGUI text = root.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
                return;

            text.text = LocalizationManager.Instance.Get(key, fallback);
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
                return;

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = value;
        }
    }
}
