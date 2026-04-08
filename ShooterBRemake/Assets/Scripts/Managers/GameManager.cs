using UnityEngine;
using System;
using System.Collections.Generic;

namespace ShooterB
{
    public class GameManager : MonoBehaviour
    {
        private const string Quack1ResourcePath = "Audio/quack1";
        private const string Quack2ResourcePath = "Audio/quack2";
        private const string Quack3ResourcePath = "Audio/quack3";

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
        public event Action<Constants.DuckType, Constants.WeaponType> OnBirdKilled;
        public event Action OnBirdPassed;
        public event Action<Constants.WeaponType> OnSelectedWeaponChanged;

        private readonly HashSet<Constants.WeaponType> unlockedWeapons = new HashSet<Constants.WeaponType>();
        private int birdsUntilNextDifficulty;
        private AudioSource duckKillSfxSource;
        private AudioClip[] quackKillClips;
        private bool hasLoggedMissingQuackClip;

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
            LoadUnlockedWeapons();
            LoadSelectedWeapon();
            _ = AchievementManager.Instance;
            _ = DailyAwardsManager.Instance;
            _ = AudioSettingsManager.Instance;
            _ = MusicManager.Instance;
            _ = UIClickSoundManager.Instance;
            _ = WeaponReloadSoundManager.Instance;
            _ = WeaponShootSoundManager.Instance;
            AudioSettingsManager.Instance.OnAudioSettingsChanged += HandleAudioSettingsChanged;
        }

        private void OnDestroy()
        {
            if (AudioSettingsManager.HasInstance)
                AudioSettingsManager.Instance.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
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

            GameLog.Log($"Game initialized - Mode: {mode}, Difficulty: {Difficulty}, Lives: {Lives}");
        }

        public void BirdCreated()
        {
            BirdCount++;
            birdsUntilNextDifficulty--;

            if (birdsUntilNextDifficulty <= 0 && Difficulty < Constants.MAX_DIFFICULTY)
            {
                Difficulty++;
                CalculateMultiplier();
                UpdateBirdsUntilNextDifficulty();
                OnDifficultyChanged?.Invoke(Difficulty);
                OnMultiplierChanged?.Invoke(Multiplier);
                GameLog.Log($"Difficulty increased to {Difficulty}, Multiplier: {Multiplier}");
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
            OnBirdKilled?.Invoke(duckType, weaponType);
            PlayRandomQuackOnDuckKill();

            GameLog.Log($"Duck killed - Type: {duckType}, Weapon: {weaponType}, Base: {basePoints}, Multiplier: {Multiplier}, Earned: {pointsEarned}");
        }

        public void BirdPassed(Constants.DuckType duckType)
        {
            MinusLife(GetLifePenaltyForPassedDuck(duckType));
            OnBirdPassed?.Invoke();
            GameLog.Log($"Duck passed - Lives remaining: {Lives}");

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
            GameLog.Log($"Combo! {type} with {weaponType} - Bonus points: {bonusPoints}");
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

        private void PlayRandomQuackOnDuckKill()
        {
            EnsureDuckKillSfxReady();
            if (duckKillSfxSource == null || quackKillClips == null || quackKillClips.Length == 0)
                return;

            int randomIndex = UnityEngine.Random.Range(0, quackKillClips.Length);
            AudioClip clip = quackKillClips[randomIndex];
            if (clip != null)
                duckKillSfxSource.PlayOneShot(clip);
        }

        private void EnsureDuckKillSfxReady()
        {
            if (duckKillSfxSource == null)
            {
                duckKillSfxSource = gameObject.GetComponent<AudioSource>();
                if (duckKillSfxSource == null)
                    duckKillSfxSource = gameObject.AddComponent<AudioSource>();

                duckKillSfxSource.playOnAwake = false;
                duckKillSfxSource.loop = false;
                duckKillSfxSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
            }

            if (quackKillClips != null)
                return;

            AudioClip quack1 = Resources.Load<AudioClip>(Quack1ResourcePath);
            AudioClip quack2 = Resources.Load<AudioClip>(Quack2ResourcePath);
            AudioClip quack3 = Resources.Load<AudioClip>(Quack3ResourcePath);
            quackKillClips = System.Array.FindAll(
                new[] { quack1, quack2, quack3 },
                clip => clip != null);

            if (quackKillClips.Length == 0 && !hasLoggedMissingQuackClip)
            {
                hasLoggedMissingQuackClip = true;
                GameLog.Warning("[GameManager] Missing quack SFX clips at Resources/Audio/quack1|quack2|quack3.");
            }
        }

        private void HandleAudioSettingsChanged()
        {
            if (duckKillSfxSource == null)
                return;

            duckKillSfxSource.volume = AudioSettingsManager.Instance.GetEffectiveSfxVolume();
        }

        private void MinusLife(int amount = 1)
        {
            Lives = Mathf.Max(0, Lives - Mathf.Max(1, amount));
            OnLivesChanged?.Invoke(Lives);
        }

        private static int GetLifePenaltyForPassedDuck(Constants.DuckType duckType)
        {
            switch (duckType)
            {
                case Constants.DuckType.MK_SAMUIL_BOSS_DUCK:
                case Constants.DuckType.FRENCH_MUSKETEER_BOSS_DUCK:
                case Constants.DuckType.BRITISH_SHERLOCK_BOSS_DUCK:
                case Constants.DuckType.USA_BOSS_DUCK:
                case Constants.DuckType.USA_ADMIRAL_BOSS_DUCK:
                case Constants.DuckType.JAPANESE_SAMURAI_BOSS_DUCK:
                case Constants.DuckType.EGYPT_SCARAB_BOSS_DUCK:
                    return 3;
                default:
                    return 1;
            }
        }

        public void AddBonusLives(int amount)
        {
            if (amount <= 0)
                return;

            Lives += amount;
            OnLivesChanged?.Invoke(Lives);
            GameLog.Log($"[GameManager] Bonus lives added: +{amount}. Total lives: {Lives}");
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
            GameLog.Log($"Arcade very hard mode set to: {ArcadeVeryHardMode}");
        }

        public void SetSelectedWeapon(Constants.WeaponType weaponType)
        {
            if (!IsWeaponSupportedInCurrentBuild(weaponType))
            {
                GameLog.Warning($"[GameManager] Unsupported armory weapon '{weaponType}'. Falling back to Rifle.");
                weaponType = Constants.WeaponType.Rifle;
            }

            if (!IsWeaponUnlocked(weaponType))
            {
                GameLog.Warning($"[GameManager] Locked weapon '{weaponType}' cannot be selected. Keeping {SelectedWeaponType}.");
                return;
            }

            SelectedWeaponType = weaponType;
            PlayerPrefs.SetInt(Constants.PREFS_SELECTED_WEAPON, (int)weaponType);
            PlayerPrefs.Save();
            OnSelectedWeaponChanged?.Invoke(SelectedWeaponType);
            GameLog.Log($"[GameManager] Selected weapon saved: {SelectedWeaponType}");
        }

        public bool IsWeaponUnlocked(Constants.WeaponType weaponType)
        {
            return unlockedWeapons.Contains(weaponType);
        }

        public void UnlockWeapon(Constants.WeaponType weaponType)
        {
            if (!IsWeaponSupportedInCurrentBuild(weaponType))
                return;

            if (!unlockedWeapons.Add(weaponType))
                return;

            SaveUnlockedWeapons();
            GameLog.Log($"[GameManager] Weapon unlocked: {weaponType}");
        }

        public void UnlockAllWeapons()
        {
            bool changed = false;
            Constants.WeaponType[] weaponTypes = (Constants.WeaponType[])Enum.GetValues(typeof(Constants.WeaponType));
            for (int i = 0; i < weaponTypes.Length; i++)
            {
                Constants.WeaponType weaponType = weaponTypes[i];
                if (!IsWeaponSupportedInCurrentBuild(weaponType))
                    continue;

                changed |= unlockedWeapons.Add(weaponType);
            }

            if (!changed)
                return;

            SaveUnlockedWeapons();
            GameLog.Log("[GameManager] All supported weapons unlocked for testing.");
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;

            Coins += amount;
            PlayerPrefs.SetInt(Constants.PREFS_COINS, Coins);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(Coins);
            GameLog.Log($"[GameManager] Coins added: +{amount}. Total: {Coins}");
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
            GameLog.Log($"[GameManager] Coins spent: -{amount}. Total: {Coins}");
            return true;
        }

        public void ResetCoins()
        {
            if (Coins == 0)
                return;

            Coins = 0;
            PlayerPrefs.SetInt(Constants.PREFS_COINS, Coins);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(Coins);
            GameLog.Log("[GameManager] Coins reset to 0.");
        }

        public void ResetUnlockedWeaponsToDefault()
        {
            unlockedWeapons.Clear();
            unlockedWeapons.Add(Constants.WeaponType.PiranhaGun);
            SaveUnlockedWeapons();

            if (!IsWeaponUnlocked(SelectedWeaponType))
            {
                SelectedWeaponType = GetDefaultSelectedWeapon();
                PlayerPrefs.SetInt(Constants.PREFS_SELECTED_WEAPON, (int)SelectedWeaponType);
                PlayerPrefs.Save();
                OnSelectedWeaponChanged?.Invoke(SelectedWeaponType);
            }

            GameLog.Log("[GameManager] Unlocked weapons reset to defaults.");
        }

        public void ResetAllProgressForTesting()
        {
            PlayerPrefs.DeleteKey(Constants.PREFS_COINS);
            PlayerPrefs.DeleteKey(Constants.PREFS_SELECTED_WEAPON);
            PlayerPrefs.DeleteKey(Constants.PREFS_UNLOCKED_WEAPONS);

            Coins = 0;
            LoadUnlockedWeapons();
            LoadSelectedWeapon();
            OnCoinsChanged?.Invoke(Coins);
            OnSelectedWeaponChanged?.Invoke(SelectedWeaponType);
            GameLog.Log("[GameManager] Core testing progress reset.");
        }

        public void PauseGame()
        {
            if (IsGameOver || IsPaused)
                return;

            IsPaused = true;
            Time.timeScale = 0f;
            OnPauseStateChanged?.Invoke(true);
            GameLog.Log("Game paused");
        }

        public void ResumeGame()
        {
            if (IsGameOver || !IsPaused)
                return;

            IsPaused = false;
            Time.timeScale = 1f;
            OnPauseStateChanged?.Invoke(false);
            GameLog.Log("Game resumed");
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            InitializeGame(CurrentGameMode);
            GameLog.Log("Game restarted");
        }

        private void TriggerGameOver()
        {
            if (IsGameOver)
                return;

            if (CurrentGameMode == Constants.GameMode.Campaign)
            {
                ConsecutiveFailedRuns++;
                GameLog.Log($"[GameManager] Campaign failed run streak increased to {ConsecutiveFailedRuns}");
            }

            IsGameOver = true;
            IsPaused = false;
            Time.timeScale = 0f;
            SaveHighScore();
            OnPauseStateChanged?.Invoke(false);
            OnGameOver?.Invoke();
            GameLog.Log($"Game Over! Final Score: {Score}, High Score: {HighScore}");
        }

        private void LoadHighScore()
        {
            if (CurrentGameMode == Constants.GameMode.Campaign)
            {
                HighScore = 0;
                return;
            }

            HighScore = PlayerPrefs.GetInt(Constants.PREFS_HIGH_SCORE_ARCADE, 0);
            GameLog.Log($"High score loaded: {HighScore} for mode {CurrentGameMode}");
        }

        private void SaveHighScore()
        {
            if (CurrentGameMode == Constants.GameMode.Campaign)
                return;

            PlayerPrefs.SetInt(Constants.PREFS_HIGH_SCORE_ARCADE, (int)HighScore);
            PlayerPrefs.Save();
            GameLog.Log($"High score saved: {HighScore} for mode {CurrentGameMode}");
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
            GameLog.Log("[GameManager] Campaign failed run streak reset.");
        }

        private void LoadCoins()
        {
            Coins = Mathf.Max(0, PlayerPrefs.GetInt(Constants.PREFS_COINS, 0));
        }

        private void LoadUnlockedWeapons()
        {
            unlockedWeapons.Clear();
            unlockedWeapons.Add(Constants.WeaponType.PiranhaGun);

            string stored = PlayerPrefs.GetString(Constants.PREFS_UNLOCKED_WEAPONS, string.Empty);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                string[] parts = stored.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!int.TryParse(parts[i], out int rawValue) || !Enum.IsDefined(typeof(Constants.WeaponType), rawValue))
                        continue;

                    Constants.WeaponType weaponType = (Constants.WeaponType)rawValue;
                    if (IsWeaponSupportedInCurrentBuild(weaponType))
                        unlockedWeapons.Add(weaponType);
                }
            }

            SaveUnlockedWeapons();
        }

        private void LoadSelectedWeapon()
        {
            int storedValue = PlayerPrefs.GetInt(Constants.PREFS_SELECTED_WEAPON, (int)Constants.WeaponType.Rifle);
            if (!Enum.IsDefined(typeof(Constants.WeaponType), storedValue))
            {
                SelectedWeaponType = GetDefaultSelectedWeapon();
                return;
            }

            Constants.WeaponType loadedType = (Constants.WeaponType)storedValue;
            SelectedWeaponType = IsWeaponSupportedInCurrentBuild(loadedType) && IsWeaponUnlocked(loadedType)
                ? loadedType
                : GetDefaultSelectedWeapon();
        }

        private void SaveUnlockedWeapons()
        {
            List<int> storedValues = new List<int>(unlockedWeapons.Count);
            foreach (Constants.WeaponType weaponType in unlockedWeapons)
            {
                if (IsWeaponSupportedInCurrentBuild(weaponType))
                    storedValues.Add((int)weaponType);
            }

            storedValues.Sort();
            PlayerPrefs.SetString(Constants.PREFS_UNLOCKED_WEAPONS, string.Join(",", storedValues));
            PlayerPrefs.Save();
        }

        private Constants.WeaponType GetDefaultSelectedWeapon()
        {
            return IsWeaponUnlocked(Constants.WeaponType.Rifle)
                ? Constants.WeaponType.Rifle
                : Constants.WeaponType.PiranhaGun;
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
