using UnityEngine;

namespace ShooterB
{
    public class DuckPartAnimator : MonoBehaviour
    {
        [SerializeField] private DuckPartLibrary partLibrary;

        public bool IsActive => isActive;

        private bool isActive;
        private DuckPartConfig config;
        private float animTime;

        private GameObject torsoObject;
        private GameObject leftWingPivot;
        private GameObject rightWingPivot;

        public bool TryInitialize(Constants.DuckType type, SpriteRenderer rootRenderer, int sortingOrder)
        {
            if (partLibrary == null || !partLibrary.TryGetConfig(type, out config))
                return false;

            rootRenderer.enabled = false;

            int sortingLayerID = rootRenderer.sortingLayerID;

            torsoObject = new GameObject("Torso");
            torsoObject.transform.SetParent(transform, false);
            SpriteRenderer torsoRenderer = torsoObject.AddComponent<SpriteRenderer>();
            torsoRenderer.sprite = config.torsoSprite;
            torsoRenderer.sortingLayerID = sortingLayerID;
            torsoRenderer.sortingOrder = sortingOrder;

            leftWingPivot = new GameObject("LeftWingPivot");
            leftWingPivot.transform.SetParent(transform, false);
            leftWingPivot.transform.localPosition = new Vector3(config.leftWingPivotOffset.x, config.leftWingPivotOffset.y, 0f);
            GameObject leftWingObj = new GameObject("LeftWing");
            leftWingObj.transform.SetParent(leftWingPivot.transform, false);
            SpriteRenderer leftWingRenderer = leftWingObj.AddComponent<SpriteRenderer>();
            leftWingRenderer.sprite = config.leftWingSprite;
            leftWingRenderer.sortingLayerID = sortingLayerID;
            leftWingRenderer.sortingOrder = sortingOrder + 1;

            rightWingPivot = new GameObject("RightWingPivot");
            rightWingPivot.transform.SetParent(transform, false);
            rightWingPivot.transform.localPosition = new Vector3(config.rightWingPivotOffset.x, config.rightWingPivotOffset.y, 0f);
            GameObject rightWingObj = new GameObject("RightWing");
            rightWingObj.transform.SetParent(rightWingPivot.transform, false);
            SpriteRenderer rightWingRenderer = rightWingObj.AddComponent<SpriteRenderer>();
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
            return config.torsoSprite;
        }

        public void Tick(float deltaTime)
        {
            if (!isActive) return;

            animTime += deltaTime;

            if (leftWingPivot != null)
            {
                float leftAngle = Mathf.Sin(animTime * config.flapSpeed * Mathf.PI * 2f) * config.flapAmplitude;
                leftWingPivot.transform.localRotation = Quaternion.Euler(0f, 0f, leftAngle);
            }

            if (rightWingPivot != null)
            {
                float rightAngle = Mathf.Sin((animTime + config.phaseOffset) * config.flapSpeed * Mathf.PI * 2f) * config.flapAmplitude;
                rightWingPivot.transform.localRotation = Quaternion.Euler(0f, 0f, rightAngle);
            }

            if (torsoObject != null && config.torsoBobAmount > 0f)
            {
                float bobY = Mathf.Sin(animTime * config.torsoBobSpeed * Mathf.PI * 2f) * config.torsoBobAmount;
                torsoObject.transform.localPosition = new Vector3(0f, bobY, 0f);
            }
        }

        public void PrepareForDeath(SpriteRenderer rootRenderer)
        {
            DestroyParts();
            if (rootRenderer != null)
                rootRenderer.enabled = true;
            isActive = false;
        }

        public void ResetState(SpriteRenderer rootRenderer)
        {
            DestroyParts();
            if (rootRenderer != null)
                rootRenderer.enabled = true;
            isActive = false;
        }

        private void DestroyParts()
        {
            if (torsoObject != null) Destroy(torsoObject);
            if (leftWingPivot != null) Destroy(leftWingPivot);
            if (rightWingPivot != null) Destroy(rightWingPivot);
            torsoObject = null;
            leftWingPivot = null;
            rightWingPivot = null;
        }
    }
}
