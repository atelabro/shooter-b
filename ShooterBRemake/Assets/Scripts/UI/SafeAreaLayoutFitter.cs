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
        public bool showDebugOverlay = false;
        public bool logSafeAreaChanges = false;

        private Rect lastSafeArea = new Rect(0f, 0f, -1f, -1f);
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;
        private static Texture2D debugTexture;

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

            if (logSafeAreaChanges)
                GameLog.Log($"[SAFE AREA] {name} safeArea={safeArea} screen={screenWidth}x{screenHeight} orientation={orientation}");

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

        private void OnGUI()
        {
            if (!showDebugOverlay)
                return;

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            Rect safeArea = GetValidatedSafeArea(screenWidth, screenHeight);
            Rect guiSafe = ToGuiRect(safeArea, screenHeight);

            DrawRect(new Rect(0f, 0f, screenWidth, guiSafe.yMin), new Color(1f, 0f, 0f, 0.2f));
            DrawRect(new Rect(0f, guiSafe.yMax, screenWidth, screenHeight - guiSafe.yMax), new Color(1f, 0f, 0f, 0.2f));
            DrawRect(new Rect(0f, guiSafe.yMin, guiSafe.xMin, guiSafe.height), new Color(1f, 0f, 0f, 0.2f));
            DrawRect(new Rect(guiSafe.xMax, guiSafe.yMin, screenWidth - guiSafe.xMax, guiSafe.height), new Color(1f, 0f, 0f, 0.2f));

            const float border = 2f;
            DrawRect(new Rect(guiSafe.xMin, guiSafe.yMin, guiSafe.width, border), Color.green);
            DrawRect(new Rect(guiSafe.xMin, guiSafe.yMax - border, guiSafe.width, border), Color.green);
            DrawRect(new Rect(guiSafe.xMin, guiSafe.yMin, border, guiSafe.height), Color.green);
            DrawRect(new Rect(guiSafe.xMax - border, guiSafe.yMin, border, guiSafe.height), Color.green);
        }

        private static Rect ToGuiRect(Rect safeArea, int screenHeight)
        {
            float topY = screenHeight - safeArea.yMax;
            return new Rect(safeArea.xMin, topY, safeArea.width, safeArea.height);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            if (debugTexture == null)
            {
                debugTexture = new Texture2D(1, 1);
                debugTexture.SetPixel(0, 0, Color.white);
                debugTexture.Apply();
            }

            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, debugTexture);
            GUI.color = prev;
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
