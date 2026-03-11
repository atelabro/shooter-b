using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class ModalDialogAnimator : MonoBehaviour
    {
        [Header("Targets")]
        public GameObject modalRoot;
        public CanvasGroup canvasGroup;
        public Graphic backdropGraphic;
        public RectTransform contentTarget;

        [Header("Show")]
        [Min(0f)] public float showDuration = 0.13f;
        [Range(0.5f, 1.2f)] public float showStartScale = 0.97f;
        [Range(0.8f, 1.2f)] public float showOvershootScale = 1f;

        [Header("Hide")]
        [Min(0f)] public float hideDuration = 0.09f;
        [Range(0.5f, 1.2f)] public float hideEndScale = 0.98f;

        private Coroutine activeTransition;
        private Vector3 contentBaseScale = Vector3.one;
        private float backdropTargetAlpha = 1f;
        private bool hasCapturedContentBaseScale;
        private bool hasCapturedBackdropAlpha;
        private bool isVisible;
        private bool pendingShow;

        public bool IsVisible => isVisible;

        private void Awake()
        {
            EnsureReferences();
            CaptureVisualDefaults();
        }

        private void OnEnable()
        {
            if (!pendingShow)
                return;

            pendingShow = false;

            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(AnimateShow());
        }

        public void Show()
        {
            EnsureReferences();
            CaptureVisualDefaults();

            if (modalRoot != null && !modalRoot.activeSelf)
            {
                pendingShow = true;
                modalRoot.SetActive(true);
                return;
            }

            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(AnimateShow());
        }

        public void Hide()
        {
            EnsureReferences();
            CaptureVisualDefaults();

            if (modalRoot == null)
                return;

            if (!modalRoot.activeSelf && !isVisible)
            {
                HideImmediate();
                return;
            }

            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(AnimateHide());
        }

        public void HideImmediate()
        {
            EnsureReferences();
            CaptureVisualDefaults();

            if (activeTransition != null)
            {
                StopCoroutine(activeTransition);
                activeTransition = null;
            }

            pendingShow = false;

            ApplyVisualState(0f, showStartScale, false);

            if (modalRoot != null)
                modalRoot.SetActive(false);

            isVisible = false;
        }

        private void EnsureReferences()
        {
            if (modalRoot == null)
                modalRoot = gameObject;

            if (canvasGroup == null && modalRoot != null)
                canvasGroup = modalRoot.GetComponent<CanvasGroup>() ?? modalRoot.AddComponent<CanvasGroup>();

            if (backdropGraphic == null && modalRoot != null)
                backdropGraphic = modalRoot.GetComponent<Graphic>();

            if (contentTarget == null && modalRoot != null)
            {
                Transform panelTransform = modalRoot.transform.Find("Panel");
                if (panelTransform != null)
                    contentTarget = panelTransform as RectTransform;
            }

            if (contentTarget == null && modalRoot != null)
            {
                for (int i = 0; i < modalRoot.transform.childCount; i++)
                {
                    RectTransform childRect = modalRoot.transform.GetChild(i) as RectTransform;
                    if (childRect != null)
                    {
                        contentTarget = childRect;
                        break;
                    }
                }
            }
        }

        private void CaptureVisualDefaults()
        {
            if (contentTarget != null && !hasCapturedContentBaseScale)
            {
                contentBaseScale = contentTarget.localScale;
                hasCapturedContentBaseScale = true;
            }

            if (backdropGraphic != null && !hasCapturedBackdropAlpha)
            {
                backdropTargetAlpha = backdropGraphic.color.a;
                hasCapturedBackdropAlpha = true;
            }
            else if (backdropGraphic == null)
                backdropTargetAlpha = 1f;
        }

        private IEnumerator AnimateShow()
        {
            if (modalRoot == null)
                yield break;

            modalRoot.SetActive(true);
            ApplyVisualState(0f, showStartScale, false);

            float duration = Mathf.Max(0.0001f, showDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBack01(t);
                float alpha = Mathf.LerpUnclamped(0f, 1f, t);
                float scale = Mathf.LerpUnclamped(showStartScale, showOvershootScale, eased);
                ApplyVisualState(alpha, scale, false);
                yield return null;
            }

            ApplyVisualState(1f, 1f, true);
            isVisible = true;
            activeTransition = null;
        }

        private IEnumerator AnimateHide()
        {
            float duration = Mathf.Max(0.0001f, hideDuration);
            float elapsed = 0f;

            ApplyVisualState(1f, 1f, false);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInCubic01(t);
                float alpha = Mathf.LerpUnclamped(1f, 0f, eased);
                float scale = Mathf.LerpUnclamped(1f, hideEndScale, eased);
                ApplyVisualState(alpha, scale, false);
                yield return null;
            }

            ApplyVisualState(0f, showStartScale, false);

            if (modalRoot != null)
                modalRoot.SetActive(false);

            isVisible = false;
            activeTransition = null;
        }

        private void ApplyVisualState(float normalizedAlpha, float contentScaleMultiplier, bool interactive)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(normalizedAlpha);
                canvasGroup.interactable = interactive;
                canvasGroup.blocksRaycasts = interactive;
            }

            if (backdropGraphic != null)
            {
                Color color = backdropGraphic.color;
                color.a = backdropTargetAlpha * Mathf.Clamp01(normalizedAlpha);
                backdropGraphic.color = color;
            }

            if (contentTarget != null)
                contentTarget.localScale = contentBaseScale * contentScaleMultiplier;
        }

        private static float EaseInCubic01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        private static float EaseOutBack01(float t)
        {
            t = Mathf.Clamp01(t);
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + (c3 * x * x * x) + (c1 * x * x);
        }
    }
}
