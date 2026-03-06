# New Duck Movement System

## Problem

The current duck movement randomly switches between three patterns (GoStraight, GoTop, GoBottom) every 20 frames using probability weights. This produces chaotic, unpredictable movement. The goal is to replace this with explicitly assigned per-duck movement paths that are predictable, choreographable via spawn config, and constrained to the safe screen area.

Scope: campaign mode only. Arcade keeps existing random-weight behavior.

---

## New Paths

### Straight lanes (8 total)

The safe play area (between header and footer) is divided into 8 equal horizontal bands.
Each `Straight_N` path keeps the duck flying at a fixed Y for that band.

```
Straight_8  (top band)
Straight_7
Straight_6
Straight_5
Straight_4
Straight_3
Straight_2
Straight_1  (bottom band)
```

### Bezier arcs (2)

**BezierMountain** - enters bottom-left, peaks at center-top, exits bottom-right:
```
  screen top  |           /-----\          |
              |          /       \         |
              |         /         \        |
  screen bot  |--------/           \-------|
              spawn                        exit
```

**BezierValley** - enters top-left, dips to center-bottom, exits top-right:
```
  screen top  |--------\           /-------|
              |         \         /        |
              |          \       /         |
  screen bot  |           \-----/          |
              spawn                        exit
```

All paths are computed relative to screen bounds (header and footer safe margins are respected).

---

## Files to Change

| File | Change |
|---|---|
| `Assets/Scripts/Utils/Constants.cs` | Add `DuckPathType` enum |
| `Assets/Scripts/Models/Campaign/StageSpawnConfig.cs` | Add `pathType` field to `SpawnEntry` |
| `Assets/Scripts/Models/Ducks/Duck.cs` | Path-following movement modes |
| `Assets/Scripts/Controllers/CampaignDuckSpawner.cs` | Pass `pathType` from entry into `Duck.Initialize()` |

---

## Step 1 - Constants.cs: DuckPathType enum

Add inside the `Constants` class:

```csharp
public enum DuckPathType
{
    Random,         // existing weight-based behavior (default, value = 0)
    Straight_1,     // horizontal lane 1 (lowest, near bottom)
    Straight_2,
    Straight_3,
    Straight_4,
    Straight_5,
    Straight_6,
    Straight_7,
    Straight_8,     // lane 8 (highest, near top)
    BezierMountain, // enters bottom-left, peaks center-top, exits bottom-right
    BezierValley,   // enters top-left, dips center-bottom, exits top-right
}
```

---

## Step 2 - StageSpawnConfig.cs: pathType in SpawnEntry

```csharp
[Serializable]
public struct SpawnEntry
{
    public Constants.DuckType duckType;
    [Tooltip("Seconds to wait after the previous spawn before spawning this duck")]
    public float delay;
    [Tooltip("Movement path for this duck. Random uses weight-based pattern switching.")]
    public Constants.DuckPathType pathType;
}
```

Default enum value is `Random` (int 0), so all existing .asset spawn configs are backward compatible.

---

## Step 3 - Duck.cs: Path-following movement

### New fields

```csharp
private Constants.DuckPathType currentPathType;
private Vector2 bezierP0, bezierP1, bezierP2, bezierP3;
private float pathProgress; // 0..1 across screen width for Bezier paths
```

### Initialize() signature

Add `pathType` before the existing weight parameters:

```csharp
public void Initialize(
    Constants.DuckType type, int difficulty, Vector2 startPosition,
    float boundTop, float boundBottom, float boundRight, float boundLeft,
    Sprite[] typeAliveFrames,
    Constants.DuckPathType pathType = Constants.DuckPathType.Random,
    float goStraightWeight = 0.4f, float goTopWeight = 0.3f, float goBottomWeight = 0.3f)
```

Inside Initialize, after storing bounds:

- Store `currentPathType = pathType`
- Reset `pathProgress = 0f`
- **Straight_N**: override `startPosition.y` to the lane center Y (see below)
- **Bezier**: compute 4 control points relative to bounds (see below)
- **Random**: unchanged, call `SelectRandomPattern()` as before

### Straight lane Y (inside Initialize)

```csharp
float safeHeight = screenTop - screenBottom;
float laneHeight = safeHeight / 8f;
int laneIndex = (int)pathType - (int)Constants.DuckPathType.Straight_1; // 0..7
startPosition.y = screenBottom + laneHeight * (laneIndex + 0.5f);
```

### Bezier control points (inside Initialize)

```csharp
float w = screenRight - screenLeft;
float margin = (screenTop - screenBottom) * 0.05f;

// BezierMountain
bezierP0 = new Vector2(screenLeft,           screenBottom + margin);
bezierP1 = new Vector2(screenLeft + w*0.35f, screenTop - margin);
bezierP2 = new Vector2(screenRight - w*0.35f, screenTop - margin);
bezierP3 = new Vector2(screenRight,           screenBottom + margin);

// BezierValley
bezierP0 = new Vector2(screenLeft,           screenTop - margin);
bezierP1 = new Vector2(screenLeft + w*0.35f, screenBottom + margin);
bezierP2 = new Vector2(screenRight - w*0.35f, screenBottom + margin);
bezierP3 = new Vector2(screenRight,           screenTop - margin);
```

### FixedUpdate

```csharp
private void FixedUpdate()
{
    if (isDead) return;
    AnimateAliveSprite();

    if (currentPathType == Constants.DuckPathType.Random)
    {
        patternChangeCounter++;
        if (patternChangeCounter >= PATTERN_CHANGE_FRAMES)
        {
            patternChangeCounter = 0;
            SelectRandomPattern();
        }
        ApplyMovement();
        EnforceBoundaries();
        rb.linearVelocity = velocity;
    }
    else if (IsStraightPath(currentPathType))
    {
        velocity.x = speed;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        if (transform.position.x > screenRight)
            DuckPassedScreen();
    }
    else
    {
        AdvanceBezierPath();
    }
}
```

### Helper methods to add

```csharp
private bool IsStraightPath(Constants.DuckPathType p)
    => p >= Constants.DuckPathType.Straight_1 && p <= Constants.DuckPathType.Straight_8;

private void AdvanceBezierPath()
{
    float screenWidth = screenRight - screenLeft;
    pathProgress += (speed * Time.fixedDeltaTime) / screenWidth;
    if (pathProgress >= 1f)
    {
        DuckPassedScreen();
        return;
    }
    Vector2 pos = CubicBezier(bezierP0, bezierP1, bezierP2, bezierP3, pathProgress);
    rb.MovePosition(new Vector3(pos.x, pos.y, transform.position.z));
}

private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
{
    float u = 1f - t;
    return u*u*u*p0 + 3f*u*u*t*p1 + 3f*u*t*t*p2 + t*t*t*p3;
}
```

Bezier and Straight paths skip `EnforceBoundaries()`. The path control points include the safe margin so ducks never exit the header/footer area. Existing `EnforceBoundaries()` is unchanged and still used for Random mode.

---

## Step 4 - CampaignDuckSpawner.cs

In `SpawnDuck()`, pass `entry.pathType` as the new argument:

```csharp
duck.Initialize(
    entry.duckType,
    GameManager.Instance.Difficulty,
    spawnPosition,
    boundTop, boundBottom, boundRight, boundLeft,
    frames,
    entry.pathType,           // new
    config.weightGoStraight,
    config.weightGoTop,
    config.weightGoBottom
);
```

No other changes needed in CampaignDuckSpawner.

---

## No changes needed

- `DuckSpawner.cs` (arcade) - calls Initialize without pathType, default `Random` applies automatically
- Existing `.asset` spawn configs - `pathType` field defaults to `Random` (int 0), backward compatible
- `StageConfig.cs`, `CampaignGameController.cs` - unaffected

---

## Verification

1. Open Unity, load CampaignGameScene
2. Select `StageSpawnConfig_Skopje_0.asset` - each SpawnEntry should now show a `Path Type` dropdown
3. Set a few entries to `Straight_3`, `BezierMountain`, `BezierValley`
4. Play the Skopje Stage 0 from the editor
5. Straight ducks must fly perfectly horizontally at the correct vertical band
6. BezierMountain ducks must follow the mountain arc (bottom -> peak -> bottom)
7. BezierValley ducks must follow the valley arc (top -> dip -> top)
8. All ducks must stay within safe area margins
9. Dead ducks on any path must still fall with gravity and clean up normally
10. Stages with no pathType set must behave identically to before

---

## Future path ideas (after testing)

- `DiagonalRise` / `DiagonalFall` - straight diagonal lines
- `SinWave` - oscillating up/down while crossing screen
- `BezierS` - S-curve from bottom-left to top-right
- `ZigZag` - sharp direction changes at fixed X intervals
