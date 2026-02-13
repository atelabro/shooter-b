# Unity Scene Setup Instructions

Follow these steps in Unity Editor to get the game running.

## Quick Start

**To run the game immediately:**
1. Open Unity Hub: `cd ShooterBRemake && open -a "Unity Hub" .`
2. Open the project in Unity Editor (select it from Unity Hub)
3. Open `Assets/Scenes/MenuScene.unity`
4. Press the Play button (▶) at the top center or press `Cmd+P`

If you see errors or missing UI elements, follow the detailed setup steps below.

## Prerequisites

**Current Implementation Status:**
- ✅ **Scenes**: MenuScene and GameScene exist in `Assets/Scenes/`
- ✅ **Core Scripts**: GameManager, SceneController, Constants, ObjectPool implemented
- ✅ **UI Scripts**: MenuController, GameHUD, GameController implemented
- ⚠️ **Assets**: Some backgrounds imported, but duck/weapon sprites need importing
- ❌ **Gameplay**: Duck spawning, shooting mechanics, weapons not yet implemented

**What Works Now:**
- Menu scene with Play/Quit buttons
- Scene transitions (Menu ↔ Game)
- Basic HUD display (score, lives, multiplier)
- Pause/Resume (press Escape in game)
- High score persistence

**What's Still Needed:**
- Import original game sprites (ducks, weapons, bullets, UI)
- Implement duck spawning and movement
- Implement shooting mechanics and input
- Create weapon system
- Add visual and audio effects

## 0. Import Original Game Assets (Recommended First Step)

The original Android game assets are available in `../ShooterBgame/assets/`. You should import these into Unity:

### Available Assets:
- **Ducks**: `duckanimated04.png`, `duckanimateddead04.png`, various small animated ducks (Che, Rambo, Soldier, Kimono, Private)
- **Weapons**: `gun.png`, `sniper.png`, `laserGun.png`, `teslaGun.png`, `beretta.png`, `cabarne.png`, `mrsulko.png`
- **Bullets**: `shot01.png`, `rifleshot01.png`, `laserShot.png`, `teslaShot.png`, `mrsulBullet.png`, `helloDucksBullet.png`
- **Backgrounds**: `parallax_background_layer_back.png`, `parallax_background_layer_mid.png`, `parallax_background_layer_front.png`, `background01.png`
- **UI Elements**: `pause.png`, `play.png`, `replay.png`, `bonusbox.png`, `gameHeader.png`
- **Lives Icons**: `0lives.png`, `1lives.png`, `2lives.png`, `3lives.png`
- **Dead Ducks**: `friedChicken.png`, `friedchickenMrSulko.png`, `friedchickenCoal.png`
- **Fonts**: `capture_it.ttf`, `droid.ttf`

### How to Import:
1. In Unity, navigate to `Assets/Sprites/` in Project window
2. Drag and drop image files from `../ShooterBgame/assets/gfx/` into Unity
3. For each sprite, set Texture Type to "Sprite (2D and UI)"
4. For animated ducks, set Sprite Mode to "Multiple" and use Sprite Editor to slice
5. Import fonts to `Assets/Fonts/` from `../ShooterBgame/assets/font/`

## 1. Configure Build Settings

1. Open **File > Build Settings**
2. Ensure scenes are in this order:
   - 0: MenuScene
   - 1: GameScene
3. If scenes are missing, click "Add Open Scenes" after opening each scene

## 2. Setup MenuScene

**Note**: If MenuScene already has UI elements set up, skip to step 3. Otherwise, follow these steps:

1. **Open MenuScene** in Unity Editor

2. **Create Canvas** (if not exists):
   - Right-click in Hierarchy > **UI > Canvas**
   - Set Canvas Scaler to "Scale With Screen Size"
   - Reference Resolution: 1920 x 1080

3. **Add MenuController Script**:
   - Create empty GameObject: **GameObject > Create Empty**
   - Rename to "MenuController"
   - Add component: `MenuController.cs`

4. **Create UI Elements**:

   **Title Text**:
   - Right-click Canvas > **UI > Text - TextMeshPro**
   - Rename to "TitleText"
   - Position: Center top (Y: 300)
   - Font Size: 120
   - Text: "DUCKOFF"
   - Alignment: Center

   **Play Button**:
   - Right-click Canvas > **UI > Button - TextMeshPro**
   - Rename to "PlayButton"
   - Position: Center (Y: 0)
   - Width: 400, Height: 100
   - Button text: "PLAY"
   - Font Size: 60

   **High Score Text**:
   - Right-click Canvas > **UI > Text - TextMeshPro**
   - Rename to "HighScoreText"
   - Position: Center bottom (Y: -300)
   - Font Size: 40
   - Text: "High Score: 0"

   **Quit Button**:
   - Right-click Canvas > **UI > Button - TextMeshPro**
   - Rename to "QuitButton"
   - Position: Center (Y: -150)
   - Width: 400, Height: 100
   - Button text: "QUIT"
   - Font Size: 60

5. **Link UI to MenuController**:
   - Select "MenuController" GameObject
   - In Inspector, drag components to script fields:
     - Play Button → playButton
     - Quit Button → quitButton
     - High Score Text → highScoreText
     - Title Text → titleText

6. **Save Scene** (Ctrl/Cmd + S)

## 3. Setup GameScene

**Note**: If GameScene already has UI and camera configured, verify the setup. Otherwise, follow these steps:

1. **Open GameScene** in Unity Editor

2. **Setup Camera**:
   - Select Main Camera
   - Set Projection: **Orthographic**
   - Size: 5.4
   - Position: (0, 0, -10)
   - Background: Sky blue or black

3. **Create Canvas**:
   - Right-click in Hierarchy > **UI > Canvas**
   - Set Canvas Scaler to "Scale With Screen Size"
   - Reference Resolution: 1920 x 1080
   - Set Render Mode to "Screen Space - Overlay"

4. **Add GameController Script**:
   - Create empty GameObject: **GameObject > Create Empty**
   - Rename to "GameController"
   - Add component: `GameController.cs`
   - Drag Main Camera to "Main Camera" field

5. **Create HUD UI Elements**:

   **Score Text** (Top Left):
   - Right-click Canvas > **UI > Text - TextMeshPro**
   - Rename to "ScoreText"
   - Anchor: Top-Left
   - Position: X: 20, Y: -20
   - Font Size: 36
   - Text: "Score: 0"
   - Color: White

   **High Score Text** (Top Left):
   - Duplicate ScoreText
   - Rename to "HighScoreText"
   - Position: X: 20, Y: -70
   - Text: "High: 0"
   - Color: Green

   **Multiplier Text** (Top Center):
   - Duplicate ScoreText
   - Rename to "MultiplierText"
   - Anchor: Top-Center
   - Position: X: 0, Y: -20
   - Text: "x1"
   - Font Size: 48

   **Lives Text** (Top Right):
   - Duplicate ScoreText
   - Rename to "LivesText"
   - Anchor: Top-Right
   - Position: X: -20, Y: -20
   - Text: "Lives: 3"

   **Pause Button** (Top Right):
   - Right-click Canvas > **UI > Button - TextMeshPro**
   - Rename to "PauseButton"
   - Anchor: Top-Right
   - Position: X: -20, Y: -80
   - Width: 120, Height: 50
   - Button text: "PAUSE"

   **Menu Button** (Bottom Right):
   - Duplicate PauseButton
   - Rename to "MenuButton"
   - Anchor: Bottom-Right
   - Position: X: -20, Y: 20
   - Button text: "MENU"

6. **Add GameHUD Script**:
   - Create empty GameObject under Canvas
   - Rename to "GameHUD"
   - Add component: `GameHUD.cs`

7. **Link UI to GameHUD**:
   - Select "GameHUD" GameObject
   - Drag components to script fields:
     - ScoreText → scoreText
     - HighScoreText → highScoreText
     - MultiplierText → multiplierText
     - LivesText → livesText
     - PauseButton → pauseButton
     - MenuButton → menuButton

8. **Save Scene** (Ctrl/Cmd + S)

## 4. Test the Basic Game Flow

1. **Open MenuScene**
2. **Press Play** in Unity Editor
3. You should see:
   - Title "DUCKOFF"
   - Play and Quit buttons
   - High Score display
4. **Click Play Button**:
   - Should load GameScene
   - Should show HUD with Score, High Score, Multiplier, Lives
   - Pause and Menu buttons should work
5. **Click Menu Button**:
   - Should return to MenuScene

## Troubleshooting

**"Missing Script" errors?**
- Make sure all `.cs` files have been imported by Unity
- Check Console for compilation errors
- Refresh Assets (Right-click Assets folder > Reimport All)

**Buttons don't work?**
- Check that EventSystem exists in scene (auto-created with Canvas)
- Verify button OnClick events are connected in Inspector

**Scenes don't load?**
- Verify scene names match exactly: "MenuScene" and "GameScene"
- Check Build Settings includes both scenes

**TextMeshPro import prompt?**
- Click "Import TMP Essentials" when prompted
- This is normal for first-time TMP use

## 5. Next Implementation Steps

Once the game runs and you can navigate Menu → Game → Menu, the following systems need to be implemented:

### Phase 2: Duck System
1. **DuckController.cs** - Duck spawning manager
   - Spawn timing based on difficulty (see `Constants.SpawnTiming`)
   - Spawn patterns: Single, Double, Fleet, Bonus Weapon (see `Constants.DuckSpawnProbability`)
2. **Duck.cs** - Individual duck behavior
   - Movement patterns: GoStraight, GoTop, GoBottom (see original `Duck.java`)
   - Speed calculation based on difficulty (see `Constants.DuckSpeed`)
   - Physics-based movement using Rigidbody2D
   - Collision detection with bullets
3. **Duck Prefabs** - Create prefabs for 5 duck types (Type0-Type4)
   - Use sprites from imported assets
   - Attach Rigidbody2D and Collider2D
   - Set points value from `Constants.DuckPoints`

### Phase 3: Weapon & Shooting System
1. **WeaponManager.cs** - Weapon switching and state
   - 7 weapon types (Rifle, Cabirne, Beretta, MrSulko, LaserGun, TeslaGun, PiranhaGun)
   - Reference original weapons in `ShooterBgame/src/com/example/model/weapons/`
2. **InputController.cs** - Touch/click input handling
   - Touch to shoot
   - Weapon switching UI buttons
3. **Bullet System**:
   - **Bullet.cs** - Base bullet behavior
   - Individual bullet types for each weapon (see `ShooterBgame/src/com/example/model/bullets/`)
   - Bullet prefabs with physics
   - Object pooling for bullets (use existing `ObjectPool.cs`)

### Phase 4: Visual Effects & Polish
1. **Duck death animations** - Use `friedChicken.png`, `friedchickenMrSulko.png`, `friedchickenCoal.png`
2. **Parallax background** - Implement scrolling background layers
3. **Particle effects** - Muzzle flash, bullet trails, hit effects
4. **Combo text display** - Show DOUBLE_KILL, TRIPLE_KILL, QUADRA_KILL
5. **Score popups** - Show points earned on duck kills

### Phase 5: Audio & Monetization
1. **AudioManager.cs** - Sound effects and music
2. **AdMob Integration** - Banner ads, interstitials, rewarded videos
3. **Game Over Screen** - Results display, restart button
4. **Pause Menu** - Pause overlay with resume/quit options

### Reference the Original Game
All game mechanics are defined in the original Java implementation:
- Duck behavior: `ShooterBgame/src/com/example/model/Duck.java`
- Weapon system: `ShooterBgame/src/com/example/model/weapons/`
- Game controller: `ShooterBgame/src/com/example/controller/BaseGameController.java`
- Constants: `ShooterBgame/src/com/example/Constants.java`

### Current Architecture
All core systems are ready:
- ✅ GameManager - Score, lives, difficulty, events
- ✅ SceneController - Scene loading
- ✅ ObjectPool - Performance optimization
- ✅ Constants - All game configuration
- ✅ Menu and HUD - UI controllers
