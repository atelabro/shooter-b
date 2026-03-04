using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ShooterB
{
    public class RewardPopupQueueController : MonoBehaviour
    {
        public struct RewardPopupRequest
        {
            public string header;
            public string title;
            public int coins;
            public bool isDebug;
            public string source;
            public string logPrefix;
            public GameObject prefab;
            public Transform container;
            public float lifetime;
        }

        private readonly Queue<RewardPopupRequest> pending = new Queue<RewardPopupRequest>();
        private Coroutine processingCoroutine;
        private GameObject activePopup;
        private bool isProcessing;

        public void Enqueue(RewardPopupRequest request)
        {
            pending.Enqueue(request);
            TryStartProcessing();
        }

        private void OnEnable()
        {
            TryStartProcessing();
        }

        private void OnDisable()
        {
            CleanupQueueAndActivePopup();
        }

        private void OnDestroy()
        {
            CleanupQueueAndActivePopup();
        }

        private void TryStartProcessing()
        {
            if (!isActiveAndEnabled || isProcessing || pending.Count == 0)
                return;

            processingCoroutine = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            isProcessing = true;

            while (pending.Count > 0)
            {
                RewardPopupRequest request = pending.Dequeue();
                yield return ShowRequest(request);
            }

            isProcessing = false;
            processingCoroutine = null;
        }

        private IEnumerator ShowRequest(RewardPopupRequest request)
        {
            if (request.prefab == null)
            {
                Debug.LogWarning($"{request.logPrefix} achievementPopupPrefab is missing.");
                yield break;
            }

            Transform parent = ResolveParent(request.container);
            activePopup = Instantiate(request.prefab, parent);

            AchievementUnlockPopupController popupController = activePopup.GetComponent<AchievementUnlockPopupController>();
            if (popupController != null)
            {
                popupController.ConfigureCustom(request.header, request.title, request.coins);
            }
            else
            {
                TextMeshProUGUI popupText = activePopup.GetComponent<TextMeshProUGUI>();
                if (popupText != null)
                    popupText.text = $"{request.header}\n{request.title}\n+{Mathf.Max(0, request.coins)} COINS";
            }

            RectTransform popupRect = activePopup.GetComponent<RectTransform>();
            if (popupRect != null)
            {
                popupRect.anchoredPosition = new Vector2(0f, 220f);
                popupRect.SetAsLastSibling();
            }

            Debug.Log($"{request.logPrefix} {request.source} popup shown ({(request.isDebug ? "DEBUG" : "LIVE")}): {request.title}, +{Mathf.Max(0, request.coins)} coins");

            float lifetime = Mathf.Max(0.1f, request.lifetime);
            yield return new WaitForSecondsRealtime(lifetime);

            if (activePopup != null)
            {
                Destroy(activePopup);
                activePopup = null;
            }
        }

        private Transform ResolveParent(Transform requestedParent)
        {
            if (requestedParent != null)
                return requestedParent;

            Transform parent = transform;
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null)
                return canvas.transform;

            return parent;
        }

        private void CleanupQueueAndActivePopup()
        {
            if (processingCoroutine != null)
            {
                StopCoroutine(processingCoroutine);
                processingCoroutine = null;
            }

            isProcessing = false;
            pending.Clear();

            if (activePopup != null)
            {
                Destroy(activePopup);
                activePopup = null;
            }
        }
    }
}
