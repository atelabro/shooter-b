# Unity Scene Setup Instructions

## Quick Start

1. Open Unity Hub and open the project from `ShooterBRemake/`
2. Open `Assets/Scenes/MenuScene.unity`
3. Press Play

## Current Scenes

| Scene | File | Entry Point |
|---|---|---|
| MenuScene | `Assets/Scenes/MenuScene.unity` | App start |
| CampaignMapScene | `Assets/Scenes/CampaignMapScene.unity` | From menu Campaign button |
| CampaignGameScene | `Assets/Scenes/CampaignGameScene.unity` | From map stage selection |
| GameScene | `Assets/Scenes/GameScene.unity` | Arcade mode (not in main menu flow) |
| ArmoryScene | `Assets/Scenes/ArmoryScene.unity` | From menu Armory button |
| AchievementsScene | `Assets/Scenes/AchievementsScene.unity` | From menu Achievements button |

## Build Settings Scene Order

Ensure all scenes are registered under File > Build Settings:
- MenuScene
- GameScene
- CampaignMapScene
- CampaignGameScene
- ArmoryScene
- AchievementsScene

## What Is Fully Implemented

- All 7 weapons (Rifle, Cabirne, Beretta, MrSulko, LaserGun, TeslaGun, PiranhaGun) and their bullets
- Duck spawning (arcade random and campaign sequence)
- All game managers (GameManager, SceneController, CampaignProgressManager, AchievementManager, DailyAwardsManager, LocalizationManager)
- Campaign map with city pins and stage selection
- Achievement system (26 achievements)
- Daily awards system (3 rotating daily objectives)
- Armory (weapon unlock and selection with coin economy)
- Localization (English + Macedonian)
- HUDs for both arcade and campaign scenes
- Pause, game over, and stage complete modals

## Scene Object Requirements

### MenuScene
- `MenuController` component needs: `campaignButton`, `armoryButton`, `achievementsButton`, `quitButton`, `highScoreText`, `titleText`, and matching button text fields
- `LanguageDropdownController` for language switching

### CampaignGameScene
- `CampaignGameController` GameObject: `CampaignGameController` + `InputController` + `ShooterController`
- `DuckSpawner` GameObject: `CampaignDuckSpawner` with duck prefab and 5 frame arrays assigned
- `HUD` GameObject: `CampaignHUD` with all fields assigned
- Canvas modals: `PauseModalPanel`, `GameOverModalPanel`, `StageCompleteModalPanel`
- `CampaignGameController.editorFallbackStage`: assign a StageConfig asset for editor Play testing

### GameScene (Arcade)
- `GameController` GameObject: `GameController` + `InputController` + `ShooterController`
- `DuckSpawner` GameObject: `DuckSpawner` with duck prefab and frame arrays
- `HUD` GameObject: `GameHUD` with all fields assigned
- Canvas modals: `PauseModalPanel`, `GameOverModalPanel`

## Campaign Data Assets

Located in `Assets/Data/Campaign/`:
- `CityConfig_Countryside.asset`, `CityConfig_NewYork.asset`, `CityConfig_Paris.asset`
- `StageConfig_Countryside_0.asset`, `StageConfig_Countryside_1.asset`, `StageConfig_Countryside_2.asset`
  - Note: `stageIndex` on each asset must match its filename number
- `SpawnConfig/StageSpawnConfig_CountrySide_0.asset`
  - Stages 1 and 2 do not have spawn configs assigned yet

## Troubleshooting

**Missing Script errors**: Reimport assets via right-click Assets > Reimport All

**Scenes not loading**: Verify scene names match exactly in Build Settings

**Campaign stage not starting in editor**: Assign `editorFallbackStage` on `CampaignGameController` to any valid StageConfig asset
