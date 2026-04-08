# Shooter B Rework

Unity remake of the original Android "Shooter B" duck hunting game.

Primary project instructions and architecture notes live in [CLAUDE.md](./CLAUDE.md).

## Project Layout

- `ShooterBRemake/`: Unity remake project
- `ShooterBgame/`: original Android reference implementation

## Dev Cheats And Debug Shortcuts

### Stage Start Modal

These only work while the campaign stage start modal is open, before pressing Start.

- `J`: grant `+2 lives`
- `H`: grant `+ bullets`

Notes:
- These follow the same one-use-per-stage behavior as the start-modal reward buttons.
- They are intended for development/testing only.

### Campaign Gameplay

- `K`: skip the current wave
- `Y`: force stage complete with 1 star
- `U`: force stage complete with 2 stars
- `I`: force stage complete with 3 stars

### HUD Debug Shortcuts

Available in both campaign and arcade HUD flows.

- `[` or `O` or `F9`: trigger achievement popup debug action
- `]` or `P` or `F10`: trigger daily popup debug action
- `R`: reset daily debug state

### Weapon Switching

Available during gameplay when the corresponding weapons exist in the scene.

- `1`: Piranha Gun
- `2`: Cabirne
- `3`: Rifle
- `4`: Tesla Gun
- `5`: Mr Sulko
- `6`: Beretta
- `7`: Laser Gun

