using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace ShooterB
{
    public class CityPanelController : MonoBehaviour
    {
        public event Action OnPanelHidden;

        [Header("UI Elements")]
        public TextMeshProUGUI cityNameText;
        public TextMeshProUGUI briefingText;
        public Button closeButton;
        [Header("Briefing Animation")]
        [Min(1f)] public float typingCharsPerSecond = 45f;
        [Min(0f)] public float punctuationPause = 0.08f;

        [Header("Stage Cards")]
        public Transform stageListContainer;
        public CampaignStageEntryController stageEntryPrefab;

        private Coroutine briefingTypingCoroutine;
        private CityConfig[] allCities;
        private CityConfig currentCity;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            gameObject.SetActive(false);
        }

        public void Initialize(CityConfig[] _cities)
        {
            allCities = _cities;
        }

        public void Show(CityConfig city)
        {
            if (city == null)
                return;

            currentCity = city;
            ClearStageList();
            PopulateStageList(city);

            if (cityNameText != null)
                cityNameText.text = CampaignLocalizationResolver.GetCityName(city);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (briefingText != null)
            {
                if (briefingTypingCoroutine != null)
                    StopCoroutine(briefingTypingCoroutine);

                briefingTypingCoroutine = StartCoroutine(TypeBriefing(CampaignLocalizationResolver.GetCityBriefing(city)));
            }
        }

        public void Hide()
        {
            if (briefingTypingCoroutine != null)
            {
                StopCoroutine(briefingTypingCoroutine);
                briefingTypingCoroutine = null;
            }

            gameObject.SetActive(false);
            OnPanelHidden?.Invoke();
        }

        public void RefreshLocalizationIfVisible()
        {
            if (!gameObject.activeInHierarchy || currentCity == null)
                return;

            Show(currentCity);
        }

        private void PopulateStageList(CityConfig city)
        {
            if (stageListContainer == null || stageEntryPrefab == null || city == null || city.stages == null)
                return;

            foreach (StageConfig stage in city.stages)
            {
                bool isUnlocked = CampaignProgressManager.Instance.IsStageUnlocked(stage, city, allCities);
                int stars = CampaignProgressManager.Instance.GetStarsForStage(stage.stageIndex);

                CampaignStageEntryController entry = Instantiate(stageEntryPrefab, stageListContainer);
                StageConfig capturedStage = stage;
                CityConfig capturedCity = city;
                entry.Initialize(stage, isUnlocked, stars, () => OnStageSelected(capturedCity, capturedStage));
            }
        }

        private void OnStageSelected(CityConfig city, StageConfig stage)
        {
            CampaignProgressManager.Instance.SetActiveCampaignLocation(city, stage);
            SceneController.Instance.LoadCampaignStage(stage);
        }

        private void ClearStageList()
        {
            if (stageListContainer == null)
                return;

            foreach (Transform child in stageListContainer)
                Destroy(child.gameObject);
        }

        private IEnumerator TypeBriefing(string fullText)
        {
            if (briefingText == null)
                yield break;

            briefingText.text = string.Empty;

            if (string.IsNullOrEmpty(fullText))
            {
                briefingTypingCoroutine = null;
                yield break;
            }

            float secondsPerCharacter = 1f / Mathf.Max(1f, typingCharsPerSecond);
            for (int i = 0; i < fullText.Length; i++)
            {
                briefingText.text += fullText[i];
                yield return new WaitForSeconds(secondsPerCharacter);

                if (IsPunctuation(fullText[i]))
                    yield return new WaitForSeconds(punctuationPause);
            }

            briefingTypingCoroutine = null;
        }

        private static bool IsPunctuation(char c)
        {
            return c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':';
        }
    }
}
