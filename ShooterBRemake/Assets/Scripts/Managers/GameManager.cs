using UnityEngine;
using System;

namespace ShooterB
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public long Score { get; private set; }
        public int Multiplier { get; private set; }
        public long BirdCount { get; private set; }
        public int BirdsKilled { get; private set; }
        public int Difficulty { get; private set; }
        public long HighScore { get; private set; }
        public int Lives { get; private set; }
        public int Coins { get; private set; }
        public Constants.GameMode CurrentGameMode { get; private set; }
        public Constants.WeaponType SelectedWeaponType { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool ArcadeVeryHardMode { get; private set; }
        public int ConsecutiveFailedRuns { get; private set; }

        public event Action<long> OnScoreChanged;
        public event Action<int> OnMultiplierChanged;
        public event Action<int> OnLivesChanged;
        public event Action<int> OnDifficultyChanged;
        public event Action<int> OnCoinsChanged;
        public event Action<bool> OnPauseStateChanged;
        public event Action OnGameOver;
        public event Action<Constants.MultiKillType, int, Vector3> OnComboKill;
        public event Action<Constants.MultiKillType, Constants.WeaponType, int, Vector3> OnComboKillDetailed;
        public event Action<int> OnBirdsKilledChanged;
        public event Action<Constants.DuckType, Constants.WeaponType> OnBirdKilled;
        public event Action OnBirdPassed;
        public event Action<Constants.WeaponType> OnSelectedWeaponChanged;

        private int birdsUntilNextDifficulty;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCoins();
            LoadSelectedWeapon();
            _ = AchievementManager.Instance;
            _ = DailyAwardsManager.Instance;
        }

        public void InitializeGame(Constants.GameMode mode, int startingDifficulty = Constants.INITIAL_DIFFICULTY)
        {
            CurrentGameMode = mode;
            Score = 0;
            BirdCount = 0;
            BirdsKilled = 0;
            Difficulty = mode == Constants.GameMode.Arcade && ArcadeVeryHardMode ? 5 : startingDifficulty;
            Lives = Constants.INITIAL_LIVES;
            IsPaused = false;
            IsGameOver = false;

            CalculateMultiplier();
            LoadHighScore();
            UpdateBirdsUntilNextDifficulty();

            OnScoreChanged?.Invoke(Score);
            OnMultiplierChanged?.Invoke(Multiplier);
            OnLivesChanged?.Invoke(Lives);
            OnDifficultyChanged?.Invoke(Difficulty);

            Debug.Log($"Game initialized - Mode: {mode}, Difficulty: {Difficulty}, Lives: {Lives}");
        }

        public void BirdCreated()
        {
            BirdCount++;
            birdsUntilNextDifficulty--;

            if (BirdCount % Constants.BONUS_LIFE_BIRD_COUNT == 0)
            {
                PlusLife();
                Debug.Log($"Bonus life awarded at bird count: {BirdCount}");
            }

            if (birdsUntilNextDifficulty <= 0 && Difficulty < Constants.MAX_DIFFICULTY)
            {
                Difficulty++;
                CalculateMultiplier();
                UpdateBirdsUntilNextDifficulty();
                OnDifficultyChanged?.Invoke(Difficulty);
                OnMultiplierChanged?.Invoke(Multiplier);
                Debug.Log($"Difficulty increased to {Difficulty}, Multiplier: {Multiplier}");
            }
        }

        public void BirdKilled(Constants.DuckType duckType)
        {
            BirdKilled(duckType, SelectedWeaponType);
        }

        public void BirdKilled(Constants.DuckType duckType, Constants.WeaponType weaponType)
        {
            int basePoints = Constants.DuckPoints.GetPoints(duckType);
            int pointsEarned = basePoints * Multiplier;

            AddPoints(pointsEarned);

            BirdsKilled++;
            OnBirdsKilledChanged?.Invoke(BirdsKilled);
            OnBirdKilled?.Invoke(duckType, weaponType);

            Debug.Log($"Duck killed - Type: {duckType}, Weapon: {weaponType}, Base: {basePoints}, Multiplier: {Multiplier}, Earned: {pointsEarned}");
        }

        public void BirdPassed()
        {
            MinusLife();
            OnBirdPassed?.Invoke();
            Debug.Log($"Duck passed - Lives remaining: {Lives}");

            if (Lives <= 0)
            {
                TriggerGameOver();
            }
        }

        public void AddComboPoints(Constants.MultiKillType type, Vector3? worldPosition = null)
        {
            AddComboPoints(type, SelectedWeaponType, worldPosition);
        }

        public void AddComboPoints(Constants.MultiKillType type, Constants.WeaponType weaponType, Vector3? worldPosition = null)
        {
            int bonusPoints = Constants.ComboPoints.GetPoints(type);
            AddPoints(bonusPoints);
            Vector3 comboPosition = worldPosition ?? Vector3.zero;
            OnComboKill?.Invoke(type, bonusPoints, comboPosition);
            OnComboKillDetailed?.Invoke(type, weaponType, bonusPoints, comboPosition);
            Debug.Log($"Combo! {type} with {weaponType} - Bonus points: {bonusPoints}");
        }

        private void AddPoints(int points)
        {
            Score += points;

            if (Score > HighScore)
            {
                HighScore = Score;
                SaveHighScore();
            }

            OnScoreChanged?.Invoke(Score);
        }

        private void MinusLife()
        {
            Lives = Mathf.Max(0, Lives - 1);
            OnLivesChanged?.Invoke(Lives);
        }

        private void PlusLife()
        {
            Lives = Mathf.Min(Constants.MAX_LIVES, Lives + 1);
            OnLivesChanged?.Invoke(Lives);
        }

        public void AddBonusLives(int amount)
        {
            if (amount <= 0)
                return;

            Lives += amount;
            OnLivesChanged?.Invoke(Lives);
            Debug.Log($"[GameManager] Bonus lives added: +{amount}. Total lives: {Lives}");
        }

        private void CalculateMultiplier()
        {
            Multiplier = Mathf.Max(1, Difficulty / 5);
        }

        private void UpdateBirdsUntilNextDifficulty()
        {
            bool veryHardArcade = CurrentGameMode == Constants.GameMode.Arcade && ArcadeVeryHardMode;
            birdsUntilNextDifficulty = veryHardArcade
                ? Constants.DifficultyProgression.GetBirdsForNextDifficultyArcadeVeryHard(Difficulty)
                : Constants.DifficultyProgression.GetBirdsForNextDifficulty(Difficulty);
        }

        public void SetArcadeVeryHardMode(bool enabled)
        {
            ArcadeVeryHardMode = enabled;
            Debug.Log($"Arcade very hard mode set to: {ArcadeVeryHardMode}");
        }

        public void SetSelectedWeapon(Constants.WeaponType weaponType)
        {
            if (!IsWeaponSupportedInCurrentBuild(weaponType))
            {
                Debug.LogWarning($"[GameManager] Unsupported armory weapon '{weaponType}'. Falling back to Rifle.");
                weaponType = Constants.WeaponType.Rifle;
            }

            SelectedWeaponType = weaponType;
            PlayerPrefs.SetInt(Constants.PREFS_SELECTED_WEAPON, (int)weaponType);
            PlayerPrefs.Save();
            OnSelectedWeaponChanged?.Invoke(SelectedWeaponType);
            Debug.Log($"[GameManager] Selected weapon saved: {SelectedWeaponType}");
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;

            Coins += amount;
            PlayerPrefs.SetInt(Constants.PREFS_COINS, Coins);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(Coins);
            Debug.Log($"[GameManager] Coins added: +{amount}. Total: {Coins}");
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0)
                return false;

            if (amount == 0)
                return true;

            if (Coins < amount)
                return false;

            Coins -= amount;
            PlayerPrefs.SetInt(Constants.PREFS_COINS, Coins);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(Coins);
            Debug.Log($"[GameManager] Coins spent: -{amount}. Total: {Coins}");
            return true;
        }

        public void PauseGame()
        {
            if (IsGameOver || IsPaused)
                return;

            IsPaused = true;
            Time.timeScale = 0f;
            OnPauseStateChanged?.Invoke(true);
            Debug.Log("Game paused");
        }

        public void ResumeGame()
        {
            if (IsGameOver || !IsPaused)
                return;

            IsPaused = false;
            Time.timeScale = 1f;
            OnPauseStateChanged?.Invoke(false);
            Debug.Log("Game resumed");
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            InitializeGame(CurrentGameMode);
            Debug.Log("Game restarted");
        }

        private void TriggerGameOver()
        {
            if (IsGameOver)
                return;

            if (CurrentGameMode == Constants.GameMode.Campaign)
            {
                ConsecutiveFailedRuns++;
                Debug.Log($"[GameManager] Campaign failed run streak increased to {ConsecutiveFailedRuns}");
            }

            IsGameOver = true;
            IsPaused = false;
            Time.timeScale = 0f;
            SaveHighScore();
            OnPauseStateChanged?.Invoke(false);
            OnGameOver?.Invoke();
            Debug.Log($"Game Over! Final Score: {Score}, High Score: {HighScore}");
        }

        private void LoadHighScore()
        {
            if (CurrentGameMode == Constants.GameMode.Campaign)
            {
                HighScore = 0;
                return;
            }

            HighScore = PlayerPrefs.GetInt(Constants.PREFS_HIGH_SCORE_ARCADE, 0);
            Debug.Log($"High score loaded: {HighScore} for mode {CurrentGameMode}");
        }

        private void SaveHighScore()
        {
            if (CurrentGameMode == Constants.GameMode.Campaign)
                return;

            PlayerPrefs.SetInt(Constants.PREFS_HIGH_SCORE_ARCADE, (int)HighScore);
            PlayerPrefs.Save();
            Debug.Log($"High score saved: {HighScore} for mode {CurrentGameMode}");
        }

        public bool IsNewHighScore()
        {
            return Score >= HighScore && Score > 0;
        }

        public void ResetConsecutiveFailedRuns()
        {
            if (ConsecutiveFailedRuns == 0)
                return;

            ConsecutiveFailedRuns = 0;
            Debug.Log("[GameManager] Campaign failed run streak reset.");
        }

        private void LoadCoins()
        {
            Coins = Mathf.Max(0, PlayerPrefs.GetInt(Constants.PREFS_COINS, 0));
        }

        private void LoadSelectedWeapon()
        {
            int storedValue = PlayerPrefs.GetInt(Constants.PREFS_SELECTED_WEAPON, (int)Constants.WeaponType.Rifle);
            if (!Enum.IsDefined(typeof(Constants.WeaponType), storedValue))
            {
                SelectedWeaponType = Constants.WeaponType.Rifle;
                return;
            }

            Constants.WeaponType loadedType = (Constants.WeaponType)storedValue;
            SelectedWeaponType = IsWeaponSupportedInCurrentBuild(loadedType)
                ? loadedType
                : Constants.WeaponType.Rifle;
        }

        private static bool IsWeaponSupportedInCurrentBuild(Constants.WeaponType type)
        {
            switch (type)
            {
                case Constants.WeaponType.Rifle:
                case Constants.WeaponType.Cabirne:
                case Constants.WeaponType.Beretta:
                case Constants.WeaponType.MrSulko:
                case Constants.WeaponType.LaserGun:
                case Constants.WeaponType.TeslaGun:
                case Constants.WeaponType.PiranhaGun:
                    return true;
                default:
                    return false;
            }
        }
    }
}
