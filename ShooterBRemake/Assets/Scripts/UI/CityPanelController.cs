using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace ShooterB
{
    public class CityPanelController : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI cityNameText;
        public TextMeshProUGUI briefingText;
        public Button closeButton;

        [Header("Stage Entries")]
        public Transform stageListContainer;
        public CampaignStageEntryController stageEntryPrefab;

        [Header("Slide Animation")]
        public float slideSpeed = 8f;

        private RectTransform rectTransform;
        private float panelWidth;
        private CityConfig[] allCities;
        private Coroutine slideCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            panelWidth = rectTransform.rect.width;

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            gameObject.SetActive(false);
        }

        public void Initialize(CityConfig[] cities)
        {
            allCities = cities;
        }

        public void Show(CityConfig city)
        {
            ClearStageList();
            PopulateStageList(city);

            if (cityNameText != null)
                cityNameText.text = city.cityName;

            if (briefingText != null)
                briefingText.text = city.briefingText;

            gameObject.SetActive(true);

            if (slideCoroutine != null)
                StopCoroutine(slideCoroutine);

            slideCoroutine = StartCoroutine(SlideTo(0f));
        }

        public void Hide()
        {
            if (slideCoroutine != null)
                StopCoroutine(slideCoroutine);

            slideCoroutine = StartCoroutine(SlideTo(panelWidth, deactivateOnDone: true));
        }

        private void PopulateStageList(CityConfig city)
        {
            foreach (StageConfig stage in city.stages)
            {
                bool isUnlocked = CampaignProgressManager.Instance.IsStageUnlocked(stage, city);
                int stars = CampaignProgressManager.Instance.GetStarsForStage(stage.stageIndex);

                CampaignStageEntryController entry = Instantiate(stageEntryPrefab, stageListContainer);
                StageConfig capturedStage = stage;
                entry.Initialize(stage, city, isUnlocked, stars, () => OnStageSelected(capturedStage));
            }
        }

        private void OnStageSelected(StageConfig stage)
        {
            CampaignProgressManager.Instance.SetActiveStage(stage);
            SceneController.Instance.LoadCampaignStage(stage);
        }

        private void ClearStageList()
        {
            foreach (Transform child in stageListContainer)
                Destroy(child.gameObject);
        }

        private IEnumerator SlideTo(float targetX, bool deactivateOnDone = false)
        {
            while (!Mathf.Approximately(rectTransform.anchoredPosition.x, targetX))
            {
                float newX = Mathf.Lerp(rectTransform.anchoredPosition.x, targetX, Time.deltaTime * slideSpeed);
                rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
                yield return null;
            }

            rectTransform.anchoredPosition = new Vector2(targetX, rectTransform.anchoredPosition.y);

            if (deactivateOnDone)
                gameObject.SetActive(false);
        }
    }
}
