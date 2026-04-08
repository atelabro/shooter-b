using System;
using UnityEngine;

namespace ShooterB
{
    public class DailyLoginBonusManager : MonoBehaviour
    {
        private static DailyLoginBonusManager instance;

        public static DailyLoginBonusManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("DailyLoginBonusManager");
                    instance = go.AddComponent<DailyLoginBonusManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public bool HasClaimedToday
        {
            get
            {
                string lastClaim = PlayerPrefs.GetString(Constants.PREFS_DAILY_LOGIN_LAST_CLAIM, string.Empty);
                return string.Equals(lastClaim, GetTodayToken(), StringComparison.Ordinal);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Returns awarded coins, or -1 if the bonus was already claimed today.
        public int TryClaimDailyLoginBonus()
        {
            if (HasClaimedToday)
                return -1;

            int coins = UnityEngine.Random.Range(
                Constants.DAILY_LOGIN_COINS_MIN,
                Constants.DAILY_LOGIN_COINS_MAX + 1);

            PlayerPrefs.SetString(Constants.PREFS_DAILY_LOGIN_LAST_CLAIM, GetTodayToken());
            PlayerPrefs.Save();

            GameManager.Instance.AddCoins(coins);
            GameLog.Log($"[DailyLoginBonus] Claimed daily login bonus: +{coins} coins");
            return coins;
        }

        private static string GetTodayToken()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
}
