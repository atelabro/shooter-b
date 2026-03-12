using UnityEngine;

namespace ShooterB
{
    public class DuckHealthBar : MonoBehaviour
    {
        private const float DefaultWidth = 1.2f;
        private const float DefaultHeight = 0.16f;
        private static Sprite defaultBarSprite;

        private Transform barRoot;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer fillRenderer;

        public void EnsureInitialized(Transform owner, int sortingOrder)
        {
            if (barRoot != null)
            {
                ApplySorting(sortingOrder);
                return;
            }

            barRoot = new GameObject("HealthBar").transform;
            barRoot.SetParent(owner, false);

            backgroundRenderer = CreateBarPart("Background", barRoot, new Color(0.18f, 0.12f, 0.1f, 0.95f), sortingOrder);
            fillRenderer = CreateBarPart("Fill", barRoot, new Color(0.29f, 0.86f, 0.36f, 0.95f), sortingOrder + 1);
            fillRenderer.drawMode = SpriteDrawMode.Sliced;
            fillRenderer.size = new Vector2(DefaultWidth - 0.04f, DefaultHeight - 0.04f);

            Hide();
        }

        public void SetLayout(float yOffset, int sortingOrder)
        {
            if (barRoot == null)
                return;

            barRoot.localPosition = new Vector3(0f, yOffset, 0f);
            backgroundRenderer.drawMode = SpriteDrawMode.Sliced;
            backgroundRenderer.size = new Vector2(DefaultWidth, DefaultHeight);
            fillRenderer.transform.localPosition = new Vector3(-0.02f, 0f, -0.01f);
            ApplySorting(sortingOrder);
        }

        public void SetVisible(bool visible)
        {
            if (barRoot == null)
                return;

            barRoot.gameObject.SetActive(visible);
        }

        public void UpdateFill(float normalizedHealth)
        {
            if (fillRenderer == null)
                return;

            float clamped = Mathf.Clamp01(normalizedHealth);
            float fillWidth = Mathf.Lerp(0f, DefaultWidth - 0.04f, clamped);
            fillRenderer.size = new Vector2(fillWidth, DefaultHeight - 0.04f);

            if (fillWidth > 0f)
                fillRenderer.transform.localPosition = new Vector3((-DefaultWidth * 0.5f) + (fillWidth * 0.5f) + 0.02f, 0f, -0.01f);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void ApplySorting(int sortingOrder)
        {
            if (backgroundRenderer != null)
                backgroundRenderer.sortingOrder = sortingOrder;

            if (fillRenderer != null)
                fillRenderer.sortingOrder = sortingOrder + 1;
        }

        private static SpriteRenderer CreateBarPart(string objectName, Transform parent, Color color, int sortingOrder)
        {
            GameObject part = new GameObject(objectName);
            part.transform.SetParent(parent, false);

            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = GetDefaultBarSprite();
            renderer.color = color;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Sliced;
            return renderer;
        }

        private static Sprite GetDefaultBarSprite()
        {
            if (defaultBarSprite == null)
                defaultBarSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

            return defaultBarSprite;
        }
    }
}
