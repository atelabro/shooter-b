using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShooterB
{
    public class CampaignMapController : MonoBehaviour
    {
        [Header("City Data")]
        public CityConfig[] cities;

        [Header("City Pins")]
        public CityPinController[] cityPins;

        [Header("City Panel")]
        public CityPanelController cityPanel;

        [Header("Top Bar")]
        public TextMeshProUGUI totalStarsText;

        [Header("Buttons")]
        public Button backButton;

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (cityPanel != null)
            cityPanel.Initialize(cities);

            RefreshPins();
            RefreshTotalStars();
        }

        private void RefreshPins()
        {
            for (int i = 0; i < cityPins.Length; i++)
            {
                if (i >= cities.Length)
                    break;

                CityConfig city = cities[i];
                bool isUnlocked = CampaignProgressManager.Instance.IsCityUnlocked(city, cities);
                CityConfig capturedCity = city;

                cityPins[i].Initialize(city, isUnlocked, () => OnCityPinClicked(capturedCity));
            }
        }

        private void RefreshTotalStars()
        {
            if (totalStarsText == null)
                return;

            int earned = CampaignProgressManager.Instance.GetTotalStars(cities);
            int max = CampaignProgressManager.Instance.GetMaxStars(cities);
            totalStarsText.text = $"{earned} / {max}";
        }

        private void OnCityPinClicked(CityConfig city)
        {
            if (cityPanel != null)
                cityPanel.Show(city);
        }

        private void OnBackClicked()
        {
            SceneController.Instance.ReturnToMenu();
        }
    }
}
