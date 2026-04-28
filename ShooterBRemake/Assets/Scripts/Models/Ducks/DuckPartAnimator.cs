using UnityEngine;

namespace ShooterB
{
    public class DuckPartAnimator : MonoBehaviour
    {
        [SerializeField] private DuckPartLibrary partLibrary;

        public bool IsActive => isActive;
        public DuckPartLibrary PartLibrary => partLibrary;
        public Constants.DuckType CurrentDuckType => currentDuckType;
        public float SizeMultiplier => isActive && config.sizeMultiplier > 0f ? config.sizeMultiplier : 1f;

        private bool isActive;
        private DuckPartConfig config;
        private float animTime;
        private Constants.DuckType currentDuckType;

        private GameObject torsoObject;
        private SpriteRenderer torsoRenderer;
        private GameObject leftWingPivot;
        private GameObject leftWingObject;
        private GameObject rightWingPivot;
        private GameObject rightWingObject;

        public bool TryInitialize(Constants.DuckType type, SpriteRenderer rootRenderer, int sortingOrder)
        {
            if (partLibrary == null || rootRenderer == null || !partLibrary.TryGetConfig(type, out config))
                return false;

            currentDuckType = type;
            rootRenderer.enabled = false;

            int sortingLayerID = rootRenderer.sortingLayerID;

            torsoObject = new GameObject("Torso");
            torsoObject.transform.SetParent(transform, false);
            torsoRenderer = torsoObject.AddComponent<SpriteRenderer>();
            torsoRenderer.sprite = config.torsoSprite;
            torsoRenderer.sortingLayerID = sortingLayerID;
            torsoRenderer.sortingOrder = sortingOrder;

            leftWingPivot = new GameObject("LeftWingPivot");
            leftWingPivot.transform.SetParent(transform, false);
            leftWingPivot.transform.localPosition = new Vector3(config.leftWingPivotOffset.x, config.leftWingPivotOffset.y, 0f);
            leftWingObject = new GameObject("LeftWing");
            leftWingObject.transform.SetParent(leftWingPivot.transform, false);
            leftWingObject.transform.localPosition = new Vector3(config.leftWingOffset.x, config.leftWingOffset.y, 0f);
            SpriteRenderer leftWingRenderer = leftWingObject.AddComponent<SpriteRenderer>();
            leftWingRenderer.sprite = config.leftWingSprite;
            leftWingRenderer.sortingLayerID = sortingLayerID;
            leftWingRenderer.sortingOrder = sortingOrder + 1;

            rightWingPivot = new GameObject("RightWingPivot");
            rightWingPivot.transform.SetParent(transform, false);
            rightWingPivot.transform.localPosition = new Vector3(config.rightWingPivotOffset.x, config.rightWingPivotOffset.y, 0f);
            rightWingObject = new GameObject("RightWing");
            rightWingObject.transform.SetParent(rightWingPivot.transform, false);
            rightWingObject.transform.localPosition = new Vector3(config.rightWingOffset.x, config.rightWingOffset.y, 0f);
            SpriteRenderer rightWingRenderer = rightWingObject.AddComponent<SpriteRenderer>();
            rightWingRenderer.sprite = config.rightWingSprite != null ? config.rightWingSprite : config.leftWingSprite;
            rightWingRenderer.flipX = config.rightWingSprite == null;
            rightWingRenderer.sortingLayerID = sortingLayerID;
            rightWingRenderer.sortingOrder = sortingOrder - 1;

            animTime = 0f;
            isActive = true;
            return true;
        }

        public Sprite GetNormalizationSprite()
        {
            if (!isActive) return null;
            return config.torsoSprite;
        }

        public Bounds GetWorldBounds()
        {
            if (!isActive || torsoRenderer == null) return default;
            return torsoRenderer.bounds;
        }

        public void Tick(float deltaTime)
        {
            if (!isActive) return;

            if (partLibrary != null)
                partLibrary.TryGetConfig(currentDuckType, out config);

            animTime += deltaTime;

            if (leftWingPivot != null)
            {
                leftWingPivot.transform.localPosition = new Vector3(config.leftWingPivotOffset.x, config.leftWingPivotOffset.y, 0f);
                float leftAngle = Mathf.Sin(animTime * config.flapSpeed * Mathf.PI * 2f) * config.flapAmplitude;
                leftWingPivot.transform.localRotation = Quaternion.Euler(0f, 0f, leftAngle);
            }

            if (leftWingObject != null)
                leftWingObject.transform.localPosition = new Vector3(config.leftWingOffset.x, config.leftWingOffset.y, 0f);

            if (rightWingPivot != null)
            {
                rightWingPivot.transform.localPosition = new Vector3(config.rightWingPivotOffset.x, config.rightWingPivotOffset.y, 0f);
                float rightAngle = Mathf.Sin((animTime + config.phaseOffset) * config.flapSpeed * Mathf.PI * 2f) * config.flapAmplitude;
                // flipX reverses the rotation direction, so negate when right wing is a mirrored copy
                if (config.rightWingSprite == null) rightAngle = -rightAngle;
                rightWingPivot.transform.localRotation = Quaternion.Euler(0f, 0f, rightAngle);
            }

            if (rightWingObject != null)
                rightWingObject.transform.localPosition = new Vector3(config.rightWingOffset.x, config.rightWingOffset.y, 0f);

            if (torsoObject != null && config.torsoBobAmount > 0f)
            {
                float bobY = Mathf.Sin(animTime * config.torsoBobSpeed * Mathf.PI * 2f) * config.torsoBobAmount;
                torsoObject.transform.localPosition = new Vector3(0f, bobY, 0f);
            }
        }

        public void PrepareForDeath(SpriteRenderer rootRenderer)
        {
            TearDown(rootRenderer);
        }

        public void ResetState(SpriteRenderer rootRenderer)
        {
            TearDown(rootRenderer);
        }

        private void TearDown(SpriteRenderer rootRenderer)
        {
            DestroyParts();
            if (rootRenderer != null)
            {
                rootRenderer.sprite = null;
                rootRenderer.enabled = true;
            }
            isActive = false;
        }

        private void DestroyParts()
        {
            if (torsoObject != null) { torsoObject.SetActive(false); Destroy(torsoObject); }
            if (leftWingPivot != null) { leftWingPivot.SetActive(false); Destroy(leftWingPivot); }
            if (rightWingPivot != null) { rightWingPivot.SetActive(false); Destroy(rightWingPivot); }
            torsoObject = null;
            torsoRenderer = null;
            leftWingPivot = null;
            leftWingObject = null;
            rightWingPivot = null;
            rightWingObject = null;
        }
    }
}
