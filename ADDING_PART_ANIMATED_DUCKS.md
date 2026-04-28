# Adding Part-Animated Duck Types

Part-animated ducks use a skeletal-style system with a separate torso and wing sprites
animated procedurally, instead of the standard 8-frame flip cycle.

The two systems coexist. A duck that has no entry in `DuckPartLibrary` falls back to the
normal frame animation automatically.

See `ADDING_DUCK_TYPES.md` for the standard frame-animated duck workflow.

---

## Checklist for every new part-animated duck

### 1. Prepare sprites

Folder: `ShooterBRemake/Assets/Sprites/<DuckFolder>/`

Expected files:
- `<name>_torso.png` - full body without wings
- `<name>_left_wing.png` - one wing, pivot at the shoulder joint
- `<name>_right_wing.png` (optional) - if omitted the left wing is mirrored

Sprite import settings for each file (set in Unity Sprite Editor):
- **Pixels Per Unit**: match all other part-animated ducks (currently 100)
- **Pivot**: Custom, placed at the shoulder joint of the wing sprite
  (the pivot is the rotation center, so it should be at the point that attaches to the torso)
- **Sprite Mode**: Single

Check `.png.meta` files to confirm these settings persisted after import.

---

### 2. Add the duck type to Constants.cs

File: `ShooterBRemake/Assets/Scripts/Utils/Constants.cs`

Add at the end of `Constants.DuckType` enum:
```csharp
JAPANESE_TANUKI_DUCK = 43,
YOUR_NEW_DUCK = 44   // always append, never insert
```

Add to `GetDuckDisplayName`:
```csharp
case DuckType.YOUR_NEW_DUCK: return "Your New Duck";
```

Add to `GetDuckDebugName`:
```csharp
case DuckType.YOUR_NEW_DUCK: return "YOUR_NEW_DUCK";
```

Add a points constant and case in `DuckPoints`:
```csharp
public const int YOUR_NEW_DUCK = 4;
// ...
case DuckType.YOUR_NEW_DUCK: return YOUR_NEW_DUCK;
```

---

### 3. Add size mapping in Duck.cs

File: `ShooterBRemake/Assets/Scripts/Models/Ducks/Duck.cs`

Add a case in `GetTypeSizeMultiplier`:
```csharp
case Constants.DuckType.YOUR_NEW_DUCK: return britishPunkSizeMultiplier;
```

You can reuse an existing multiplier field as a starting value and tune it later via the
`sizeMultiplier` field on the DuckPartLibrary entry (see step 5).

---

### 4. Register the duck type in DuckPartLibrary

Open: `ShooterBRemake/Assets/Data/DuckPartLibrary.asset`

Add a new entry to the `entries` array:
- **Duck Type**: `YOUR_NEW_DUCK`
- **Torso Sprite**: assign `<name>_torso`
- **Left Wing Sprite**: assign `<name>_left_wing`
- **Right Wing Sprite**: leave empty to mirror the left wing, or assign a dedicated sprite

Fields to configure (can be tuned live in play mode):
| Field | Purpose |
|---|---|
| Left Wing Pivot Offset | position of the left shoulder on the torso, in local units |
| Right Wing Pivot Offset | position of the right shoulder on the torso, in local units |
| Left Wing Offset | offset of the wing sprite from its pivot |
| Right Wing Offset | offset of the right wing sprite from its pivot |
| Size Multiplier | scales the whole duck down/up relative to the torso height normalization |
| Flap Speed | wing cycles per second |
| Flap Amplitude | max rotation in degrees from the resting angle |
| Phase Offset | seconds offset between left and right wing (0 = fully in sync) |
| Torso Bob Amount | vertical bob distance in world units (0 to disable) |
| Torso Bob Speed | bob cycles per second |

---

### 5. Tune config values live in play mode

1. Enter play mode and spawn the duck (via arcade or a stage that includes it)
2. Select the duck in the Hierarchy
3. The `DuckPartAnimator` component Inspector shows all config fields for the active duck type
4. The Scene view shows cyan (left pivot) and yellow (right pivot) drag handles
5. All edits write back to `DuckPartLibrary.asset` immediately via undo-safe operations

Starting point recommendation:
- Copy pivot offsets and wing offsets from the monk duck entry if using the same wing sprite
- Set `Size Multiplier` to `0.6` and adjust until the duck matches surrounding ducks in screen size
- Set `Flap Speed` to `2.5`, `Flap Amplitude` to `25`, `Phase Offset` to `0`

---

### 6. Add to spawn config

Files: `ShooterBRemake/Assets/Data/Campaign/SpawnConfig/*.asset`

Add the duck type to the intended stage spawn configs the same way as any other duck type.

Note: part-animated ducks do not require a `DuckFrameLibrary` entry. If one is absent the
duck will spawn but show no sprite on the root renderer (the part animator handles visuals).
This is correct behavior.

---

## Sorting order reference

| Part | Sorting order relative to duck |
|---|---|
| Left wing | +1 (in front of torso) |
| Torso | 0 (base) |
| Right wing | -1 (behind torso) |

---

## Common failure modes

### Wings are out of sync (one up, one down) with phase offset 0
- Only happens when `rightWingSprite` is null (mirrored). The negation is automatic in code.
- If it still looks wrong, verify `phaseOffset` is actually 0 in the library asset.

### Duck is much larger than others
- The torso sprite is shorter than a full-body sprite, so the normalization scale is larger.
- Reduce `sizeMultiplier` in the library entry until the duck matches.

### Hit puff is sized to the old 8-sprite dimensions
- This should not happen. The hit puff uses torso renderer bounds in parts mode.
- If it does, check that `DuckPartAnimator` is attached to the duck prefab.

### Parts flash briefly on death
- Should not happen. Parts are deactivated before destruction.
- If it does, verify `DuckPartAnimator.DestroyParts` calls `SetActive(false)` before `Destroy`.

### Wings do not appear
- Check that the sprites are assigned in the `DuckPartLibrary` entry.
- Check that the `DuckPartAnimator` component is on the duck prefab.
- Check that the duck prefab has a `SpriteRenderer` on the root object.
