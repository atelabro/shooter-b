# Level/Map System Implementation Plan

## Context
The game engine is playable with ducks, shooting, death sprites, and scoring. The next step is transforming it from an endless arcade game into a city-hopping campaign with wave-based levels. Each city (Paris, New York, Tokyo) has its own background, difficulty curve, wave structure, and star ratings. Arcade mode stays as a separate option.

## Design Summary
- **3 levels**: Paris (easy, 5 waves), New York (medium, 7 waves), Tokyo (hard, 10 waves)
- **Wave-based**: Each wave spawns a fixed number of ducks at a set difficulty, then waits for all ducks to be resolved before starting the next wave
- **Star ratings**: 1 star = complete, 2 stars = score threshold, 3 stars = high score threshold
- **Progression**: Paris unlocked by default, beat a level to unlock the next
- **Arcade mode preserved**: Existing endless gameplay stays as a menu option

## New Files (6 scripts)

### 1. `Assets/Scripts/Data/LevelConfig.cs` - ScriptableObject
Level configuration with WaveConfig struct. Fields: levelId, displayName, backgroundSprite, waves array (duckCount, difficulty, preWaveDelay per wave), twoStarScore, threeStarScore, startingLives, startingDifficulty.

### 2. `Assets/Scripts/Managers/LevelProgressManager.cs` - Singleton
PlayerPrefs persistence for level unlocks, stars, and high scores. Methods: IsLevelUnlocked, UnlockLevel, GetStars, SaveStars, GetLevelHighScore, SaveLevelHighScore, GetNextLevel, CalculateStars.

### 3. `Assets/Scripts/Controllers/WaveController.cs` - MonoBehaviour
Core wave orchestration. Coroutine iterates through waves: sets difficulty, fires OnWaveStarted event, waits preWaveDelay, tells DuckSpawner to spawn N ducks, waits until all resolved, fires OnWaveCleared. After all waves: calculates stars, saves progress, unlocks next level, fires OnLevelComplete. Tracks ducksAliveOrPending counter decremented by OnDuckResolved callback.

### 4. `Assets/Scripts/UI/LevelSelectController.cs`
Drives LevelSelect scene. Reads progress from LevelProgressManager, populates level panels, handles level selection (stores config in GameManager, loads GameScene).

### 5. `Assets/Scripts/UI/LevelPanel.cs`
Per-level card UI helper. Setup method receives config + progress data and toggles star icons, lock overlay, high score text, play button interactability.

### 6. `Assets/Scripts/UI/LevelCompletePanel.cs`
End-of-level results overlay in GameScene. Shows win/fail, score, stars, retry/next/menu buttons.

## Modified Files (7)

### 1. `Constants.cs`
- Add `LevelId` enum (Paris, NewYork, Tokyo)
- Add `LevelSelect` to `SceneType` enum
- Add `LevelPrefs` static class with key generation methods

### 2. `GameManager.cs`
- Add `SelectedLevel` property + `SetSelectedLevel()` method
- Add `SetDifficulty(int)` for WaveController to override difficulty per wave
- `InitializeGame()`: use level's startingLives/startingDifficulty when SelectedLevel is set
- `BirdCreated()`: skip auto-difficulty-increase when in level mode (WaveController controls difficulty)
- `LoadHighScore()`/`SaveHighScore()`: delegate to LevelProgressManager when in level mode

### 3. `DuckSpawner.cs`
- Add `WaveController` reference + `SetWaveController()`
- Add `StartWaveSpawning(int duckCount, int difficulty)` with dedicated coroutine that spawns exactly N ducks then stops
- `ReturnDuckToPool()`: notify WaveController.OnDuckResolved() when in wave mode
- `Start()`: only auto-start spawning in Arcade mode (no SelectedLevel)

### 4. `GameController.cs`
- Add serialized refs: waveController, duckSpawner, backgroundRenderer, levelCompletePanel
- In Start(): detect level mode, setup background sprite, wire up WaveController, subscribe to OnLevelComplete
- HandleLevelComplete callback shows LevelCompletePanel

### 5. `SceneController.cs`
- Add `LevelSelect -> "LevelSelectScene"` mapping

### 6. `MenuController.cs`
- Campaign button -> loads LevelSelect scene
- Arcade button -> loads GameScene in Arcade mode
- Replace single Play button with two options

### 7. `GameHUD.cs`
- Add wave text + ducks remaining text
- Subscribe to WaveController events (OnWaveStarted, OnDuckCountChanged)
- Hide wave UI in Arcade mode

## Implementation Order

### Phase 1 - Data layer
1. Constants.cs (enums, LevelPrefs)
2. LevelConfig.cs (ScriptableObject)
3. LevelProgressManager.cs

### Phase 2 - Wave mechanics
4. WaveController.cs
5. DuckSpawner.cs modifications
6. GameManager.cs modifications
7. GameController.cs modifications

### Phase 3 - UI and scenes
8. SceneController.cs (add LevelSelect)
9. MenuController.cs (Campaign + Arcade)
10. LevelPanel.cs
11. LevelSelectController.cs
12. LevelCompletePanel.cs
13. GameHUD.cs (wave display)

## Unity Editor Setup (after code)
1. Create `Assets/Resources/Levels/` folder
2. Create 3 LevelConfig ScriptableObject assets (Create > ShooterB > Level Config): Paris, NewYork, Tokyo
3. Configure wave data, star thresholds, and assign background sprites
4. Create LevelSelectScene with Canvas, 3 level card panels, back button
5. Add WaveController GameObject and LevelCompletePanel to GameScene
6. Wire serialized references on GameController
7. Update MenuScene buttons
8. Add LevelSelectScene to Build Settings

## Level Data

### Paris (Easy)
- startingLives: 5, startingDifficulty: 1
- 5 waves: (5 ducks/d1), (7/d3), (8/d5), (10/d7), (12/d10)
- 2 stars: 150 points, 3 stars: 300 points

### New York (Medium)
- startingLives: 4, startingDifficulty: 5
- 7 waves: (8/d5), (10/d8), (12/d10), (14/d13), (15/d16), (16/d19), (18/d22)
- 2 stars: 500 points, 3 stars: 1000 points

### Tokyo (Hard)
- startingLives: 3, startingDifficulty: 10
- 10 waves with difficulty from 10 to 32
- 2 stars: 1000 points, 3 stars: 2000 points

## Verification
1. Menu shows Campaign + Arcade buttons
2. Campaign -> LevelSelect shows Paris unlocked, NY and Tokyo locked
3. Select Paris -> GameScene loads with Paris background, wave counter shows
4. Waves progress: "Wave 1/5" through "Wave 5/5", ducks count down per wave
5. Complete Paris -> LevelCompletePanel shows stars, NY unlocks
6. Arcade mode still works as endless
7. Stars and high scores persist across sessions
