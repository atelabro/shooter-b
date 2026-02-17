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

        [Header("Stage Cards")]
        public Transform stageListContainer;
        public CampaignStageEntryController stageEntryPrefab;

        [Header("Pop Animation")]
        public float animSpeed = 8f;

        private RectTransform rectTransform;
        private CityConfig[] allCities;
        private Coroutine animCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

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
            rectTransform.localScale = Vector3.zero;

            if (animCoroutine != null)
                StopCoroutine(animCoroutine);

            animCoroutine = StartCoroutine(ScaleTo(Vector3.one));
        }

        public void Hide()
        {
            if (animCoroutine != null)
                StopCoroutine(animCoroutine);

            animCoroutine = StartCoroutine(ScaleTo(Vector3.zero, deactivateOnDone: true));
        }

        private void PopulateStageList(CityConfig city)
        {
            foreach (StageConfig stage in city.stages)
            {
                bool isUnlocked = CampaignProgressManager.Instance.IsStageUnlocked(stage, city);
                int stars = CampaignProgressManager.Instance.GetStarsForStage(stage.stageIndex);

                CampaignStageEntryController entry = Instantiate(stageEntryPrefab, stageListContainer);
                StageConfig capturedStage = stage;
                entry.Initialize(stage, isUnlocked, stars, () => OnStageSelected(capturedStage));
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

        private IEnumerator ScaleTo(Vector3 targetScale, bool deactivateOnDone = false)
        {
            while ((rectTransform.localScale - targetScale).sqrMagnitude > 0.0001f)
            {
                rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * animSpeed);
                yield return null;
            }

            rectTransform.localScale = targetScale;

            if (deactivateOnDone)
                gameObject.SetActive(false);
        }
    }
}
