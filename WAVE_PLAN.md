# Wave System Plan

## Context

Campaign stages currently execute a single flat spawn sequence (`SpawnEntry[]`). Skopje stages need a wave structure with a "Wave N" announcement shown mid-screen for 3 seconds before each wave's ducks start spawning. Some stages will not have waves (e.g. Countryside), so the feature must be opt-in per stage config.

## Approach

Add a nested `WaveConfig[]` to `StageSpawnConfig`. If `waves` is non-empty the spawner uses wave mode; if empty it falls back to the existing flat `spawnSequence` (zero migration cost for existing stages). Each wave controls its own spawn entries and whether to show the announcement.

The spawner fires an event carrying the wave number and display duration, waits that duration, then starts spawning. The game controller relays the event to a new `WaveAnnouncementController` that fades the text in/out.

---

## File Changes

### 1. `Assets/Scripts/Models/Campaign/StageSpawnConfig.cs`

Add `WaveConfig` serializable class above `StageSpawnConfig`:

```csharp
[Serializable]
public class WaveConfig
{
    public bool showAnnouncement = true;
    public float announcementDuration = 3f;
    public SpawnEntry[] spawnSequence;
}
```

Add to `StageSpawnConfig`:

```csharp
[Header("Waves (overrides spawnSequence if non-empty)")]
public WaveConfig[] waves;
```

Existing `spawnSequence` field stays untouched for backwards compatibility.

---

### 2. `Assets/Scripts/Controllers/CampaignDuckSpawner.cs`

Add event:

```csharp
public event Action<int, float> OnWaveStarting; // waveNumber (1-based), displayDuration
```

In `StartSpawning()`, branch on whether `waves` is populated:

```csharp
if (spawnConfig.waves != null && spawnConfig.waves.Length > 0)
    StartCoroutine(SpawnWavesCoroutine(spawnConfig));
else
    StartCoroutine(SpawnSequenceCoroutine(spawnConfig));
```

New `SpawnWavesCoroutine`:

```csharp
private IEnumerator SpawnWavesCoroutine(StageSpawnConfig config)
{
    for (int i = 0; i < config.waves.Length; i++)
    {
        if (!isSpawning || GameManager.Instance.IsGameOver) yield break;

        WaveConfig wave = config.waves[i];
        int waveNumber = i + 1;

        if (wave.showAnnouncement)
        {
            OnWaveStarting?.Invoke(waveNumber, wave.announcementDuration);
            yield return new WaitForSeconds(wave.announcementDuration);
        }

        if (wave.spawnSequence != null)
        {
            foreach (SpawnEntry entry in wave.spawnSequence)
            {
                if (!isSpawning || GameManager.Instance.IsGameOver) yield break;
                yield return new WaitForSeconds(entry.delay);
                if (!isSpawning || GameManager.Instance.IsGameOver) yield break;
                SpawnDuck(entry, config);
            }
        }
    }

    StartCoroutine(WaitForAllDucksResolved());
}
```

---

### 3. `Assets/Scripts/UI/WaveAnnouncementController.cs` (new file)

Simple UI controller with a TMP_Text and CanvasGroup:

```csharp
public void ShowWave(int waveNumber, float duration)
{
    StartCoroutine(ShowWaveCoroutine(waveNumber, duration));
}
```

Coroutine behaviour:
- Set text to localized string: `string.Format(LocalizationManager.Instance.Get("campaign.wave.label", "Wave {0}"), waveNumber)`
- Fade CanvasGroup alpha 0 to 1 over 0.3s
- Hold for `duration - 0.6s`
- Fade out over 0.3s
- Reset alpha to 0 and deactivate

---

### 4. `Assets/Scripts/Controllers/CampaignGameController.cs`

Add inspector field:

```csharp
[Header("Wave Announcement")]
public WaveAnnouncementController waveAnnouncementController;
```

In `Start()`, after finding the spawner:

```csharp
campaignDuckSpawner.OnWaveStarting += HandleWaveStarting;
```

In `OnDestroy()`:

```csharp
if (campaignDuckSpawner != null)
    campaignDuckSpawner.OnWaveStarting -= HandleWaveStarting;
```

Handler:

```csharp
private void HandleWaveStarting(int waveNumber, float duration)
{
    if (waveAnnouncementController != null)
        waveAnnouncementController.ShowWave(waveNumber, duration);
}
```

---

### 5. `Assets/Scripts/Managers/LocalizationManager.cs`

Add localization key to both language dictionaries:
- Key: `"campaign.wave.label"`
- EN: `"Wave {0}"`
- MK: `"Бран {0}"`

---

## Unity Editor Steps (manual, after code)

1. In CampaignGameScene, add a UI panel centered on screen with a TMP_Text child.
2. Add a `CanvasGroup` component to the panel.
3. Attach `WaveAnnouncementController` script to the panel.
4. Set the panel's sort order to render above game HUD.
5. Assign the `WaveAnnouncementController` reference in `CampaignGameController` inspector.
6. Update `StageSpawnConfig_Skopje_0..4`: populate `waves[]` by grouping the existing `spawnSequence` entries into wave buckets. Leave `spawnSequence` empty.
7. Stages without waves (Countryside etc.) leave `waves` array empty - no other changes needed.

---

## Verification

1. Set `editorFallbackStage` to a Skopje stage with `waves` populated and Play.
2. Confirm "Wave 1" banner appears for 3s, then first wave ducks spawn.
3. After last wave 1 spawn, confirm "Wave 2" banner appears, then wave 2 ducks spawn.
4. Switch fallback to a Countryside stage (no `waves`) - flat sequence runs with no announcement.
5. Confirm `WaitForAllDucksResolved` fires after all waves complete and stage complete modal shows.
