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
        public int Difficulty { get; private set; }
        public long HighScore { get; private set; }
        public int Lives { get; private set; }
        public Constants.GameMode CurrentGameMode { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsGameOver { get; private set; }

        public event Action<long> OnScoreChanged;
        public event Action<int> OnMultiplierChanged;
        public event Action<int> OnLivesChanged;
        public event Action<int> OnDifficultyChanged;
        public event Action<bool> OnPauseStateChanged;
        public event Action OnGameOver;
        public event Action<Constants.MultiKillType, int> OnComboKill;

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
        }

        public void InitializeGame(Constants.GameMode mode)
        {
            CurrentGameMode = mode;
            Score = 0;
            BirdCount = 0;
            Difficulty = Constants.INITIAL_DIFFICULTY;
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
            int basePoints = Constants.DuckPoints.GetPoints(duckType);
            int pointsEarned = basePoints * Multiplier;

            AddPoints(pointsEarned);
            Debug.Log($"Duck killed - Type: {duckType}, Base: {basePoints}, Multiplier: {Multiplier}, Earned: {pointsEarned}");
        }

        public void BirdPassed()
        {
            MinusLife();
            Debug.Log($"Duck passed - Lives remaining: {Lives}");

            if (Lives <= 0)
            {
                TriggerGameOver();
            }
        }

        public void AddComboPoints(Constants.MultiKillType type)
        {
            int bonusPoints = Constants.ComboPoints.GetPoints(type);
            AddPoints(bonusPoints);
            OnComboKill?.Invoke(type, bonusPoints);
            Debug.Log($"Combo! {type} - Bonus points: {bonusPoints}");
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

        private void CalculateMultiplier()
        {
            Multiplier = Mathf.Max(1, Difficulty / 5);
        }

        private void UpdateBirdsUntilNextDifficulty()
        {
            birdsUntilNextDifficulty = Constants.DifficultyProgression.GetBirdsForNextDifficulty(Difficulty);
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
            string key = CurrentGameMode == Constants.GameMode.Normal
                ? Constants.PREFS_HIGH_SCORE_NORMAL
                : Constants.PREFS_HIGH_SCORE_ARCADE;

            HighScore = PlayerPrefs.GetInt(key, 0);
            Debug.Log($"High score loaded: {HighScore} for mode {CurrentGameMode}");
        }

        private void SaveHighScore()
        {
            string key = CurrentGameMode == Constants.GameMode.Normal
                ? Constants.PREFS_HIGH_SCORE_NORMAL
                : Constants.PREFS_HIGH_SCORE_ARCADE;

            PlayerPrefs.SetInt(key, (int)HighScore);
            PlayerPrefs.Save();
            Debug.Log($"High score saved: {HighScore} for mode {CurrentGameMode}");
        }

        public bool IsNewHighScore()
        {
            return Score >= HighScore && Score > 0;
        }
    }
}
