# Shooter B Unity Remake - Implementation Roadmap

## Current Status

✅ **Completed Features:**
- Duck spawning system with object pooling
- Duck movement AI (GoTop/GoBottom/GoStraight patterns)
- Basic GameManager (score, lives, difficulty, multiplier)
- Scene management (Menu → Game transitions)
- Basic HUD (Score, Lives, Multiplier display)
- Background rendering (single layer)
- Camera setup at Z=-100
- Constants and configuration

## Phase 1: Core Shooting Mechanics (NEXT)

**Goal:** Make the game playable with basic shooting

### 1.1 Input Controller
- Detect mouse click / touch input
- Convert screen position to world position
- Trigger shot at touch location

### 1.2 Rifle Weapon (Default)
- Properties: 2 bullets, 0.3s fire delay, 0.8s refill
- Bullet spawning at click position
- Bullet physics: kinematic body, moves toward target
- Bullet lifecycle: spawn → travel → bang → dispose

### 1.3 Bullet System
- RifleBullet class with CircleCollider2D
- Size morphing during travel (60px → 20px radius)
- Bang effect at 15px from target
- Auto-dispose on impact or off-screen

### 1.4 Collision Detection
- Check bullet-duck overlap using CircleCollider2D
- OnHit() triggers on duck
- Duck death animation (turn gray, fall with gravity)
- Award points to GameManager

## Phase 2: All Weapons

**Goal:** Complete weapon variety

### 2.1 Weapon Base Class
- Abstract Weapon class with common properties
- Shoot(), Refill(), DisplayWeapon() methods
- Ammo tracking and reload timers

### 2.2 Seven Weapon Implementations

**Rifle:**
- Bullets: 2, Delay: 0.3s, Refill: 0.8s
- Radius: 60→20px, Effective: 45px

**Cabirne (Sniper):**
- Bullets: 7, Delay: 0.2s, Refill: 0.6s
- Radius: 15→3px, Effective: 10px

**Beretta (Machine Gun):**
- Bullets: 27, Delay: 0.2s, Refill: 1.0s
- Radius: 8→2px, Effective: 6px
- No ACTION_DOWN check (continuous fire)

**MrSulko:**
- Bullets: 30, Delay: 0.1s, Refill: 0.2s
- Radius: 15→5px, Effective: 10px

**LaserGun:**
- Bullets: 100, Delay: 0.4s, Refill: 0.4s
- Radius: 15→5px, Effective: 10px

**TeslaGun:**
- Bullets: 5, Delay: 0.3s, Refill: 0.6s
- Radius: 30→15px, Effective: 24px
- AOE: 200px, Chain: 2 targets

**PiranhaGun:**
- Bullets: 10, Delay: 0.2s, Refill: 0.5s
- Radius: 40→10px, Effective: 20px
- 3 visual variants, 3x slower speed

### 2.3 Weapon Switching & HUD
- Weapon selection UI
- Ammo counter display in HUD footer
- Weapon icon display
- Reload progress indicator

## Phase 3: Visual Polish

**Goal:** Match original game aesthetics

### 3.1 Layered Background System
- **Back Layer** (4 options):
  - Yellow (backgroundBackLayerYellow.png)
  - Red (backgroundBackLayerRed.png) ✅ Currently using
  - New York (bckNewYork.png)
  - Paris (bckParis.png)

- **Cloud Layer:**
  - White clouds (parallax_background_layer_mid.png)
  - Parallax scrolling at different speed

- **Grass Layer** (2 options):
  - Green grass (grassGreen.png)
  - None

### 3.2 Particle Effects
- Bang cloud on bullet impact (BangCloudParticleSystem)
- Duck death particles (feathers/red parts)
- Weapon muzzle flash
- Explosion effects for bombs

### 3.3 Duck Death Animation
- 7 dead duck sprite variants per weapon
- Rotate and fall with gravity
- Gray tint on hit
- Auto-remove after 2 seconds

## Phase 4: Progression Systems

**Goal:** Add replay value

### 4.1 Multi-Kill Combo System
- Track kills within 1-second window
- Double Kill: 30 bonus points
- Triple Kill: 50 bonus points
- Quadra Kill: 100 bonus points
- Display combo text with animation
- Play combo sound effects

### 4.2 Difficulty Progression
- Bird count thresholds:
  - Difficulty 1-5: +1 every 10 birds
  - Difficulty 6-13: +1 every 15 birds
  - Difficulty 14-23: +1 every 23 birds
  - Difficulty 24-33: +1 every 52 birds
  - Difficulty 34+: +1 every 100 birds (max 35)

### 4.3 Multiplier System
- Formula: difficulty / 5
- Minimum: 1
- Maximum: 7 (at difficulty 35)
- Applied to all duck kills

### 4.4 High Score Persistence
- Save to PlayerPrefs
- Arcade mode only (Campaign has no global high score)
- Display in menu and game over screen

## Phase 4.5: Campaign Mode

**Goal:** Stage-based world travel progression with star ratings

### Story: The Duck Uprising
Ducks have gained intelligence and are taking over cities one by one. You are the last licensed
duck hunter hired by D.U.C.K. (Department of Urban Containment and Killing), a secret government
agency. Your mission: push the duck invasion back city by city before they reach the capital and
the world falls under duck control. Each city you liberate is a stage. The further you go, the
more organized and aggressive the ducks become.

### Concept
- Campaign is a world travel experience - the player moves between cities around the world
- Each city = one stage with a unique background, duck spawn pattern, and difficulty config
- Pressing Campaign in the menu opens the Campaign Map Scene (not the game directly)
- No global high score - progression is measured by stars earned per stage

### Campaign Map Scene (new scene: CampaignMapScene)
- Intermediate scene between MenuScene and GameScene
- Shows a world map or city list with all stages
- Each stage node shows: city name, locked/unlocked state, stars earned (0-3)
- Tapping an unlocked stage loads the GameScene configured for that stage
- Back button returns to MenuScene
- SceneController gets a new LoadCampaignMapScene() method

### Stage Star System
- Each stage awards 0-3 stars based on in-stage performance (score thresholds defined per stage)
- Stars persisted per stage: PlayerPrefs key pattern "Campaign_Stage_{N}_Stars"
- Minimum cumulative stars required to unlock next stage (defined per stage)
- Replaying a completed stage can improve the star rating

### Stage Definition (StageConfig ScriptableObject)
- stageIndex: int
- cityName: string (e.g. "Paris", "New York", "Tokyo")
- backgroundId: string (maps to a background asset)
- spawnPattern: enum or config (unique duck behavior per city)
- duckCountGoal: int (how many ducks to kill to finish the stage)
- starThreshold1/2/3: int (score required for 1, 2, or 3 stars)
- starsRequiredToUnlock: int (cumulative stars needed to access this stage)
- startingDifficulty: int (each city can start at a different difficulty)

### Planned Cities / Stages
The duck uprising started in rural areas and is spreading to major world cities. Stages escalate
in duck aggression, speed, and organization. D.U.C.K. briefings introduce each city with a short
flavor text before the stage starts.

- Stage 1: Countryside (the uprising begins - slow scattered ducks, tutorial pace, red/yellow background)
- Stage 2: Paris (ducks have taken the Eiffel Tower - medium pace, Paris background)
- Stage 3: New York (duck gridlock on Manhattan - faster ducks, New York background)
- Stage 4+: More cities TBD (Tokyo, London, Sydney, etc.)
- Final stage: The Capital (last stand - maximum difficulty, the Duck Commander boss?)

### Duck Spawn Patterns per City (SpawnPattern)
- Each stage config references a spawn pattern that controls:
  - Duck types probability weights (more rare ducks in harder cities)
  - Spawn timing overrides
  - Movement pattern distribution (GoStraight vs GoTop/GoBottom ratio)
  - Fleet size (single, double, fleet)

### CampaignProgressManager (new)
- Singleton, persists across scenes
- Tracks stars earned per stage in PlayerPrefs
- Exposes: GetStarsForStage(stageIndex), IsStageUnlocked(stageIndex), SaveStageStars(stageIndex, stars)
- Loaded stage config passed to GameManager on stage start
- Separate from GameManager high score logic

## Phase 5: Advanced Features

**Goal:** Complete feature parity with original

### 5.1 Bonus Elements
- Coins spawn on duck kills (random chance)
- Bombs spawn on duck kills (random chance)
- Pickup detection with collision
- Visual fly-in animation with text
- Sound effects on pickup

### 5.2 Bomb Mechanic
- Tap/click anywhere to activate bomb
- Explosion radius: 100px
- Kills all ducks in radius
- Limited bomb count (default 10)
- HUD display of bomb counter
- Particle explosion effect

### 5.3 Sound System (AudioManager)

**Weapon Sounds (14 files):**
- Shoot: shotgun.ogg, cabirne.ogg, machine.ogg, mrsulko.ogg, laser.ogg, tesla.ogg, piranha.ogg
- Reload: shotgunreload.ogg, sniperReload.ogg, mrsulkoReload.ogg, laserReload.ogg, teslaReload.ogg, piranhaReload.ogg

**Game Event Sounds:**
- Button click: click.ogg
- Duck death: quack1.ogg, quack2.ogg, quack3.ogg (random)
- Bomb: bomb.ogg
- Coin: coin.ogg
- Bonus: bum.ogg
- Box break: boxBreak.ogg
- Combos: doubleKill.ogg, tripleKill.ogg

### 5.4 Pause Menu
- Pause/Resume functionality
- Restart game option
- Return to menu option
- Settings (sound toggle)

## Phase 6: Mobile & Platform Features

**Goal:** Cross-platform deployment

### 6.1 Touch Input System
- Full touch gesture support
- Multi-touch for rapid firing
- Touch to shoot anywhere on screen

### 6.2 Android Build
- Package name: com.yourcompany.shooterb
- Min API: 24 (Android 7.0)
- Target API: 33
- IL2CPP backend, ARM64

### 6.3 iOS Build
- Bundle ID: com.yourcompany.shooterb
- Target iOS: 12.0+
- ARM64 architecture

### 6.4 AdMob Integration
- Banner ads on menu
- Interstitial on game over
- Rewarded video for extra life
- Test IDs initially, production IDs later

## Phase 7: Shop & Upgrades (Future)

**Goal:** Monetization and progression

### 7.1 Weapon Shop
- Purchase locked weapons with coins
- Upgrade weapon stats:
  - Increase bullet count
  - Reduce fire delay
  - Increase bullet radius
  - Reduce refill delay

### 7.2 Background Shop
- Purchase background combinations
- Preview before buying
- Persist selected background

### 7.3 Coin Economy
- Earn coins from duck kills
- Bonus coin rewards
- Persistent coin balance
- In-app purchases for coins (optional)

## Implementation Priority

**Immediate (Next 2-3 days):**
1. Fix current duck issues (flickering, speed, removal)
2. Implement basic shooting (Phase 1.1-1.4)
3. Add collision detection and scoring
4. [ ] Tesla chain targeting: ignore ducks outside visible camera bounds (on-screen ducks only)
5. Campaign Map Scene + CampaignProgressManager + StageConfig ScriptableObject
6. Wire Campaign button -> CampaignMapScene -> GameScene with stage config

**Short-term (Next week):**
4. Implement all 7 weapons (Phase 2)
5. Add weapon switching UI
6. Implement layered backgrounds (Phase 3.1)

**Medium-term (Next 2 weeks):**
7. Particle effects and polish (Phase 3.2-3.3)
8. Multi-kill combo system (Phase 4.1)
9. Sound effects (Phase 5.3)
10. Bomb mechanic (Phase 5.2)

**Long-term (Future):**
11. Shop systems (Phase 7)
12. Mobile deployment (Phase 6)
13. AdMob integration (Phase 6.4)

## Technical Debt & Cleanup

- Remove all debug logging when features stabilize
- Optimize object pooling for better performance
- Create proper sorting layers instead of using sortingOrder values
- Implement proper Input System (currently using legacy)
- Add unit tests for core mechanics
- Performance profiling and optimization

## Success Metrics

- 60 FPS on mid-range devices
- No memory leaks during extended gameplay
- All 7 weapons functional with unique feel
- Difficulty scales smoothly from 1-35
- High score persistence works correctly
- Touch input responsive (<50ms latency)
