# Adding Duck Types

This guide documents the current workflow for adding new duck types to the project.

For ducks that use the skeletal torso+wings animation system instead of the 8-frame flip cycle,
see `ADDING_PART_ANIMATED_DUCKS.md`.

Use this when adding one duck or batching many ducks at once.

## Recommended approach for many ducks

When adding many ducks with the same logic:

1. Add 1 duck end-to-end first
2. Verify enum wiring, frame library mapping, sprite import settings, and spawn config behavior
3. Add the remaining ducks in one batch

Reason:
- behavior is shared
- most failures come from registration or serialized asset mismatches
- batching is efficient once one duck is proven

## Checklist for every new duck

### 1. Add the duck type
File:
- `ShooterBRemake/Assets/Scripts/Utils/Constants.cs`

Update:
- `Constants.DuckType`
- `GetDuckDisplayName`
- `GetDuckDebugName`
- `DuckPoints`
- `DuckPoints.GetPoints`

Important:
- keep new enum values at the end unless you intentionally want to change serialized values in Unity assets
- explicit enum assignments like `Type4 = 4` create numbering gaps
- always verify the actual serialized numeric value used by Unity assets

## 2. Add size mapping
File:
- `ShooterBRemake/Assets/Scripts/Models/Ducks/Duck.cs`

Update:
- add a dedicated serialized size multiplier if the duck needs custom visual size
- map the new duck type in `GetTypeSizeMultiplier`

Recommendation:
- prefer a dedicated multiplier for each special duck instead of reusing another duck’s value when tuning is likely

## 3. Add frame validation
File:
- `ShooterBRemake/Assets/Scripts/Controllers/DuckSpawner.cs`

Update:
- add `ValidateTypeFrames(...)` for the new duck type

Purpose:
- catches missing frame assignments early

## 4. Register frames in the frame library
File:
- `ShooterBRemake/Assets/Data/DuckFrameLibrary.asset`

Update:
- add one or more `frameSets` entries for the serialized duck type value
- each variant should have a full frame list in order

Notes:
- multiple entries with the same duck type are allowed
- `DuckFrameLibrary.GetFrames()` randomly chooses one valid variant
- this is the correct way to support multiple visual variants for one duck type

## 5. Check sprite import metadata
Folder:
- `ShooterBRemake/Assets/Sprites/<DuckFolder>/`

Check each `.png.meta`:
- sprite rect
- pivot
- border
- sprite mode

Current project expectation for full-frame ducks:
- `x: 0`
- `y: 0`
- `width: 512`
- `height: 512`

This is important:
- every duck frame in a duck folder should use the full `0,0,512,512` rect unless there is a deliberate documented exception
- do not leave trimmed sprite rects in place for duck animation frames
- if a new duck ships with trimmed imports, fix the `.png.meta` files before tuning size or hitbox values

Why this matters:
- trimmed imports can cause visual inconsistencies
- mismatched frame bounds can look like jitter or shaking

## 6. Add spawn config usage
Files:
- `ShooterBRemake/Assets/Data/Campaign/SpawnConfig/*.asset`
- any arcade config or weighted spawn source if relevant

Update:
- add the new duck type to the intended stage spawn configs
- verify the serialized value is the intended duck type

Important:
- if a duck exists in code but is not referenced by stage/spawn assets, it will never appear

## 7. Optional systems to update
Only update these if the duck should participate in them.

Possible files:
- `ShooterBRemake/Assets/Scripts/Managers/AchievementManager.cs`
- `ShooterBRemake/Assets/Scripts/Managers/DailyAwardsManager.cs`
- `ShooterBRemake/Assets/Scripts/Managers/LocalizationManager.cs`

Examples:
- city grouping
- exact duck-name localization keys
- elite classification
- boss classification

## Batch workflow for adding 10 ducks

1. Prepare all sprite folders first
2. Normalize all sprite imports before wiring code
3. Add all enum values at the end of `DuckType`
4. Add all display/debug names and points together
5. Add all size mappings together
6. Add all frame validation entries together
7. Add all `DuckFrameLibrary.asset` entries together
8. Add all stage spawn entries together
9. Test one representative stage per city/group
10. Fix any serialized enum mismatches before adding more content

## Common failure modes

### Fallback duck appears
Likely causes:
- wrong serialized duck type value in `DuckFrameLibrary.asset`
- wrong serialized duck type value in spawn config asset
- missing frame registration for the duck type
- empty frame array for that type

### Duck shakes during animation
Likely causes:
- art is not aligned consistently frame-to-frame inside the canvas
- sprite rects are trimmed or inconsistent
- visual content moves within the 512x512 frame

### Duck looks too large or too small
Likely causes:
- wrong `GetTypeSizeMultiplier` mapping
- reused multiplier that does not fit the new duck art

## Practical rule

If ducks share logic, batch the shared plumbing.
If ducks have new visuals, still verify one fully before mass-adding the rest.
