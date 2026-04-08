using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class DailyLoginCrateController : MonoBehaviour
    {
        private const string EarnedBonusClipResourcePath = "Audio/earned_bonus";
        private const string CoinClipResourcePath = "Audio/coin";

        [Header("References")]
        public Button crateButton;
        public CanvasGroup canvasGroup;
        public Animator crateAnimator;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI hintText;

        [Header("Popup")]
        public RewardPopupQueueController rewardPopupQueue;
        public GameObject rewardPopupPrefab;
        public Transform rewardPopupContainer;
        public float rewardPopupLifetime = 4f;

        [Header("Animation")]
        public float bobAmplitude = 4f;
        public float bobSpeed = 1.5f;
        public float shakeDuration = 0.25f;
        public float shakeMagnitude = 10f;
        public float shakeFrequency = 40f;
        public string openTriggerName = "Open";
        public string openStateName = "Crate";
        public float openAnimationDuration = 0.6f;
        public float postOpenHoldDuration = 0.75f;
        public float hideFadeDuration = 0.3f;

        [Header("Debug")]
        public bool debugResetClaimOnStart;

        private RectTransform rectTransform;
        private Vector2 basePosition;
        private Coroutine bobCoroutine;
        private AudioSource rewardAudioSource;
        private AudioClip earnedBonusClip;
        private AudioClip coinClip;

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugResetClaimOnStart)
            {
                PlayerPrefs.DeleteKey(Constants.PREFS_DAILY_LOGIN_LAST_CLAIM);
                PlayerPrefs.Save();
            }
#endif

            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
                basePosition = rectTransform.anchoredPosition;

            if (crateButton == null)
                crateButton = GetComponentInChildren<Button>(true);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (crateAnimator == null && crateButton != null)
                crateAnimator = crateButton.GetComponent<Animator>();

            ResolveTextReferences();
            RefreshLocalizedTexts();

            if (DailyLoginBonusManager.Instance.HasClaimedToday)
            {
                gameObject.SetActive(false);
                return;
            }

            if (crateButton == null)
            {
                GameLog.Warning("[DailyLoginCrate] crateButton is missing.");
                return;
            }

            ResolveRewardPopupQueue();
            crateButton.onClick.RemoveListener(OnCrateTapped);
            crateButton.onClick.AddListener(OnCrateTapped);
            bobCoroutine = StartCoroutine(IdleBob());
        }

        private void OnDestroy()
        {
            if (crateButton != null)
                crateButton.onClick.RemoveListener(OnCrateTapped);
        }

        private void OnCrateTapped()
        {
            UIClickSoundManager.Instance.PlayClick();

            crateButton.interactable = false;

            if (bobCoroutine != null)
            {
                StopCoroutine(bobCoroutine);
                bobCoroutine = null;
            }

            int coins = DailyLoginBonusManager.Instance.TryClaimDailyLoginBonus();
            if (coins < 0)
            {
                gameObject.SetActive(false);
                return;
            }

            PushNotificationManager.ScheduleDailyReminder();
            StartCoroutine(PlayClaimAnimation(coins));
        }

        private IEnumerator PlayClaimAnimation(int coins)
        {
            yield return ShakeAtCenter();

            if (crateAnimator != null)
            {
                if (!string.IsNullOrWhiteSpace(openTriggerName))
                    crateAnimator.ResetTrigger(openTriggerName);

                if (!string.IsNullOrWhiteSpace(openStateName))
                    crateAnimator.Play(openStateName, 0, 0f);
                else if (!string.IsNullOrWhiteSpace(openTriggerName))
                    crateAnimator.SetTrigger(openTriggerName);
            }

            if (openAnimationDuration > 0f)
                yield return new WaitForSeconds(openAnimationDuration);

            PlayRewardClip(ref earnedBonusClip, EarnedBonusClipResourcePath);

            if (postOpenHoldDuration > 0f)
                yield return new WaitForSeconds(postOpenHoldDuration);

            EnqueueRewardPopup(coins);
            yield return FadeOutAndHide();
        }

        private IEnumerator IdleBob()
        {
            while (true)
            {
                if (rectTransform != null)
                {
                    float y = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
                    rectTransform.anchoredPosition = basePosition + new Vector2(0f, y);
                }

                yield return null;
            }
        }

        private void EnqueueRewardPopup(int coins)
        {
            if (rewardPopupPrefab == null)
            {
                GameLog.Warning("[DailyLoginCrate] rewardPopupPrefab is missing.");
                return;
            }

            ResolveRewardPopupQueue();
            if (rewardPopupQueue == null)
            {
                GameLog.Warning("[DailyLoginCrate] rewardPopupQueue could not be resolved.");
                return;
            }

            RewardPopupQueueController.RewardPopupRequest request = new RewardPopupQueueController.RewardPopupRequest
            {
                header = LocalizationManager.Instance.Get("daily_login.popup.header", "DAILY BONUS"),
                title = LocalizationManager.Instance.Get("daily_login.popup.title", "Come back tomorrow!"),
                coins = coins,
                isDebug = false,
                source = "DailyLoginBonus",
                logPrefix = "[DailyLoginCrate]",
                prefab = rewardPopupPrefab,
                container = rewardPopupContainer != null ? rewardPopupContainer : transform,
                anchoredPosition = Vector2.zero,
                lifetime = rewardPopupLifetime,
                showActionButton = false
            };

            PlayRewardClip(ref coinClip, CoinClipResourcePath);
            rewardPopupQueue.Enqueue(request);
        }

        private void ResolveRewardPopupQueue()
        {
            if (rewardPopupQueue != null)
                return;

            rewardPopupQueue = GetComponent<RewardPopupQueueController>();
            if (rewardPopupQueue != null)
                return;

            rewardPopupQueue = FindObjectOfType<RewardPopupQueueController>(true);
        }

        private void ResolveTextReferences()
        {
            if (labelText == null)
            {
                Transform labelTransform = transform.Find("LabelText");
                if (labelTransform != null)
                    labelText = labelTransform.GetComponent<TextMeshProUGUI>();
            }

            if (hintText == null)
            {
                Transform hintTransform = transform.Find("HintText");
                if (hintTransform != null)
                    hintText = hintTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        private void RefreshLocalizedTexts()
        {
            if (labelText != null)
                labelText.text = LocalizationManager.Instance.Get("daily_login.label", "DAILY BONUS");

            if (hintText != null)
                hintText.text = LocalizationManager.Instance.Get("daily_login.hint", "Tap to claim!");
        }

        private IEnumerator ShakeAtCenter()
        {
            if (rectTransform == null || shakeDuration <= 0f || shakeMagnitude <= 0f)
                yield break;

            Vector2 anchoredCenter = rectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = Mathf.Sin(elapsed * shakeFrequency) * shakeMagnitude;
                rectTransform.anchoredPosition = anchoredCenter + new Vector2(x, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            rectTransform.anchoredPosition = anchoredCenter;
        }

        private IEnumerator FadeOutAndHide()
        {
            if (canvasGroup == null || hideFadeDuration <= 0f)
            {
                gameObject.SetActive(false);
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < hideFadeDuration)
            {
                float t = hideFadeDuration <= 0f ? 1f : elapsed / hideFadeDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void PlayRewardClip(ref AudioClip clipField, string resourcePath)
        {
            EnsureRewardAudioReady();
            if (rewardAudioSource == null)
                return;

            if (clipField == null)
                clipField = Resources.Load<AudioClip>(resourcePath);

            if (clipField == null)
                return;

            rewardAudioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
            rewardAudioSource.PlayOneShot(clipField);
        }

        private void EnsureRewardAudioReady()
        {
            if (rewardAudioSource != null)
                return;

            rewardAudioSource = gameObject.GetComponent<AudioSource>();
            if (rewardAudioSource == null)
                rewardAudioSource = gameObject.AddComponent<AudioSource>();

            rewardAudioSource.playOnAwake = false;
            rewardAudioSource.loop = false;
            rewardAudioSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
        }
    }
}
