using System;
using System.Collections.Generic;
using UnityEngine;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using GoogleMobileAds.Api;
#endif

namespace ShooterB
{
    public class RewardedAdService : MonoBehaviour, IRewardedAdService
    {
        public enum ProviderMode
        {
            Mock,
            AdMobMediation
        }

        public enum MockOutcomeMode
        {
            AlwaysCompleted,
            AlwaysCanceled,
            AlwaysFailed,
            AlwaysUnavailable,
            WeightedRandom
        }

        private static RewardedAdService instance;
        private static bool instanceSourceLogged;
        public static RewardedAdService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<RewardedAdService>();
                    if (instance != null)
                    {
                        instance.LogInstanceSourceOnce("scene");
                    }
                    else
                    {
                        GameObject go = new GameObject("RewardedAdService");
                        instance = go.AddComponent<RewardedAdService>();
                        DontDestroyOnLoad(go);
                        instance.LogInstanceSourceOnce("auto-created");
                    }
                }

                return instance;
            }
        }

        [Header("Provider")]
        [SerializeField] private ProviderMode providerMode = ProviderMode.Mock;

        [Header("AdMob Unit IDs")]
        [SerializeField] private bool useTestAdUnitIds = true;
        [SerializeField] private string androidCampaignStartPlusLivesUnitId = string.Empty;
        [SerializeField] private string androidCampaignStartPlusBulletsUnitId = string.Empty;
        [SerializeField] private string androidDailyAwardsSetBonusUnitId = string.Empty;
        [SerializeField] private string androidCityFirstCompletionUnitId = string.Empty;
        [SerializeField] private string iosCampaignStartPlusLivesUnitId = string.Empty;
        [SerializeField] private string iosCampaignStartPlusBulletsUnitId = string.Empty;
        [SerializeField] private string iosDailyAwardsSetBonusUnitId = string.Empty;
        [SerializeField] private string iosCityFirstCompletionUnitId = string.Empty;

        [Header("Mock Provider")]
        [SerializeField] private MockOutcomeMode mockOutcomeMode = MockOutcomeMode.AlwaysCompleted;
        [Range(0f, 1f)] [SerializeField] private float weightedCompletionChance = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float weightedCanceledChance = 0.1f;
        [Range(0f, 1f)] [SerializeField] private float weightedFailedChance = 0.1f;

        private IRewardedAdProvider provider;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LogInstanceSourceOnce("awake");
            provider = CreateProvider();
        }

        public bool IsReady(RewardedAdPlacement placement)
        {
            EnsureProvider();
            return provider.IsReady(placement);
        }

        public void ShowRewardedAd(RewardedAdPlacement placement, string source, Action<RewardedAdResult> onCompleted)
        {
            EnsureProvider();
            provider.Show(placement, source, result =>
            {
                Debug.Log($"[RewardedAdService] placement={placement}, source={source}, result={result}");
                onCompleted?.Invoke(result);
            });
        }

        public string GetResultMessage(RewardedAdResult result)
        {
            switch (result)
            {
                case RewardedAdResult.Completed:
                    return LocalizationManager.Instance.Get("ads.status.completed", "Reward granted.");
                case RewardedAdResult.Canceled:
                    return LocalizationManager.Instance.Get("ads.status.canceled", "Ad canceled.");
                case RewardedAdResult.Unavailable:
                    return LocalizationManager.Instance.Get("ads.status.unavailable", "Ad unavailable. Try again.");
                case RewardedAdResult.Failed:
                default:
                    return LocalizationManager.Instance.Get("ads.status.failed", "Ad failed. Try again.");
            }
        }

        private void EnsureProvider()
        {
            if (provider == null)
                provider = CreateProvider();
        }

        private IRewardedAdProvider CreateProvider()
        {
            if (providerMode == ProviderMode.AdMobMediation)
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                return new AdMobRewardedAdProvider(this);
#else
                Debug.LogWarning("[RewardedAdService] AdMob provider selected, but current platform/build does not support runtime AdMob SDK calls. Falling back to Mock provider.");
#endif
            }

            return new MockRewardedAdProvider(this);
        }

        private void LogInstanceSourceOnce(string source)
        {
            if (instanceSourceLogged)
                return;

            instanceSourceLogged = true;
            Debug.Log($"[RewardedAdService] Singleton source={source}, gameObject={gameObject.name}");
        }

        private string ResolveAdUnitId(RewardedAdPlacement placement)
        {
            if (useTestAdUnitIds)
            {
#if UNITY_ANDROID
                return "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IOS
                return "ca-app-pub-3940256099942544/1712485313";
#else
                return string.Empty;
#endif
            }

#if UNITY_ANDROID
            switch (placement)
            {
                case RewardedAdPlacement.CampaignStartPlusLives:
                    return androidCampaignStartPlusLivesUnitId;
                case RewardedAdPlacement.CampaignStartPlusBullets:
                    return androidCampaignStartPlusBulletsUnitId;
                case RewardedAdPlacement.DailyAwardsSetBonus:
                    return androidDailyAwardsSetBonusUnitId;
                case RewardedAdPlacement.CityFirstCompletion:
                    return androidCityFirstCompletionUnitId;
                default:
                    return string.Empty;
            }
#elif UNITY_IOS
            switch (placement)
            {
                case RewardedAdPlacement.CampaignStartPlusLives:
                    return iosCampaignStartPlusLivesUnitId;
                case RewardedAdPlacement.CampaignStartPlusBullets:
                    return iosCampaignStartPlusBulletsUnitId;
                case RewardedAdPlacement.DailyAwardsSetBonus:
                    return iosDailyAwardsSetBonusUnitId;
                case RewardedAdPlacement.CityFirstCompletion:
                    return iosCityFirstCompletionUnitId;
                default:
                    return string.Empty;
            }
#else
            return string.Empty;
#endif
        }

        private sealed class MockRewardedAdProvider : IRewardedAdProvider
        {
            private readonly RewardedAdService service;

            public MockRewardedAdProvider(RewardedAdService service)
            {
                this.service = service;
            }

            public bool IsReady(RewardedAdPlacement placement)
            {
                return service.mockOutcomeMode != MockOutcomeMode.AlwaysUnavailable;
            }

            public void Show(RewardedAdPlacement placement, string source, Action<RewardedAdResult> onCompleted)
            {
                RewardedAdResult result;
                switch (service.mockOutcomeMode)
                {
                    case MockOutcomeMode.AlwaysCompleted:
                        result = RewardedAdResult.Completed;
                        break;
                    case MockOutcomeMode.AlwaysCanceled:
                        result = RewardedAdResult.Canceled;
                        break;
                    case MockOutcomeMode.AlwaysFailed:
                        result = RewardedAdResult.Failed;
                        break;
                    case MockOutcomeMode.AlwaysUnavailable:
                        result = RewardedAdResult.Unavailable;
                        break;
                    case MockOutcomeMode.WeightedRandom:
                    default:
                        result = GetWeightedRandomResult();
                        break;
                }

                onCompleted?.Invoke(result);
            }

            private RewardedAdResult GetWeightedRandomResult()
            {
                float completeWeight = Mathf.Max(0f, service.weightedCompletionChance);
                float canceledWeight = Mathf.Max(0f, service.weightedCanceledChance);
                float failedWeight = Mathf.Max(0f, service.weightedFailedChance);
                float unavailableWeight = Mathf.Max(0f, 1f - (completeWeight + canceledWeight + failedWeight));

                float total = completeWeight + canceledWeight + failedWeight + unavailableWeight;
                if (total <= 0f)
                    return RewardedAdResult.Unavailable;

                float roll = UnityEngine.Random.value * total;
                if (roll < completeWeight)
                    return RewardedAdResult.Completed;

                roll -= completeWeight;
                if (roll < canceledWeight)
                    return RewardedAdResult.Canceled;

                roll -= canceledWeight;
                if (roll < failedWeight)
                    return RewardedAdResult.Failed;

                return RewardedAdResult.Unavailable;
            }
        }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private sealed class AdMobRewardedAdProvider : IRewardedAdProvider
        {
            private readonly RewardedAdService service;
            private readonly Dictionary<RewardedAdPlacement, RewardedAd> adsByPlacement = new Dictionary<RewardedAdPlacement, RewardedAd>();
            private readonly HashSet<RewardedAdPlacement> loadingPlacements = new HashSet<RewardedAdPlacement>();

            private bool initialized;

            public AdMobRewardedAdProvider(RewardedAdService service)
            {
                this.service = service;
                Initialize();
            }

            public bool IsReady(RewardedAdPlacement placement)
            {
                if (!initialized)
                    return false;

                if (!adsByPlacement.TryGetValue(placement, out RewardedAd ad) || ad == null)
                    return false;

                return ad.CanShowAd();
            }

            public void Show(RewardedAdPlacement placement, string source, Action<RewardedAdResult> onCompleted)
            {
                if (!initialized)
                {
                    onCompleted?.Invoke(RewardedAdResult.Unavailable);
                    return;
                }

                if (!adsByPlacement.TryGetValue(placement, out RewardedAd ad) || ad == null || !ad.CanShowAd())
                {
                    LoadPlacement(placement);
                    onCompleted?.Invoke(RewardedAdResult.Unavailable);
                    return;
                }

                bool rewardEarned = false;
                bool callbackSent = false;

                Action<RewardedAdResult> complete = result =>
                {
                    if (callbackSent)
                        return;

                    callbackSent = true;
                    onCompleted?.Invoke(result);
                };

                Action onClosed = null;
                Action<AdError> onFailed = null;

                onClosed = () =>
                {
                    ad.OnAdFullScreenContentClosed -= onClosed;
                    ad.OnAdFullScreenContentFailed -= onFailed;
                    ad.Destroy();
                    complete(rewardEarned ? RewardedAdResult.Completed : RewardedAdResult.Canceled);
                };

                onFailed = error =>
                {
                    ad.OnAdFullScreenContentClosed -= onClosed;
                    ad.OnAdFullScreenContentFailed -= onFailed;
                    ad.Destroy();
                    complete(RewardedAdResult.Failed);
                };

                ad.OnAdFullScreenContentClosed += onClosed;
                ad.OnAdFullScreenContentFailed += onFailed;

                adsByPlacement[placement] = null;
                LoadPlacement(placement);

                ad.Show(reward =>
                {
                    rewardEarned = true;
                });
            }

            private void Initialize()
            {
                if (initialized)
                    return;

                MobileAds.Initialize(_ =>
                {
                    initialized = true;
                    LoadAllPlacements();
                });
            }

            private void LoadAllPlacements()
            {
                LoadPlacement(RewardedAdPlacement.CampaignStartPlusLives);
                LoadPlacement(RewardedAdPlacement.CampaignStartPlusBullets);
                LoadPlacement(RewardedAdPlacement.DailyAwardsSetBonus);
                LoadPlacement(RewardedAdPlacement.CityFirstCompletion);
            }

            private void LoadPlacement(RewardedAdPlacement placement)
            {
                if (!initialized || loadingPlacements.Contains(placement))
                    return;

                string adUnitId = service.ResolveAdUnitId(placement);
                if (string.IsNullOrWhiteSpace(adUnitId))
                    return;

                loadingPlacements.Add(placement);
                AdRequest request = new AdRequest();
                RewardedAd.Load(adUnitId, request, (RewardedAd loadedAd, LoadAdError loadError) =>
                {
                    loadingPlacements.Remove(placement);

                    if (loadError != null || loadedAd == null)
                    {
                        Debug.LogWarning($"[RewardedAdService] Failed to load rewarded ad for {placement}. error={loadError}");
                        return;
                    }

                    if (adsByPlacement.TryGetValue(placement, out RewardedAd existingAd) && existingAd != null)
                        existingAd.Destroy();

                    adsByPlacement[placement] = loadedAd;
                });
            }
        }
#endif
    }
}
