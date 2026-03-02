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
        public Color selectedBackgroundColor = new Color(0.16f, 0.42f, 0.22f, 1f);
        public Color unlockedBackgroundColor = new Color(0.15f, 0.17f, 0.22f, 0.95f);
        public Color lockedBackgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        public Color canAffordCostColor = new Color(1f, 0.9f, 0.35f, 1f);
        public Color cannotAffordCostColor = new Color(1f, 0.45f, 0.45f, 1f);

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
                fireTypeText.text = $"Fire: {model.fireTypeLabel}";
            if (fireRateText != null)
                fireRateText.text = $"Rate: {model.fireRateLabel}";
            if (reloadText != null)
                reloadText.text = $"Reload: {model.reloadLabel}";
            if (travelSpeedText != null)
                travelSpeedText.text = $"Travel: {model.travelSpeedLabel}";
            if (chainLightningText != null)
                chainLightningText.text = $"Bullets: {model.bulletsLabel}";
            if (aoeText != null)
                aoeText.text = $"AoE: {model.aoeLabel}";

            if (costText != null)
            {
                if (state.isLocked)
                {
                    costText.text = $"{model.cost} coins";
                    costText.color = state.canAfford ? canAffordCostColor : cannotAffordCostColor;
                }
                else
                {
                    costText.text = model.weaponType == Constants.WeaponType.PiranhaGun ? "Starter" : "Unlocked";
                    costText.color = new Color(0.5f, 1f, 0.5f, 1f);
                }
            }

            if (iconImage != null)
            {
                iconImage.sprite = model.icon;
                iconImage.enabled = model.icon != null;
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
        }
    }
}
