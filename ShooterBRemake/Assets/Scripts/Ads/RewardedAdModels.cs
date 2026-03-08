using System;

namespace ShooterB
{
    public enum RewardedAdPlacement
    {
        CampaignStartPlusLives,
        CampaignStartPlusBullets,
        DailyAwardsSetBonus,
        CityFirstCompletion
    }

    public enum RewardedAdResult
    {
        Completed,
        Canceled,
        Failed,
        Unavailable
    }

    public interface IRewardedAdProvider
    {
        bool IsReady(RewardedAdPlacement placement);
        void Show(RewardedAdPlacement placement, string source, Action<RewardedAdResult> onCompleted);
    }

    public interface IRewardedAdService
    {
        bool IsReady(RewardedAdPlacement placement);
        void ShowRewardedAd(RewardedAdPlacement placement, string source, Action<RewardedAdResult> onCompleted);
    }
}
