using System.Collections;
using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class GameStartingModalController : MonoBehaviour
    {
        [Header("Modal Root")]
        public GameObject modalRoot;

        [Header("Stage Info")]
        public TMP_Text titleText;
        public TMP_Text briefingText;

        [Header("Countdown")]
        public TMP_Text countdownText;
        public float countdownStepSeconds = 1f;
        public float startMessageSeconds = 0.75f;

        public void Configure(string stageName, string stageBriefing)
        {
            EnsureReferences();

            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(stageName)
                    ? LocalizationManager.Instance.Get("campaign.starting.default_stage", "Stage")
                    : stageName;

            if (briefingText != null)
                briefingText.text = stageBriefing ?? string.Empty;
        }

        public IEnumerator PlayCountdown()
        {
            EnsureReferences();

            if (modalRoot != null)
                modalRoot.SetActive(true);

            float stepDelay = Mathf.Max(0.05f, countdownStepSeconds);
            float startDelay = Mathf.Max(0.05f, startMessageSeconds);

            SetCountdownText("3");
            yield return new WaitForSecondsRealtime(stepDelay);

            SetCountdownText("2");
            yield return new WaitForSecondsRealtime(stepDelay);

            SetCountdownText("1");
            yield return new WaitForSecondsRealtime(stepDelay);

            SetCountdownText(LocalizationManager.Instance.Get("campaign.starting.start", "Start"));
            yield return new WaitForSecondsRealtime(startDelay);

            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        private void SetCountdownText(string value)
        {
            if (countdownText != null)
                countdownText.text = value;
        }

        private void EnsureReferences()
        {
            if (modalRoot == null)
                modalRoot = gameObject;

            if (titleText == null)
            {
                Transform titleTransform = transform.Find("Card/TitleText");
                if (titleTransform != null)
                    titleText = titleTransform.GetComponent<TMP_Text>();
            }

            if (briefingText == null)
            {
                Transform briefingTransform = transform.Find("Card/BriefingText");
                if (briefingTransform != null)
                    briefingText = briefingTransform.GetComponent<TMP_Text>();
            }

            if (countdownText == null)
            {
                Transform countdownTransform = transform.Find("Card/CountdownText");
                if (countdownTransform != null)
                    countdownText = countdownTransform.GetComponent<TMP_Text>();
            }
        }
    }
}
