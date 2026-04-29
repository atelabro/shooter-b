using UnityEngine;

namespace ShooterB
{
    public class DuckPartAnimator : MonoBehaviour
    {
        [SerializeField] private DuckPartLibrary partLibrary;

        public bool IsActive => isActive;
        public DuckPartLibrary PartLibrary => partLibrary;
        public Constants.DuckType CurrentDuckType => currentDuckType;
        public float SizeMultiplier
        {
            get
            {
                if (!isActive) return 1f;
                DuckPartSkinConfig visualConfig = GetActiveVisualConfig();
                return visualConfig.sizeMultiplier > 0f ? visualConfig.sizeMultiplier : 1f;
            }
        }

        private bool isActive;
        private DuckPartConfig config;
        private int selectedSkinIndex = -1;
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
            selectedSkinIndex = SelectSkinIndex(config);
            DuckPartSkinConfig visualConfig = GetActiveVisualConfig();

            int sortingLayerID = rootRenderer.sortingLayerID;

            torsoObject = new GameObject("Torso");
            torsoObject.transform.SetParent(transform, false);
            torsoRenderer = torsoObject.AddComponent<SpriteRenderer>();
            torsoRenderer.sprite = visualConfig.torsoSprite;
            torsoRenderer.sortingLayerID = sortingLayerID;
            torsoRenderer.sortingOrder = sortingOrder;

            leftWingPivot = new GameObject("LeftWingPivot");
            leftWingPivot.transform.SetParent(transform, false);
            leftWingPivot.transform.localPosition = new Vector3(visualConfig.leftWingPivotOffset.x, visualConfig.leftWingPivotOffset.y, 0f);
            leftWingObject = new GameObject("LeftWing");
            leftWingObject.transform.SetParent(leftWingPivot.transform, false);
            leftWingObject.transform.localPosition = new Vector3(visualConfig.leftWingOffset.x, visualConfig.leftWingOffset.y, 0f);
            SpriteRenderer leftWingRenderer = leftWingObject.AddComponent<SpriteRenderer>();
            leftWingRenderer.sprite = visualConfig.leftWingSprite;
            leftWingRenderer.sortingLayerID = sortingLayerID;
            leftWingRenderer.sortingOrder = sortingOrder + 1;

            rightWingPivot = new GameObject("RightWingPivot");
            rightWingPivot.transform.SetParent(transform, false);
            rightWingPivot.transform.localPosition = new Vector3(visualConfig.rightWingPivotOffset.x, visualConfig.rightWingPivotOffset.y, 0f);
            rightWingObject = new GameObject("RightWing");
            rightWingObject.transform.SetParent(rightWingPivot.transform, false);
            rightWingObject.transform.localPosition = new Vector3(visualConfig.rightWingOffset.x, visualConfig.rightWingOffset.y, 0f);
            SpriteRenderer rightWingRenderer = rightWingObject.AddComponent<SpriteRenderer>();
            rightWingRenderer.sprite = visualConfig.rightWingSprite != null ? visualConfig.rightWingSprite : visualConfig.leftWingSprite;
            rightWingRenderer.flipX = visualConfig.rightWingSprite == null;
            rightWingRenderer.sortingLayerID = sortingLayerID;
            rightWingRenderer.sortingOrder = sortingOrder - 1;

            animTime = 0f;
            isActive = true;
            return true;
        }

        public Sprite GetNormalizationSprite()
        {
            if (!isActive) return null;
            return GetActiveVisualConfig().torsoSprite;
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

            DuckPartSkinConfig visualConfig = GetActiveVisualConfig();
            animTime += deltaTime;

            if (leftWingPivot != null)
            {
                leftWingPivot.transform.localPosition = new Vector3(visualConfig.leftWingPivotOffset.x, visualConfig.leftWingPivotOffset.y, 0f);
                float leftAngle = Mathf.Sin(animTime * visualConfig.flapSpeed * Mathf.PI * 2f) * visualConfig.flapAmplitude;
                leftWingPivot.transform.localRotation = Quaternion.Euler(0f, 0f, leftAngle);
            }

            if (leftWingObject != null)
                leftWingObject.transform.localPosition = new Vector3(visualConfig.leftWingOffset.x, visualConfig.leftWingOffset.y, 0f);

            if (rightWingPivot != null)
            {
                rightWingPivot.transform.localPosition = new Vector3(visualConfig.rightWingPivotOffset.x, visualConfig.rightWingPivotOffset.y, 0f);
                float rightAngle = Mathf.Sin((animTime + visualConfig.phaseOffset) * visualConfig.flapSpeed * Mathf.PI * 2f) * visualConfig.flapAmplitude;
                // flipX reverses the rotation direction, so negate when right wing is a mirrored copy
                if (visualConfig.rightWingSprite == null) rightAngle = -rightAngle;
                rightWingPivot.transform.localRotation = Quaternion.Euler(0f, 0f, rightAngle);
            }

            if (rightWingObject != null)
                rightWingObject.transform.localPosition = new Vector3(visualConfig.rightWingOffset.x, visualConfig.rightWingOffset.y, 0f);

            if (torsoObject != null && visualConfig.torsoBobAmount > 0f)
            {
                float bobY = Mathf.Sin(animTime * visualConfig.torsoBobSpeed * Mathf.PI * 2f) * visualConfig.torsoBobAmount;
                torsoObject.transform.localPosition = new Vector3(0f, bobY, 0f);
            }
        }

        private DuckPartSkinConfig GetActiveVisualConfig()
        {
            if (selectedSkinIndex <= 0)
                return config.ToSkinConfig();

            if (config.alternativeSkins != null)
            {
                int alternativeIndex = selectedSkinIndex - 1;
                if (alternativeIndex >= 0 && alternativeIndex < config.alternativeSkins.Length)
                    return config.alternativeSkins[alternativeIndex];
            }

            return config.ToSkinConfig();
        }

        private static int SelectSkinIndex(DuckPartConfig partConfig)
        {
            if (partConfig.alternativeSkins == null || partConfig.alternativeSkins.Length == 0)
                return 0;

            return Random.Range(0, partConfig.alternativeSkins.Length + 1);
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
