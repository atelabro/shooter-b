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
        [Min(0f)] public float showDuration = 0.18f;
        [Range(0.5f, 1.2f)] public float showStartScale = 0.94f;
        [Range(0.8f, 1.2f)] public float showOvershootScale = 1.015f;
        public Vector2 showStartOffset = new Vector2(0f, -28f);

        [Header("Hide")]
        [Min(0f)] public float hideDuration = 0.14f;
        [Range(0.5f, 1.2f)] public float hideEndScale = 0.96f;
        public Vector2 hideEndOffset = new Vector2(0f, -18f);

        private Coroutine activeTransition;
        private Vector3 contentBaseScale = Vector3.one;
        private Vector2 contentBaseAnchoredPosition = Vector2.zero;
        private bool hasCapturedContentBaseScale;
        private bool hasCapturedContentBaseAnchoredPosition;
        private bool isVisible;
        private bool pendingShow;

        public bool IsVisible => isVisible;

        public float HideDuration => Mathf.Max(0.0001f, hideDuration);

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

        public float HideWithDelay()
        {
            Hide();
            return HideDuration;
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

            ApplyVisualState(0f, showStartScale, showStartOffset, false);

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
                Transform preferredChild = FindPreferredContentChild(modalRoot.transform);
                if (preferredChild != null)
                    contentTarget = preferredChild as RectTransform;
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

        private static Transform FindPreferredContentChild(Transform root)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;

                string childName = child.name;
                if (string.IsNullOrEmpty(childName))
                    continue;

                string lowered = childName.ToLowerInvariant();
                if (lowered == "panel" || lowered.EndsWith("card"))
                    return child;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;

                string childName = child.name;
                if (string.IsNullOrEmpty(childName))
                    return child;

                string lowered = childName.ToLowerInvariant();
                if (lowered.Contains("backdrop"))
                    continue;

                return child;
            }

            return null;
        }

        private void CaptureVisualDefaults()
        {
            if (contentTarget != null && !hasCapturedContentBaseScale)
            {
                contentBaseScale = contentTarget.localScale;
                hasCapturedContentBaseScale = true;
            }

            if (contentTarget != null && !hasCapturedContentBaseAnchoredPosition)
            {
                contentBaseAnchoredPosition = contentTarget.anchoredPosition;
                hasCapturedContentBaseAnchoredPosition = true;
            }

        }

        private IEnumerator AnimateShow()
        {
            if (modalRoot == null)
                yield break;

            modalRoot.SetActive(true);
            ApplyVisualState(1f, showStartScale, showStartOffset, false);

            float duration = Mathf.Max(0.0001f, showDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleEase = EaseOutBackSoft01(t);
                float moveEase = EaseOutCubic01(t);
                float scale = Mathf.LerpUnclamped(showStartScale, showOvershootScale, scaleEase);
                Vector2 offset = Vector2.LerpUnclamped(showStartOffset, Vector2.zero, moveEase);
                ApplyVisualState(1f, scale, offset, false);
                yield return null;
            }

            ApplyVisualState(1f, 1f, Vector2.zero, true);
            isVisible = true;
            activeTransition = null;
        }

        private IEnumerator AnimateHide()
        {
            float duration = Mathf.Max(0.0001f, hideDuration);
            float elapsed = 0f;

            ApplyVisualState(1f, 1f, Vector2.zero, false);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = EaseInCubic01(t);
                float scale = Mathf.LerpUnclamped(1f, hideEndScale, ease);
                Vector2 offset = Vector2.LerpUnclamped(Vector2.zero, hideEndOffset, ease);
                ApplyVisualState(1f, scale, offset, false);
                yield return null;
            }

            ApplyVisualState(1f, hideEndScale, hideEndOffset, false);

            if (modalRoot != null)
                modalRoot.SetActive(false);

            isVisible = false;
            activeTransition = null;
        }

        private void ApplyVisualState(float normalizedAlpha, float contentScaleMultiplier, Vector2 contentOffset, bool interactive)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(normalizedAlpha);
                canvasGroup.interactable = interactive;
                canvasGroup.blocksRaycasts = interactive;
            }

            if (backdropGraphic != null)
                backdropGraphic.enabled = true;

            if (contentTarget != null)
            {
                contentTarget.localScale = contentBaseScale * contentScaleMultiplier;
                contentTarget.anchoredPosition = contentBaseAnchoredPosition + contentOffset;
            }
        }

        private static float EaseInCubic01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        private static float EaseOutCubic01(float t)
        {
            t = Mathf.Clamp01(t);
            float x = 1f - t;
            return 1f - (x * x * x);
        }

        private static float EaseOutBackSoft01(float t)
        {
            t = Mathf.Clamp01(t);
            float c1 = 1.1f;
            float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + (c3 * x * x * x) + (c1 * x * x);
        }
    }
}
