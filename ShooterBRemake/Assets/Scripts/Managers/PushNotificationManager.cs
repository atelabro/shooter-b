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
        private const int DebugNotificationId = 1002;

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
            _ = new AuthorizationRequest(
                AuthorizationOption.Alert |
                AuthorizationOption.Sound |
                AuthorizationOption.Badge,
                true);
#endif
        }

        public static void ScheduleDailyReminder()
        {
            DateTime target = DateTime.Today
                .AddDays(1)
                .AddHours(Constants.DAILY_LOGIN_NOTIFICATION_HOUR);
            ScheduleReminder(target, NotificationId);
        }

        public static void ScheduleDebugReminder(DateTime fireTime)
        {
            ScheduleReminder(fireTime, DebugNotificationId);
        }

        private static void ScheduleReminder(DateTime target, int notificationId)
        {
            string notificationTitle = LocalizationManager.Instance.Get(
                "daily_login.notification.title",
                "Daily Bonus Ready!");
            string notificationBody = LocalizationManager.Instance.Get(
                "daily_login.notification.body",
                "Your crate is waiting. Come crack it open!");

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelNotification(notificationId);

            AndroidNotification notification = new AndroidNotification
            {
                Title = notificationTitle,
                Text = notificationBody,
                FireTime = target,
                SmallIcon = "icon_0",
                LargeIcon = "icon_1"
            };

            AndroidNotificationCenter.SendNotificationWithExplicitID(
                notification,
                AndroidChannelId,
                notificationId);
#endif

#if UNITY_IOS
            iOSNotificationCenter.RemoveScheduledNotification(notificationId.ToString());

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
                Identifier = notificationId.ToString(),
                Title = notificationTitle,
                Body = notificationBody,
                ShowInForeground = false,
                Trigger = trigger
            };

            iOSNotificationCenter.ScheduleNotification(notification);
#endif
        }
    }
}
