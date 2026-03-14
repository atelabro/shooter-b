# Movement Reference

This file documents the duck movement options currently available in the Unity remake.

Primary code references:
- `ShooterBRemake/Assets/Scripts/Utils/Constants.cs`
- `ShooterBRemake/Assets/Scripts/Models/Ducks/Duck.cs`
- `ShooterBRemake/Assets/Scripts/Models/Campaign/StageSpawnConfig.cs`
- `ShooterBRemake/Assets/Scripts/Models/Campaign/DuckPatternConfig.cs`

## How movement is selected

Each spawned duck receives a `DuckPathProjection`.

- In campaign content, this is set directly on `StageSpawnConfig` entries or `DuckPatternConfig` entries.
- `Random` does not use a predefined curve. It uses the older movement pattern system and keeps changing between:
  - `GoStraight`
  - `GoTop`
  - `GoBottom`
- All other values are authored paths handled directly in `Duck.FixedUpdate()`.

## Movement patterns used by `Random`

These are not `DuckPathProjection` values. They are the internal movement states used only when the projection is `Random`.

### `GoStraight`
- Moves right while damping vertical movement back toward zero.
- Produces a flatter, steadier path.

### `GoTop`
- Moves right while accelerating upward.
- Used to create climbing motion.

### `GoBottom`
- Moves right while accelerating downward.
- Used to create diving motion.

## `DuckPathProjection` reference

### `Random`
- Starts at a random position near the left side.
- Continuously changes between `GoStraight`, `GoTop`, and `GoBottom`.
- Good for normal ducks when you want loose, reactive movement instead of a strict authored path.

### `Straight`
- Starts on the selected lane.
- Moves directly from left to right with no vertical motion.

### `BezierMountain`
- Follows a cubic Bezier arc from lower-left to lower-right.
- Climbs toward the top of the screen in the middle, then descends.

### `BezierValley`
- Follows a cubic Bezier arc from upper-left to upper-right.
- Dips toward the bottom of the screen in the middle, then rises.

### `DiagonalRise`
- Starts low on the left.
- Travels diagonally upward until exiting on the right.

### `DiagonalFall`
- Starts high on the left.
- Travels diagonally downward until exiting on the right.

### `SinWave`
- Starts on the selected lane.
- Travels right while oscillating vertically with moderate amplitude.

### `SinWaveBig`
- Same basic behavior as `SinWave`.
- Uses a much larger vertical amplitude, filling more of the screen height.

### `ZigZagTopFirst`
- Starts on the selected lane.
- Snaps through a four-segment zigzag.
- First major move is toward the top of the screen.

### `ZigZagBottomFirst`
- Starts on the selected lane.
- Snaps through a four-segment zigzag.
- First major move is toward the bottom of the screen.

### `SinWaveStartDown`
- Same family as `SinWave`.
- Begins its oscillation by moving downward first instead of upward first.

### `BounceMid`
- Starts on the selected lane.
- Pushes forward into the screen, retreats partway, then commits to the right exit.
- This is the only non-boss path that visibly reverses horizontal direction mid-path.

### `DiagonalV`
- Starts low on the left.
- Climbs to the top midpoint, then drops back down before exiting right.
- Forms a `^`-shaped traversal over time.

### `DiagonalInverseV`
- Starts high on the left.
- Drops to the bottom midpoint, then rises again before exiting right.
- Forms a `V`-shaped traversal over time.

## Boss paths

These were added for boss fights that should:
- enter once from the left
- remain on-screen while performing the pattern
- leave only on the right

### `BossCenterWeave`
- Enters from the left on the chosen lane.
- Moves into the center-left to center region.
- Performs repeated vertical weaving in the middle of the screen.
- Finishes by drifting out on the right.
- Good default boss motion when you want readability and sustained pressure.

### `BossFigureEight`
- Enters from the left and reaches a central staging area.
- Performs a broad figure-8 in the middle of the screen.
- Exits on the right after the loop completes.
- Good when you want the boss to occupy multiple firing angles without leaving the arena.

### `BossCornerTraverse`
- Enters from the left on the chosen lane.
- Pulls toward one corner-side region first.
- Traverses diagonally across the screen toward the opposite vertical side.
- Exits on the right.
- Good for bosses that should feel more territorial and sweep across the play space.

## Authoring notes

Use these paths in:
- `StageSpawnConfig.spawnSequence`
- `WaveConfig.spawnSequence`
- `DuckPatternConfig.entries`

Fields involved:
- `startLane`: picks the initial vertical lane or center reference for the path
- `pathProjection`: chooses the movement type
- `speedMultiplier`: scales how quickly the path is traversed

Practical guidance:
- Use `Straight`, `DiagonalRise`, `DiagonalFall`, and `Random` for basic enemies.
- Use `BezierMountain`, `BezierValley`, `SinWave`, and `ZigZag*` for more readable authored patterns.
- Use the `Boss*` paths for large enemies that need screen presence instead of quick fly-bys.
