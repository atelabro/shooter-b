using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class SuperPowerCardItemUI : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI ownedText;
        public TextMeshProUGUI costText;
        public Image iconImage;
        public Image backgroundImage;
        public Button buyButton;

        public Color backgroundColor = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        public Color canAffordCostColor = new Color(1f, 0.9f, 0.35f, 1f);
        public Color cannotAffordCostColor = new Color(1f, 0.45f, 0.45f, 1f);

        public void Bind(SuperPowerViewModel model, bool canAfford)
        {
            if (model == null)
                return;

            if (titleText != null)
                titleText.text = model.displayName;
            if (descriptionText != null)
                descriptionText.text = model.description;
            if (ownedText != null)
                ownedText.text = string.Format(LocalizationManager.Instance.Get("armory.card.owned_format", "Owned: {0}"), model.ownedCount);
            if (costText != null)
            {
                string coinsSuffix = LocalizationManager.Instance.Get("common.coins_suffix", "COINS");
                string costFormat = LocalizationManager.Instance.Get("armory.modal.cost_format", "Cost: {0} {1}");
                costText.text = string.Format(costFormat, model.cost, coinsSuffix);
                costText.color = canAfford ? canAffordCostColor : cannotAffordCostColor;
            }

            if (iconImage != null)
            {
                iconImage.sprite = model.icon;
                iconImage.enabled = model.icon != null;
            }

            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;

            if (buyButton != null)
            {
                buyButton.interactable = canAfford;
                TextMeshProUGUI label = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = LocalizationManager.Instance.Get("armory.card.buy_action", "BUY");
            }
        }
    }
}
