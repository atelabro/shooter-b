# Task: Daily Login Bonus - Tappable Crate on Menu

## Summary
Add a tappable crate under the logo on the menu screen. If the player has not claimed today, the crate is visible with an idle bob animation. Tapping it plays a shake-then-burst animation, awards random 10-30 coins, and enqueues the existing reward popup. Once claimed the crate hides until the next calendar day. A push notification fires at 10 AM the next day.

---

## Step 1: Add Unity Mobile Notifications Package

In `ShooterBRemake/Packages/manifest.json`, add to the `dependencies` block:
```json
"com.unity.mobile.notifications": "2.3.2"
```

---

## Step 2: Add Constants

File: `ShooterBRemake/Assets/Scripts/Utils/Constants.cs`

Add inside the `Constants` class:
```csharp
public const string PREFS_DAILY_LOGIN_LAST_CLAIM = "DailyLogin_LastClaimDate";
public const int DAILY_LOGIN_COINS_MIN = 10;
public const int DAILY_LOGIN_COINS_MAX = 30;
public const int DAILY_LOGIN_NOTIFICATION_HOUR = 10;
```

---

## Step 3: Create DailyLoginBonusManager

Create: `ShooterBRemake/Assets/Scripts/Managers/DailyLoginBonusManager.cs`

```csharp
using UnityEngine;

namespace ShooterB
{
    public class DailyLoginBonusManager : MonoBehaviour
    {
        public static DailyLoginBonusManager Instance { get; private set; }

        public bool HasClaimedToday
        {
            get
            {
                string last = PlayerPrefs.GetString(Constants.PREFS_DAILY_LOGIN_LAST_CLAIM, string.Empty);
                return last == System.DateTime.Now.ToString("yyyy-MM-dd");
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Returns coins awarded, or -1 if already claimed today.
        public int TryClaimDailyLoginBonus()
        {
            if (HasClaimedToday)
                return -1;

            int coins = Random.Range(Constants.DAILY_LOGIN_COINS_MIN, Constants.DAILY_LOGIN_COINS_MAX + 1);
            PlayerPrefs.SetString(Constants.PREFS_DAILY_LOGIN_LAST_CLAIM, System.DateTime.Now.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
            GameManager.Instance.AddCoins(coins);
            return coins;
        }
    }
}
```

Add `DailyLoginBonusManager` as a component to the Managers GameObject in the scene (the same object that holds GameManager and other DontDestroyOnLoad managers).

---

## Step 4: Create PushNotificationManager

Create: `ShooterBRemake/Assets/Scripts/Managers/PushNotificationManager.cs`

```csharp
using System;
using UnityEngine;

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
            var channel = new AndroidNotificationChannel
            {
                Id = AndroidChannelId,
                Name = "Daily Login Bonus",
                Importance = Importance.Default,
                Description = "Reminds you to collect your daily coin bonus."
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif

#if UNITY_IOS
            AuthorizationRequest.StartAuthorization(AuthorizationOption.Alert | AuthorizationOption.Sound | AuthorizationOption.Badge);
#endif
        }

        public static void ScheduleDailyReminder()
        {
            DateTime target = DateTime.Today.AddDays(1).AddHours(Constants.DAILY_LOGIN_NOTIFICATION_HOUR);

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelNotification(NotificationId);

            var notification = new AndroidNotification
            {
                Title = NotificationTitle,
                Text = NotificationBody,
                FireTime = target,
                SmallIcon = "icon_0",
                LargeIcon = "icon_1"
            };

            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, AndroidChannelId, NotificationId);
#endif

#if UNITY_IOS
            iOSNotificationCenter.RemoveScheduledNotification(NotificationId.ToString());

            var trigger = new iOSNotificationCalendarTrigger
            {
                Year  = target.Year,
                Month = target.Month,
                Day   = target.Day,
                Hour  = target.Hour,
                Minute = 0,
                Second = 0,
                Repeats = false
            };

            var notification = new iOSNotification
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
```

---

## Step 5: Create DailyLoginCrateController

Create: `ShooterBRemake/Assets/Scripts/UI/DailyLoginCrateController.cs`

```csharp
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShooterB
{
    public class DailyLoginCrateController : MonoBehaviour
    {
        [Header("References")]
        public Button crateButton;
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI hintText;

        [Header("Popup")]
        public GameObject rewardPopupPrefab;
        public Transform rewardPopupContainer;

        private RectTransform _rectTransform;
        private Vector2 _basePosition;
        private Coroutine _bobCoroutine;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            _basePosition = _rectTransform.anchoredPosition;

            if (DailyLoginBonusManager.Instance == null || DailyLoginBonusManager.Instance.HasClaimedToday)
            {
                gameObject.SetActive(false);
                return;
            }

            crateButton.onClick.AddListener(OnCrateTapped);
            _bobCoroutine = StartCoroutine(IdleBob());
        }

        private void OnCrateTapped()
        {
            crateButton.interactable = false;

            if (_bobCoroutine != null)
                StopCoroutine(_bobCoroutine);

            _rectTransform.anchoredPosition = _basePosition;

            int coins = DailyLoginBonusManager.Instance.TryClaimDailyLoginBonus();
            if (coins == -1)
            {
                gameObject.SetActive(false);
                return;
            }

            PushNotificationManager.ScheduleDailyReminder();
            StartCoroutine(PlayClaimAnimation(coins));
        }

        private IEnumerator PlayClaimAnimation(int coins)
        {
            // Phase 1: shake
            float elapsed = 0f;
            const float shakeDuration = 0.4f;
            const float shakeMagnitude = 12f;

            while (elapsed < shakeDuration)
            {
                float x = Mathf.Sin(elapsed * 60f) * shakeMagnitude;
                _rectTransform.anchoredPosition = _basePosition + new Vector2(x, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _rectTransform.anchoredPosition = _basePosition;

            // Phase 2: scale up and fade out
            elapsed = 0f;
            const float burstDuration = 0.35f;
            Vector3 startScale = Vector3.one;
            Vector3 endScale = Vector3.one * 1.4f;

            while (elapsed < burstDuration)
            {
                float t = elapsed / burstDuration;
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 3: show reward popup
            RewardPopupQueueController.Enqueue(new RewardPopupRequest
            {
                header = "Daily Bonus",
                title = "Come back tomorrow!",
                coins = coins,
                source = "DailyLoginBonus",
                logPrefix = "[DAILY LOGIN]",
                prefab = rewardPopupPrefab,
                container = rewardPopupContainer,
                showActionButton = false,
                lifetime = 4f
            });

            gameObject.SetActive(false);
        }

        private IEnumerator IdleBob()
        {
            const float bobAmplitude = 4f;
            const float bobSpeed = 1.5f;

            while (true)
            {
                float y = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
                _rectTransform.anchoredPosition = _basePosition + new Vector2(0f, y);
                yield return null;
            }
        }
    }
}
```

---

## Step 6: Modify GameManager

File: `ShooterBRemake/Assets/Scripts/Managers/GameManager.cs`

In `Awake()`, after existing initialization, add:
```csharp
PushNotificationManager.Initialize();
```

---

## Step 7: Scene Setup in Unity Editor

In the MenuScene Canvas, add the following UI hierarchy positioned below the logo:

```
DailyCrateRoot          (RectTransform, anchored center, Y positioned below logo)
  DailyLoginCrateController component attached here
  CanvasGroup component attached here
  |- LabelText          (TextMeshProUGUI) text = "DAILY BONUS", centered
  |- CrateButton        (Button + Image, 120x120 px, color #F5C518 as placeholder)
  |- HintText           (TextMeshProUGUI) text = "Tap to claim!", centered, smaller font
```

Wire in Inspector on DailyLoginCrateController:
- `crateButton` -> CrateButton
- `canvasGroup` -> the CanvasGroup on DailyCrateRoot
- `labelText` -> LabelText
- `hintText` -> HintText
- `rewardPopupPrefab` -> existing reward popup prefab used by RewardPopupQueueController
- `rewardPopupContainer` -> existing popup container in scene

Swap the placeholder color/sprite for real crate art when available.

---

## Integration Flow

```
App open
  -> GameManager.Awake() -> PushNotificationManager.Initialize()

MenuScene loads -> DailyLoginCrateController.Start()
  HasClaimedToday? YES -> crate hidden
  HasClaimedToday? NO  -> crate visible + idle bob

Player taps crate
  -> bob stops
  -> shake animation (0.4s)
  -> TryClaimDailyLoginBonus() -> Random 10-30 coins -> GameManager.AddCoins()
  -> scale-up + fade-out (0.35s)
  -> RewardPopupQueueController.Enqueue(coins)
  -> PushNotificationManager.ScheduleDailyReminder()
  -> crate deactivated
```

---

## Verification Checklist

- [ ] Crate visible on first daily open, idle bob playing
- [ ] Tapping plays shake then fade animation
- [ ] Coins in popup match coins added to CurrencyHeaderUI (10-30 range)
- [ ] Reopening same day: crate hidden, no double award
- [ ] New calendar day (past midnight): crate reappears
- [ ] Android device: notification fires at 10 AM next day
- [ ] iOS device: notification fires at 10 AM next day
