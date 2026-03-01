using UnityEngine;

namespace ShooterB
{
    [DisallowMultipleComponent]
    public class SafeAreaLayoutFitter : MonoBehaviour
    {
        public RectTransform targetRect;
        public bool applyLeft = true;
        public bool applyRight = true;
        public bool applyTop = true;
        public bool applyBottom = true;
        public Vector2 extraPaddingPx = Vector2.zero;

        private Rect lastSafeArea = new Rect(0f, 0f, -1f, -1f);
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        private void Reset()
        {
            targetRect = transform as RectTransform;
        }

        private void Awake()
        {
            if (targetRect == null)
                targetRect = transform as RectTransform;
        }

        private void OnEnable()
        {
            ApplySafeArea(force: true);
        }

        private void LateUpdate()
        {
            ApplySafeArea(force: false);
        }

        private void ApplySafeArea(bool force)
        {
            if (targetRect == null)
                return;

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            Rect safeArea = GetValidatedSafeArea(screenWidth, screenHeight);
            ScreenOrientation orientation = Screen.orientation;

            bool changed = force
                || safeArea != lastSafeArea
                || lastScreenSize.x != screenWidth
                || lastScreenSize.y != screenHeight
                || orientation != lastOrientation;

            if (!changed)
                return;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(screenWidth, screenHeight);
            lastOrientation = orientation;

            float left = (safeArea.xMin + (applyLeft ? extraPaddingPx.x : 0f)) / screenWidth;
            float right = (safeArea.xMax - (applyRight ? extraPaddingPx.x : 0f)) / screenWidth;
            float bottom = (safeArea.yMin + (applyBottom ? extraPaddingPx.y : 0f)) / screenHeight;
            float top = (safeArea.yMax - (applyTop ? extraPaddingPx.y : 0f)) / screenHeight;

            Vector2 anchorMin = targetRect.anchorMin;
            Vector2 anchorMax = targetRect.anchorMax;

            if (applyLeft)
                anchorMin.x = Mathf.Clamp01(left);
            if (applyBottom)
                anchorMin.y = Mathf.Clamp01(bottom);
            if (applyRight)
                anchorMax.x = Mathf.Clamp01(right);
            if (applyTop)
                anchorMax.y = Mathf.Clamp01(top);

            if (anchorMin.x > anchorMax.x)
                anchorMin.x = anchorMax.x;
            if (anchorMin.y > anchorMax.y)
                anchorMin.y = anchorMax.y;

            targetRect.anchorMin = anchorMin;
            targetRect.anchorMax = anchorMax;
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;
        }

        private static Rect GetValidatedSafeArea(int screenWidth, int screenHeight)
        {
            Rect raw = Screen.safeArea;
            Rect full = new Rect(0f, 0f, screenWidth, screenHeight);

            bool invalidBounds = raw.width <= 0f
                || raw.height <= 0f
                || raw.xMin < 0f
                || raw.yMin < 0f
                || raw.xMax > screenWidth
                || raw.yMax > screenHeight;

            // Some device simulators/reporting paths can return a very small safe area.
            // Fall back to full screen to avoid collapsing HUD into a thin strip.
            float widthRatio = raw.width / Mathf.Max(1f, screenWidth);
            float heightRatio = raw.height / Mathf.Max(1f, screenHeight);
            bool implausiblySmall = widthRatio < 0.5f || heightRatio < 0.5f;

            return invalidBounds || implausiblySmall ? full : raw;
        }
    }
}
