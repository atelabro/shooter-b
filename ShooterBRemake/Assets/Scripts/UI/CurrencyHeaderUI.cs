using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class CurrencyHeaderUI : MonoBehaviour
    {
        public TextMeshProUGUI coinsText;

        private void Awake()
        {
            if (coinsText == null)
            {
                Transform rewardText = transform.Find("RewardText");
                if (rewardText != null)
                    coinsText = rewardText.GetComponent<TextMeshProUGUI>();

                if (coinsText == null)
                {
                    Transform coinsTextTransform = transform.Find("CoinsText");
                    if (coinsTextTransform != null)
                        coinsText = coinsTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }

        private void HandleCoinsChanged(int coins)
        {
            if (coinsText == null)
                return;

            coinsText.text = Mathf.Max(0, coins).ToString();
        }

        private void Refresh()
        {
            if (coinsText == null || GameManager.Instance == null)
                return;

            coinsText.text = Mathf.Max(0, GameManager.Instance.Coins).ToString();
        }
    }
}
