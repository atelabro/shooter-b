# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a remake of "Shooter B", a duck hunting game originally built for Android around 2010 using AndEngine and Box2D physics. The original implementation is preserved in `ShooterBgame/` for reference, and we are rebuilding it in Unity (C#) in the `ShooterBRemake/` directory.

## Original Game Reference

The legacy code in `ShooterBgame/` provides the game design reference:

### Core Game Mechanics
- Duck hunting shooter with physics-based gameplay
- 7 different weapon types: Rifle, Cabirne, Beretta, MrSulko, LaserGun, TeslaGun, PiranhaGun
- Each weapon has unique bullet behavior and firing mechanics
- Lives system (starts with 3 lives, bonus life every 50 birds killed)
- Scoring with multiplier and combo system (DOUBLE_KILL, TRIPLE_KILL, QUADRA_KILL)
- High score tracking per game mode
- Two game modes: Arcade and Normal

### Duck Behavior
- Three movement patterns: GoTop, GoBottom, GoStraight
- Animated sprites with physics bodies
- Random spawn positions with timed intervals
- Collision detection with bullets

### Visual Elements
- Parallax background layers
- Animated duck sprites with death animations
- Weapon switching UI
- Score/lives/multiplier display
- Pause and game over screens
- Loading screen with progress bar

### Technical Patterns from Original
- Singleton managers for resources, scenes, game state
- Scene-based architecture (Splash, Menu, Game, Loading, Exit)
- Physics integration with Box2D
- Resource loading/unloading per scene
- SharedPreferences for high score persistence

## Development Guidelines

When working on the remake:

1. Reference the original implementation in `ShooterBgame/src/com/atanas/` to understand:
   - Game mechanics in `controller/BaseGameController.java`
   - Duck behavior in `model/Duck.java`
   - Weapon system in `model/weapons/` and `model/bullets/`
   - Scene flow in `manager/SceneManager.java`

2. The new implementation should preserve core gameplay while modernizing:
   - Same weapon types and mechanics
   - Same scoring and lives system
   - Same duck movement patterns
   - Improved graphics and effects

3. Key constants from `ShooterBgame/src/com/atanas/Constants.java`:
   - Original camera: 800x480
   - 7 weapon types
   - Lives: 3 starting lives
   - Bonus life every 50 birds
   - Z-index range: 0-20

## Unity Project Structure

The remake is located in `ShooterBRemake/`:

```
ShooterBRemake/
├── Assets/
│   ├── Scenes/              - Unity scenes (Main, Menu, Game, etc.)
│   ├── Scripts/
│   │   ├── Controllers/     - Game logic controllers
│   │   ├── Managers/        - Singleton managers (GameManager, SceneManager, etc.)
│   │   ├── Models/
│   │   │   ├── Ducks/      - Duck behavior scripts
│   │   │   ├── Weapons/    - Weapon scripts
│   │   │   └── Bullets/    - Bullet behavior scripts
│   │   ├── UI/             - UI controllers and components
│   │   └── Utils/          - Helper scripts and utilities
│   ├── Sprites/            - 2D textures and sprite sheets
│   ├── Audio/              - Sound effects and music
│   └── Prefabs/            - Reusable game objects
├── ProjectSettings/        - Unity project configuration
└── Packages/              - Unity package dependencies
```

## Unity Development Commands

Unity primarily uses the Editor UI, but useful terminal commands:

```bash
# Navigate to Unity project
cd ShooterBRemake

# Open project in Unity (if Unity Hub is installed)
open -a "Unity Hub" .

# Build for Android (requires Unity CLI, after initial setup in Editor)
# Unity build commands typically done through Editor: File > Build Settings
```

## Unity Architecture for This Game

### Managers (Singleton Pattern)
- **GameManager**: Core game state, score, lives, difficulty
- **SceneManager**: Scene transitions (Unity has built-in SceneManager, wrap it)
- **ResourceManager**: Asset loading/unloading, object pooling
- **AudioManager**: Sound effects and music playback
- **AdManager**: AdMob integration for monetization

### Game Flow
1. SplashScene - Initial loading
2. MenuScene - Mode selection (Arcade/Normal)
3. GameScene - Main gameplay
4. GameOverScene - Results and restart

### Physics
- Use Unity's built-in 2D Physics (Rigidbody2D, Collider2D)
- Duck spawning with physics bodies
- Bullet collision detection

### Mobile Controls
- Touch input for shooting (Input.GetTouch or new Input System)
- UI buttons for weapon switching
- Pause button

### Monetization Setup
- AdMob SDK integration
- Banner ads in menu
- Interstitial ads on game over
- Rewarded video for extra lives

## Key Unity Concepts for This Project

- **Prefabs**: Create prefabs for each duck type, weapon, bullet
- **Object Pooling**: Reuse bullet objects instead of instantiate/destroy
- **Coroutines**: For timed duck spawning and combo timers
- **ScriptableObjects**: For weapon configurations (damage, fire rate, bullet type)
- **PlayerPrefs**: For high score persistence (like SharedPreferences)
- **Sorting Layers**: Replicate z-index system (0-20 from original)

## Development Workflow

1. Create game objects and prefabs in Unity Editor
2. Write C# scripts in preferred IDE (VS Code, Rider, Visual Studio)
3. Test in Unity Editor Play mode
4. Build to Android/iOS for device testing
5. Iterate based on performance and gameplay feel

## Current Status

### Completed (Phase 1 - Core Foundation)
- ✅ Constants.cs - All game configuration and enums
- ✅ GameManager.cs - Singleton with state management, events, difficulty progression
- ✅ SceneController.cs - Scene loading wrapper
- ✅ ObjectPool.cs - Generic pooling system for performance
- ✅ MenuController.cs - Menu UI with Play/Quit buttons
- ✅ GameController.cs - Game scene controller with camera setup
- ✅ GameHUD.cs - In-game HUD (score, lives, multiplier, buttons)

### Setup Required
See `ShooterBRemake/SETUP_INSTRUCTIONS.md` for detailed Unity Editor setup steps.
You need to manually create MenuScene and GameScene in Unity Editor and link UI elements.

### Next Steps

1. Test basic game flow (Menu → Game → Menu)
2. Implement duck system (spawning, movement, AI)
3. Add shooting mechanics and input handling
4. Create weapon system (7 weapons)
5. Build bullet physics and collision
6. Add sound effects and visual effects
7. Integrate AdMob for monetization
8. Polish and optimize for mobile
