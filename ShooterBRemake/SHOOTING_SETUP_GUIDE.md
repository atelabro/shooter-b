# Shooting System Setup Guide

## Overview
This guide explains how to set up the shooting mechanics in Unity Editor.

## Code Status
All scripts have been created and are ready to use:
- `InputController.cs` - Handles mouse/touch input
- `ShooterController.cs` - Manages weapons and shooting
- `Weapon.cs` - Base class for all weapons
- `Rifle.cs` - Default rifle weapon (2 bullets, 0.3s delay, 0.8s refill)
- `Bullet.cs` - Base bullet behavior (movement, morphing, collision)
- `RifleBullet.cs` - Rifle-specific bullet implementation

## Unity Editor Setup

### Step 1: Create Bullet Prefab

1. **Create Bullet GameObject:**
   - Hierarchy → Right-click → Create Empty → Name it "RifleBullet"
   - Position: (0, 0, -5) to match duck Z-plane

2. **Add Components to RifleBullet:**
   - Add Component → Rigidbody2D
     - Body Type: Kinematic
     - Gravity Scale: 0
   - Add Component → Circle Collider 2D
     - Is Trigger: ✓ (checked)
     - Radius: 0.3
   - Add Component → Sprite Renderer
     - Sprite: Create a simple circle sprite (white circle)
     - Color: Yellow or Orange
     - Sorting Layer: Default
     - Order in Layer: 15
   - Add Component → Scripts → RifleBullet

3. **Save as Prefab:**
   - Drag "RifleBullet" from Hierarchy to Project window → Assets/Prefabs/
   - Delete the instance from Hierarchy

### Step 2: Setup Game Scene

1. **Add Controllers to GameController GameObject:**
   - Select your existing "GameController" object in Hierarchy
   - Add Component → InputController
   - Add Component → ShooterController

2. **Configure InputController:**
   - Game Camera: Drag Main Camera from Hierarchy
   - Shooter Controller: Will auto-find, or drag ShooterController component

3. **Configure ShooterController:**
   - This will auto-create the Rifle weapon at runtime
   - No configuration needed initially

### Step 3: Setup Rifle Weapon (Runtime Creation)

The Rifle weapon is created automatically by ShooterController at runtime, but we need to assign the bullet prefab:

**Option A: Manual Assignment (Editor)**
1. Run the game once to create the Rifle GameObject
2. Stop the game
3. The Rifle will be created under ShooterController
4. Assign the RifleBullet prefab to Rifle's "Bullet Prefab" field

**Option B: Script Assignment (Recommended)**
Update `Rifle.cs` to load the prefab by name:

```csharp
protected override void Start()
{
    weaponName = "Rifle";
    maxBullets = 2;
    fireDelay = 0.3f;
    refillDelay = 0.8f;

    // Load bullet prefab
    bulletPrefab = Resources.Load<GameObject>("Prefabs/RifleBullet");

    base.Start();
}
```

Then move RifleBullet prefab to: `Assets/Resources/Prefabs/RifleBullet.prefab`

### Step 4: Create Simple Bullet Sprite (Temporary)

If you don't have a bullet sprite yet:

1. Create → 2D → Sprites → Circle
2. Name it "BulletSprite"
3. Use this for the Sprite Renderer on RifleBullet prefab

### Step 5: Test the System

1. **Play the game**
2. **Click anywhere on screen**
3. **Expected behavior:**
   - Console shows: "Shot fired at (x, y)"
   - Yellow/orange bullet spawns at click position
   - Bullet grows from large to small as it travels
   - Bullet triggers "bang" effect near target
   - If bullet hits duck, duck turns gray and falls
   - After 2 shots, weapon refills automatically

### Step 6: Debug Checklist

If shooting doesn't work, check:

1. **No bullets spawning:**
   - Check Console for "Shot fired" message
   - Verify RifleBullet prefab is assigned to Rifle
   - Check bulletPrefab is not null in Rifle component

2. **Bullets spawn but don't move:**
   - Verify Rigidbody2D is Kinematic
   - Check Bullet.cs Initialize() is being called
   - Look for "BULLET Initialized" in Console

3. **Bullets don't hit ducks:**
   - Verify ducks have CircleCollider2D
   - Check bullet Z position matches duck Z (-5)
   - Enable Gizmos in Game view to see collision radius

4. **Ammo doesn't refill:**
   - Check Console for "Rifle refilled" message
   - Verify refillDelay is set correctly (0.8s)

## Expected Console Output

When shooting works correctly:
```
[SHOOTER] ShooterController initialized with weapon: Rifle
[RIFLE] Initialized - Bullets: 2, Fire Delay: 0.3s, Refill: 0.8s
[INPUT] InputController initialized
[SHOOTER] Shot fired at (x, y). Ammo remaining: 1
[BULLET] Initialized at (x, y), target: (x, y), distance: d
[BULLET] Bang triggered at (x, y)
[BULLET] Hit duck at (x, y)
[DUCK] Pattern selected: GoTop (random: 2)
[WEAPON] Rifle refilled to 2 bullets
```

## Next Steps

After basic shooting works:
1. Create proper bullet sprites (different per weapon)
2. Add muzzle flash effects
3. Add sound effects for shooting
4. Implement remaining 6 weapons
5. Add bullet pooling for performance
6. Create weapon switching UI

## Troubleshooting

**Problem: Bullets spawn at (0,0,0)**
- Solution: InputController needs gameCamera assigned

**Problem: Can't shoot after first 2 bullets**
- Solution: Check Rifle refillDelay coroutine is running

**Problem: Ducks don't die when hit**
- Solution: Verify duck CircleCollider2D has same Z-plane as bullets (-5)

**Problem: Shooting feels laggy**
- Solution: Reduce fireDelay in Rifle (currently 0.3s)
