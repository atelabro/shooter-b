# Duck Multi-Part (Skeletal) Animation Design

Date: 2026-04-28

## Overview

Add a procedural multi-part animation system for duck skins where the torso and wings are separate sprites animated independently. The system coexists with the existing frame-flip animation — ducks opt in per type. All existing skins continue working unchanged.

---

## Goals

- Torso and wings rendered as separate child GameObjects
- Wings flap via sine-wave procedural animation (no Unity Animator clips required)
- Each skin can configure flap speed, amplitude, phase offset, pivot point, and optional torso bob
- One wing sprite can be mirrored for the other side, or two distinct sprites can be used
- Torso child object is open for future animation (Animator, shader, additional scripts)
- Zero changes to existing spawners, movement, death, hit puff, or health bar systems

---

## New Files

### `DuckPartConfig.cs`

```
[Serializable]
struct DuckPartConfig
    Sprite  torsoSprite
    Sprite  wingSprite                  // primary; mirrored for right if rightWingSprite is null
    Sprite  rightWingSprite             // optional separate right wing
    Vector2 leftWingPivotOffset         // local-space offset from duck root to left shoulder pivot
    Vector2 rightWingPivotOffset        // local-space offset from duck root to right shoulder pivot
    float   flapSpeed                   // cycles per second
    float   flapAmplitude               // max rotation in degrees
    float   phaseOffset                 // time offset between left and right wing (0.5 = alternating)
    float   torsoBobAmount              // world units; 0 disables bob
    float   torsoBobSpeed               // cycles per second for torso bob
```

### `DuckPartLibrary.cs`

ScriptableObject. Stores a list of `(DuckType, DuckPartConfig)` entries.

- `GetConfig(DuckType type) -> DuckPartConfig?` — returns null if type has no entry (triggers frame-flip fallback in Duck.cs)
- `[CreateAssetMenu]` so it can be created from the Project window

### `DuckPartAnimator.cs`

MonoBehaviour. Always present on the duck prefab. Inactive (no-op) until `TryInitialize` is called with a type that has a registered config.

**Fields:**
- `[SerializeField] DuckPartLibrary partLibrary` — assigned in prefab Inspector

**Runtime state (private):**
- `bool isActive`
- `GameObject torsoObject, leftWingPivot, rightWingPivot`
- `SpriteRenderer torsoRenderer, leftWingRenderer, rightWingRenderer`
- `DuckPartConfig config`
- `float animTime`

**Public API:**

```
bool TryInitialize(DuckType type, SpriteRenderer rootRenderer, int sortingOrder)
```
- Looks up config; returns false if none found (frame-flip mode remains)
- Hides root SpriteRenderer
- Creates child GameObjects: Torso, LeftWingPivot/LeftWing, RightWingPivot/RightWing
- RightWing flips X on its SpriteRenderer if mirrored from single sprite
- Sets sorting orders: right wing = sortingOrder - 1 (behind torso), torso = sortingOrder, left wing = sortingOrder + 1 (in front of torso)
- Resets animTime to 0

```
Sprite GetNormalizationSprite()
```
- Returns torsoSprite so Duck.cs can compute scale normalization from the correct sprite dimensions

```
void Tick(float deltaTime)
```
- Guards on isActive
- Advances animTime
- Left wing pivot Z rotation: Sin(animTime * flapSpeed * 2PI) * flapAmplitude
- Right wing pivot Z rotation: Sin((animTime + phaseOffset) * flapSpeed * 2PI) * flapAmplitude
- Torso local Y bob: Sin(animTime * torsoBobSpeed * 2PI) * torsoBobAmount (skipped if torsoBobAmount == 0)

```
void PrepareForDeath()
```
- Destroys child objects
- Re-enables root SpriteRenderer
- Sets isActive = false
- Death sprite and fall physics proceed unchanged via existing Duck.cs code

```
void ResetState()
```
- Same as PrepareForDeath but called on pool return (not kill)

---

## Changes to Existing Files

### `Duck.cs`

1. Add `[SerializeField] private DuckPartAnimator partAnimator`
2. In `Initialize()`:
   - Call `partAnimator.TryInitialize(type, spriteRenderer, sortingOrder)`
   - If returns true, add an optional `Sprite overrideSprite` parameter to `ApplyNormalizedScale()` and call it with `partAnimator.GetNormalizationSprite()` so scale is computed from torso dimensions, not the disabled root sprite
3. In `AnimateAliveSprite()`: early-out if `partAnimator.IsActive`
4. In `FixedUpdate()`: call `partAnimator.Tick(Time.fixedDeltaTime)` (guarded by isActive internally)
5. In `ReceiveDamage()` on kill: call `partAnimator.PrepareForDeath()` before `Invoke(ApplyDeathState)`
6. In `ReturnToPool()`: call `partAnimator.ResetState()`

### Spawners (`DuckSpawner.cs`, `CampaignDuckSpawner.cs`)

No changes. `DuckPartLibrary` reference is stored on `DuckPartAnimator` in the prefab.

---

## Runtime Prefab Hierarchy (parts mode)

```
Duck (root)
├── SpriteRenderer            disabled while parts active; re-enabled on death/reset
├── Duck.cs
├── DuckPartAnimator.cs
├── [Torso]                   created at TryInitialize, destroyed at PrepareForDeath/ResetState
│   └── SpriteRenderer
├── [LeftWingPivot]           empty transform at left shoulder world position
│   └── [LeftWing]            offset child; rotation applied to pivot parent
│       └── SpriteRenderer
└── [RightWingPivot]
    └── [RightWing]
        └── SpriteRenderer    flipX = true if mirrored from single wing sprite
```

---

## What Does Not Change

- Frame-flip animation path in Duck.cs (all existing skins unaffected)
- Death sprite system
- Hit puff (uses collider-normalized sizing on root)
- Health bar
- All movement patterns
- DuckFrameLibrary
- DuckSpawner and CampaignDuckSpawner

---

## Constraints and Assumptions

- Child objects are created fresh per Initialize and destroyed per Reset. With ~10-20 active ducks and pooled reuse, allocation cost is negligible.
- `DuckPartAnimator` is a required component on the duck prefab. If `partLibrary` is null or the duck type has no entry, it is a no-op.
- The system is opt-in per duck type. Migration of existing skins happens incrementally over time.
- Torso child object is intentionally open: future work (Animator clips, procedural scripts, shaders) can be added to it without touching this system.
