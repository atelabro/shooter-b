# Campaign Mode - Current State

## What Is Complete (Code)

All scripts from the previous plan are implemented:

- `StageSpawnConfig.cs` - ScriptableObject with duck type weights, movement weights, spawn timing, speed multiplier
- `StageConfig.spawnConfig` - field wired in
- `Duck.cs` - `Initialize()` accepts goStraightWeight, goTopWeight, goBottomWeight with defaults
- `DuckSpawner.cs` - reads SpawnConfig in Campaign mode, passes weights and speed multiplier to ducks, respects maxActiveDucks
- `GameManager.cs` - BirdsKilled counter, OnBirdsKilledChanged event, OnStageComplete event, CheckStageClearCondition
- `GameHUD.cs` - killProgressText, starThresholdsText fields, ApplyCampaignMode(), hides highScoreText in Campaign
- `GameOverModalController.cs` - hides high score in Campaign, "Back to Map" routes to CampaignMapScene
- `GameController.cs` - wires OnStageComplete -> HandleStageComplete -> StageCompleteModalController.Show()
- `StageCompleteModalController.cs` - shows stage name, score, stars text, continue button

## What Needs To Be Done

### 1. StageCompleteModalController - Star Icons
Current implementation shows stars as plain text ("Stars: X / 3").
Replace with 3 Image slots showing filled/empty star sprites.

Fields to change:
- Remove `starsText` (TextMeshProUGUI)
- Add `Image[] starIcons` (3 elements wired in inspector)
- Add `Sprite filledStarSprite`
- Add `Sprite emptyStarSprite`
- `Show()` sets each icon sprite based on earned star count

### 2. Unity Editor Wiring (GameScene)
- Add StageCompleteModalController component to GameScene canvas
- Link killProgressText and starThresholdsText TMP fields in HUD inspector
- Link stageCompleteModalController in GameController inspector

### 3. Create StageSpawnConfig Assets
- Create StageSpawnConfig assets for each Countryside stage (3 stages)
- Assign to each StageConfig asset via inspector

### 4. Campaign Map - Pin Position Tweaks
- Pin positions on world map still need coordinate adjustments (done visually in editor)

### 5. Star Sprites
- Star icon sprites are placeholder - replace with proper star art when available

## Notes
- No new scene needed for Campaign game - same GameScene, HUD adapts to mode
- Arcade mode is completely unaffected by SpawnConfig (null returned in non-Campaign mode)
- CampaignProgressManager.ActiveStageConfig must be set before GameScene loads (done via SceneController.LoadCampaignStage)
