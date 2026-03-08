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
        [Header("Text Style")]
        public bool applyOutline = true;
        public Color outlineColor = new Color32(27, 37, 51, 255);
        [Range(0f, 1f)] public float outlineWidth = 0.15f;

        private Coroutine activeCoroutine;
        private Color baseTextColor = Color.white;
        private Vector3 baseTextScale = Vector3.one;

        private void Awake()
        {
            if (waveText == null)
                waveText = GetComponentInChildren<TMP_Text>(true);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (waveText != null)
            {
                baseTextColor = waveText.color;
                baseTextScale = waveText.rectTransform.localScale;
                ApplyOutlineToText(waveText);
                SetTextVisual(0f, 0.92f);
            }
        }

        private void ApplyOutlineToText(TMP_Text text)
        {
            if (!applyOutline || text == null)
                return;

            Material instanceMaterial = text.fontMaterial;
            if (instanceMaterial == null)
                return;

            if (instanceMaterial.HasProperty("_OutlineColor"))
                instanceMaterial.SetColor("_OutlineColor", outlineColor);

            if (instanceMaterial.HasProperty("_OutlineWidth"))
                instanceMaterial.SetFloat("_OutlineWidth", Mathf.Clamp01(outlineWidth));

            text.fontMaterial = instanceMaterial;
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
            if (waveText == null)
                yield break;

            string format = LocalizationManager.Instance.Get("campaign.wave.label", "Wave {0}");
            waveText.text = string.Format(format, waveNumber);

            gameObject.SetActive(true);
            SetTextVisual(0f, 0.92f);

            float fadeDuration = 0.22f;
            float totalDuration = Mathf.Max(0f, duration);
            float holdDuration = Mathf.Max(0f, totalDuration - (fadeDuration * 2f));

            yield return AnimateText(0f, 1f, 0.92f, 1.04f, fadeDuration);
            if (holdDuration > 0f)
                yield return new WaitForSeconds(holdDuration);
            yield return AnimateText(1f, 0f, 1.02f, 0.96f, fadeDuration);

            SetTextVisual(0f, 0.96f);
            activeCoroutine = null;
        }

        private IEnumerator AnimateText(float fromAlpha, float toAlpha, float fromScale, float toScale, float duration)
        {
            if (duration <= 0f)
            {
                SetTextVisual(toAlpha, toScale);
                yield break;
            }

            float elapsed = 0f;
            SetTextVisual(fromAlpha, fromScale);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetTextVisual(Mathf.Lerp(fromAlpha, toAlpha, t), Mathf.Lerp(fromScale, toScale, t));
                yield return null;
            }

            SetTextVisual(toAlpha, toScale);
        }

        private void SetTextVisual(float alpha, float scaleMultiplier)
        {
            if (waveText == null)
                return;

            Color c = baseTextColor;
            c.a = Mathf.Clamp01(alpha);
            waveText.color = c;
            waveText.rectTransform.localScale = baseTextScale * scaleMultiplier;
        }
    }
}
