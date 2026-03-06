using System.Collections;
using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class WaveAnnouncementController : MonoBehaviour
    {
        [Header("References")]
        public TMP_Text waveText;
        public CanvasGroup canvasGroup;

        private Coroutine activeCoroutine;

        private void Awake()
        {
            if (waveText == null)
                waveText = GetComponentInChildren<TMP_Text>(true);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        public void ShowWave(int waveNumber, float duration)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            activeCoroutine = StartCoroutine(ShowWaveCoroutine(waveNumber, duration));
        }

        private IEnumerator ShowWaveCoroutine(int waveNumber, float duration)
        {
            if (waveText == null || canvasGroup == null)
                yield break;

            string format = LocalizationManager.Instance.Get("campaign.wave.label", "Wave {0}");
            waveText.text = string.Format(format, waveNumber);

            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            float fadeDuration = 0.3f;
            float totalDuration = Mathf.Max(0f, duration);
            float holdDuration = Mathf.Max(0f, totalDuration - (fadeDuration * 2f));

            yield return FadeCanvas(0f, 1f, fadeDuration);
            if (holdDuration > 0f)
                yield return new WaitForSeconds(holdDuration);
            yield return FadeCanvas(1f, 0f, fadeDuration);

            canvasGroup.alpha = 0f;
            activeCoroutine = null;
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                canvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            canvasGroup.alpha = to;
        }
    }
}
