# Duck Multi-Part Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an opt-in procedural multi-part animation system (separate torso and wing sprites) for duck skins, coexisting with the existing frame-flip system.

**Architecture:** Three new files — `DuckPartConfig` (data struct), `DuckPartLibrary` (ScriptableObject registry), `DuckPartAnimator` (MonoBehaviour that owns all runtime part logic). `Duck.cs` gets minimal additions to delegate to `DuckPartAnimator`. Existing ducks are unaffected; `JAPANESE_MONK_DUCK` is the first skin to use the new system.

**Tech Stack:** Unity 6, C#, Unity 2D (SpriteRenderer, Rigidbody2D), ScriptableObjects

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartConfig.cs` | Serializable data struct for one duck's part animation config |
| Create | `ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartLibrary.cs` | ScriptableObject registry mapping DuckType to DuckPartConfig |
| Create | `ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartAnimator.cs` | MonoBehaviour: creates/destroys child part objects, drives sine-wave animation |
| Modify | `ShooterBRemake/Assets/Scripts/Utils/Constants.cs` | Add `JAPANESE_MONK_DUCK = 42` to DuckType enum and related switches |
| Modify | `ShooterBRemake/Assets/Scripts/Models/Ducks/Duck.cs` | Wire in DuckPartAnimator; modify ApplyNormalizedScale, AnimateAliveSprite, FixedUpdate, ReceiveDamage, ReturnToPool |

---

## Task 1: Add JAPANESE_MONK_DUCK to Constants.cs

**Files:**
- Modify: `ShooterBRemake/Assets/Scripts/Utils/Constants.cs`

- [ ] **Step 1: Add enum value**

In the `DuckType` enum, after `KYOTO_KIMONO_DUCK = 41`, add:

```csharp
KYOTO_KIMONO_DUCK = 41,
JAPANESE_MONK_DUCK = 42
```

- [ ] **Step 2: Add display name**

In `GetDuckDisplayName`, after `case DuckType.KYOTO_KIMONO_DUCK: return "Kyoto Kimono Duck";`, add:

```csharp
case DuckType.JAPANESE_MONK_DUCK: return "Japanese Monk Duck";
```

- [ ] **Step 3: Add debug name**

In `GetDuckDebugName`, after `case DuckType.KYOTO_KIMONO_DUCK: return "KYOTO_KIMONO_DUCK";`, add:

```csharp
case DuckType.JAPANESE_MONK_DUCK: return "JAPANESE_MONK_DUCK";
```

- [ ] **Step 4: Add points constant and GetPoints case**

In `DuckPoints`, after `public const int KYOTO_KIMONO_DUCK = 4;`, add:

```csharp
public const int JAPANESE_MONK_DUCK = 4;
```

In `DuckPoints.GetPoints`, after `case DuckType.KYOTO_KIMONO_DUCK: return KYOTO_KIMONO_DUCK;`, add:

```csharp
case DuckType.JAPANESE_MONK_DUCK: return JAPANESE_MONK_DUCK;
```

- [ ] **Step 5: Commit**

```bash
git add ShooterBRemake/Assets/Scripts/Utils/Constants.cs && git commit -m "feat: add JAPANESE_MONK_DUCK duck type"
```

---

## Task 2: Create DuckPartConfig.cs

**Files:**
- Create: `ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartConfig.cs`

- [ ] **Step 1: Create the file**

```csharp
using System;
using UnityEngine;

namespace ShooterB
{
    [Serializable]
    public struct DuckPartConfig
    {
        public Sprite torsoSprite;
        public Sprite wingSprite;
        public Sprite rightWingSprite;
        public Vector2 leftWingPivotOffset;
        public Vector2 rightWingPivotOffset;
        public float flapSpeed;
        public float flapAmplitude;
        public float phaseOffset;
        public float torsoBobAmount;
        public float torsoBobSpeed;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartConfig.cs && git commit -m "feat: add DuckPartConfig struct"
```

---

## Task 3: Create DuckPartLibrary.cs

**Files:**
- Create: `ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartLibrary.cs`

- [ ] **Step 1: Create the file**

```csharp
using System;
using UnityEngine;

namespace ShooterB
{
    [Serializable]
    public struct DuckPartEntry
    {
        public Constants.DuckType duckType;
        public DuckPartConfig config;
    }

    [CreateAssetMenu(fileName = "DuckPartLibrary", menuName = "ShooterB/Duck Part Library")]
    public class DuckPartLibrary : ScriptableObject
    {
        public DuckPartEntry[] entries;

        public bool TryGetConfig(Constants.DuckType type, out DuckPartConfig config)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].duckType == type)
                    {
                        config = entries[i].config;
                        return true;
                    }
                }
            }
            config = default;
            return false;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartLibrary.cs && git commit -m "feat: add DuckPartLibrary ScriptableObject"
```

---

## Task 4: Create DuckPartAnimator.cs

**Files:**
- Create: `ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartAnimator.cs`

- [ ] **Step 1: Create the file**

```csharp
using UnityEngine;

namespace ShooterB
{
    public class DuckPartAnimator : MonoBehaviour
    {
        [SerializeField] private DuckPartLibrary partLibrary;

        public bool IsActive => isActive;

        private bool isActive;
        private DuckPartConfig config;
        private float animTime;

        private GameObject torsoObject;
        private GameObject leftWingPivot;
        private GameObject rightWingPivot;

        public bool TryInitialize(Constants.DuckType type, SpriteRenderer rootRenderer, int sortingOrder)
        {
            if (partLibrary == null || !partLibrary.TryGetConfig(type, out config))
                return false;

            rootRenderer.enabled = false;

            int sortingLayerID = rootRenderer.sortingLayerID;

            torsoObject = new GameObject("Torso");
            torsoObject.transform.SetParent(transform, false);
            SpriteRenderer torsoRenderer = torsoObject.AddComponent<SpriteRenderer>();
            torsoRenderer.sprite = config.torsoSprite;
            torsoRenderer.sortingLayerID = sortingLayerID;
            torsoRenderer.sortingOrder = sortingOrder;

            leftWingPivot = new GameObject("LeftWingPivot");
            leftWingPivot.transform.SetParent(transform, false);
            leftWingPivot.transform.localPosition = new Vector3(config.leftWingPivotOffset.x, config.leftWingPivotOffset.y, 0f);
            GameObject leftWingObj = new GameObject("LeftWing");
            leftWingObj.transform.SetParent(leftWingPivot.transform, false);
            SpriteRenderer leftWingRenderer = leftWingObj.AddComponent<SpriteRenderer>();
            leftWingRenderer.sprite = config.wingSprite;
            leftWingRenderer.sortingLayerID = sortingLayerID;
            leftWingRenderer.sortingOrder = sortingOrder + 1;

            rightWingPivot = new GameObject("RightWingPivot");
            rightWingPivot.transform.SetParent(transform, false);
            rightWingPivot.transform.localPosition = new Vector3(config.rightWingPivotOffset.x, config.rightWingPivotOffset.y, 0f);
            GameObject rightWingObj = new GameObject("RightWing");
            rightWingObj.transform.SetParent(rightWingPivot.transform, false);
            SpriteRenderer rightWingRenderer = rightWingObj.AddComponent<SpriteRenderer>();
            rightWingRenderer.sprite = config.rightWingSprite != null ? config.rightWingSprite : config.wingSprite;
            rightWingRenderer.flipX = config.rightWingSprite == null;
            rightWingRenderer.sortingLayerID = sortingLayerID;
            rightWingRenderer.sortingOrder = sortingOrder - 1;

            animTime = 0f;
            isActive = true;
            return true;
        }

        public Sprite GetNormalizationSprite()
        {
            return config.torsoSprite;
        }

        public void Tick(float deltaTime)
        {
            if (!isActive) return;

            animTime += deltaTime;

            if (leftWingPivot != null)
            {
                float leftAngle = Mathf.Sin(animTime * config.flapSpeed * Mathf.PI * 2f) * config.flapAmplitude;
                leftWingPivot.transform.localRotation = Quaternion.Euler(0f, 0f, leftAngle);
            }

            if (rightWingPivot != null)
            {
                float rightAngle = Mathf.Sin((animTime + config.phaseOffset) * config.flapSpeed * Mathf.PI * 2f) * config.flapAmplitude;
                rightWingPivot.transform.localRotation = Quaternion.Euler(0f, 0f, rightAngle);
            }

            if (torsoObject != null && config.torsoBobAmount > 0f)
            {
                float bobY = Mathf.Sin(animTime * config.torsoBobSpeed * Mathf.PI * 2f) * config.torsoBobAmount;
                torsoObject.transform.localPosition = new Vector3(0f, bobY, 0f);
            }
        }

        public void PrepareForDeath(SpriteRenderer rootRenderer)
        {
            DestroyParts();
            if (rootRenderer != null)
                rootRenderer.enabled = true;
            isActive = false;
        }

        public void ResetState(SpriteRenderer rootRenderer)
        {
            DestroyParts();
            if (rootRenderer != null)
                rootRenderer.enabled = true;
            isActive = false;
        }

        private void DestroyParts()
        {
            if (torsoObject != null) Destroy(torsoObject);
            if (leftWingPivot != null) Destroy(leftWingPivot);
            if (rightWingPivot != null) Destroy(rightWingPivot);
            torsoObject = null;
            leftWingPivot = null;
            rightWingPivot = null;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add ShooterBRemake/Assets/Scripts/Models/Ducks/DuckPartAnimator.cs && git commit -m "feat: add DuckPartAnimator MonoBehaviour"
```

---

## Task 5: Modify Duck.cs

**Files:**
- Modify: `ShooterBRemake/Assets/Scripts/Models/Ducks/Duck.cs`

- [ ] **Step 1: Add partAnimator field**

In the `[Header("Components")]` section (around line 53), after `private Animator animator;`, add:

```csharp
private Animator animator;
private DuckPartAnimator partAnimator;
```

- [ ] **Step 2: Fetch partAnimator in Awake()**

In `Awake()` (around line 98), after `animator = GetComponent<Animator>();`, add:

```csharp
animator = GetComponent<Animator>();
partAnimator = GetComponent<DuckPartAnimator>();
```

- [ ] **Step 3: Wire TryInitialize into Initialize()**

In `Initialize()`, replace the existing `ApplyNormalizedScale();` call (line 170) with:

```csharp
bool usingParts = partAnimator != null && partAnimator.TryInitialize(
    duckType,
    spriteRenderer,
    spriteRenderer != null ? spriteRenderer.sortingOrder : Constants.SORTING_LAYER_DUCKS);
ApplyNormalizedScale(usingParts ? partAnimator.GetNormalizationSprite() : null);
```

- [ ] **Step 4: Update ApplyNormalizedScale signature**

Replace the existing `ApplyNormalizedScale()` method body:

```csharp
private void ApplyNormalizedScale()
{
    ApplySpriteScale(spriteRenderer != null ? spriteRenderer.sprite : null, GetTypeSizeMultiplier(duckType) * spawnSizeMultiplier);
}
```

With:

```csharp
private void ApplyNormalizedScale(Sprite overrideSprite = null)
{
    Sprite sprite = overrideSprite != null ? overrideSprite : (spriteRenderer != null ? spriteRenderer.sprite : null);
    ApplySpriteScale(sprite, GetTypeSizeMultiplier(duckType) * spawnSizeMultiplier);
}
```

- [ ] **Step 5: Early-out AnimateAliveSprite when parts are active**

At the top of `AnimateAliveSprite()`, after the existing null check, add:

```csharp
private void AnimateAliveSprite()
{
    if (partAnimator != null && partAnimator.IsActive) return;
    if (aliveFrames == null || aliveFrames.Length <= 1 || spriteRenderer == null)
    {
        return;
    }
    // ... rest unchanged
```

- [ ] **Step 6: Call Tick in FixedUpdate**

In `FixedUpdate()`, after the `AnimateAliveSprite();` call (line 377), add:

```csharp
AnimateAliveSprite();
if (partAnimator != null) partAnimator.Tick(Time.fixedDeltaTime);
```

- [ ] **Step 7: Call PrepareForDeath on kill**

In `ReceiveDamage()`, before `ShowHitPuff();` (line 1046), add:

```csharp
if (partAnimator != null) partAnimator.PrepareForDeath(spriteRenderer);
ShowHitPuff();
```

- [ ] **Step 8: Call ResetState on pool return**

In `ReturnToPool()`, after the two `CancelInvoke` calls (lines 1072-1073), add:

```csharp
CancelInvoke(nameof(ApplyDeathState));
CancelInvoke(nameof(ReturnToPool));
if (partAnimator != null) partAnimator.ResetState(spriteRenderer);
```

- [ ] **Step 9: Add JAPANESE_MONK_DUCK to GetTypeSizeMultiplier**

In `GetTypeSizeMultiplier`, after `case Constants.DuckType.KYOTO_KIMONO_DUCK: return britishPunkSizeMultiplier;`, add:

```csharp
case Constants.DuckType.JAPANESE_MONK_DUCK: return britishPunkSizeMultiplier;
```

- [ ] **Step 10: Commit**

```bash
git add ShooterBRemake/Assets/Scripts/Models/Ducks/Duck.cs && git commit -m "feat: wire DuckPartAnimator into Duck lifecycle"
```

---

## Task 6: Unity Editor Setup

These steps must be done manually in the Unity Editor after the scripts compile without errors.

- [ ] **Step 1: Create the DuckPartLibrary asset**

In the Unity Project window: right-click `Assets/Data` > Create > ShooterB > Duck Part Library. Name it `DuckPartLibrary`.

- [ ] **Step 2: Add DuckPartAnimator to the duck prefab**

Open the duck prefab (the one used by `DuckSpawner` and `CampaignDuckSpawner`). Add Component > `DuckPartAnimator`. Drag `DuckPartLibrary` from `Assets/Data` into the `Part Library` field.

- [ ] **Step 3: Add the JAPANESE_MONK_DUCK entry**

In `DuckPartLibrary` Inspector, add one entry to `Entries`:
- Duck Type: `JAPANESE_MONK_DUCK`
- Config:
  - Torso Sprite: `jp_monk_torso` (from `Assets/Sprites/JapanMonkDuck/`)
  - Wing Sprite: `jp_mong_left_wing` (from `Assets/Sprites/JapanMonkDuck/`) — note the typo in the filename
  - Right Wing Sprite: leave empty (will be mirrored from Wing Sprite)
  - Left Wing Pivot Offset: start with `(0, 0)`, tune in play mode
  - Right Wing Pivot Offset: start with `(0, 0)`, tune in play mode
  - Flap Speed: `2` (cycles/sec — tune to taste)
  - Flap Amplitude: `30` (degrees — tune to taste)
  - Phase Offset: `0.5` (left and right wing alternate)
  - Torso Bob Amount: `0` (disabled until you want it)
  - Torso Bob Speed: `0`

- [ ] **Step 4: Tune pivot offsets in play mode**

Spawn a `JAPANESE_MONK_DUCK` in a test scene. With the duck alive, select its `LeftWingPivot` child in the Hierarchy and adjust `Left Wing Pivot Offset` on `DuckPartLibrary` until the pivot sits at the duck's left shoulder. Do the same for `Right Wing Pivot Offset`. Copy the values out of play mode into the asset.

- [ ] **Step 5: Verify existing ducks are unaffected**

Spawn a `Type0` duck. Confirm it still uses the frame-flip system (no child parts appear in the Hierarchy during play, animation runs as before).

- [ ] **Step 6: Verify death works**

Kill a `JAPANESE_MONK_DUCK`. Confirm:
- Parts disappear on kill
- The standard death sprite appears on the root SpriteRenderer
- The duck falls with gravity and disappears after ~2 seconds

- [ ] **Step 7: Commit assets**

```bash
git add ShooterBRemake/Assets/Data/DuckPartLibrary.asset ShooterBRemake/Assets/Data/DuckPartLibrary.asset.meta && git commit -m "feat: add DuckPartLibrary asset with JAPANESE_MONK_DUCK entry"
```
