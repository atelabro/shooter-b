using System;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace ShooterB
{
    public static class PushNotificationManager
    {
        private const string AndroidChannelId = "daily_login_bonus";
        private const int NotificationId = 1001;
        private const string NotificationTitle = "Daily Bonus Ready!";
        private const string NotificationBody = "Your crate is waiting. Come crack it open!";

        public static void Initialize()
        {
#if UNITY_ANDROID
            AndroidNotificationChannel channel = new AndroidNotificationChannel
            {
                Id = AndroidChannelId,
                Name = "Daily Login Bonus",
                Importance = Importance.Default,
                Description = "Reminds you to collect your daily coin bonus."
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif

#if UNITY_IOS
            AuthorizationRequest.StartAuthorization(
                AuthorizationOption.Alert |
                AuthorizationOption.Sound |
                AuthorizationOption.Badge);
#endif
        }

        public static void ScheduleDailyReminder()
        {
            DateTime target = DateTime.Today
                .AddDays(1)
                .AddHours(Constants.DAILY_LOGIN_NOTIFICATION_HOUR);

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelNotification(NotificationId);

            AndroidNotification notification = new AndroidNotification
            {
                Title = NotificationTitle,
                Text = NotificationBody,
                FireTime = target,
                SmallIcon = "icon_0",
                LargeIcon = "icon_1"
            };

            AndroidNotificationCenter.SendNotificationWithExplicitID(
                notification,
                AndroidChannelId,
                NotificationId);
#endif

#if UNITY_IOS
            iOSNotificationCenter.RemoveScheduledNotification(NotificationId.ToString());

            iOSNotificationCalendarTrigger trigger = new iOSNotificationCalendarTrigger
            {
                Year = target.Year,
                Month = target.Month,
                Day = target.Day,
                Hour = target.Hour,
                Minute = 0,
                Second = 0,
                Repeats = false
            };

            iOSNotification notification = new iOSNotification
            {
                Identifier = NotificationId.ToString(),
                Title = NotificationTitle,
                Body = NotificationBody,
                ShowInForeground = false,
                Trigger = trigger
            };

            iOSNotificationCenter.ScheduleNotification(notification);
#endif
        }
    }
}
