# Campaign Intro Video Brief (D.U.C.K. Briefing)

## Purpose
Create a short intro cinematic that plays before `CampaignMapScene` and explains how the duck uprising conquered the world city by city.

## Duration
- Target: 30-40 seconds
- Must be skippable

## Story Alignment (Current Lore)
- Ducks gained coordinated intelligence and began a global uprising.
- The uprising started in Countryside.
- Paris is under heavy duck control (Eiffel Tower seized).
- New York has duck gridlock in Manhattan.
- The player is the last licensed duck hunter hired by D.U.C.K. (Department of Urban Containment and Killing).
- Mission: liberate cities one by one before the capital falls.

## Sequence (Shot List)

1. `0:00-0:04` - Establishing
- Visual: Dark world map, subtle motion, radio static/glitch overlay.
- Text/VO: "D.U.C.K. Command - Global Emergency Broadcast"

2. `0:04-0:09` - Inciting Incident
- Visual: News-like flashes of ducks gathering in massive formations.
- Text/VO: "Year 20XX. Ducks developed coordinated intelligence."

3. `0:09-0:15` - Conquest Spread
- Visual: Red takeover markers spread across the world map.
- Text/VO: "Cities fell one by one. Human response collapsed."

4. `0:15-0:22` - Known Hotzones
- Visual: Landmark shots under occupation:
  - Countryside (origin of uprising)
  - Eiffel Tower (Paris)
  - Manhattan streets (New York)
- Text/VO: "Countryside was first. Paris was seized. Manhattan is gridlocked."

5. `0:22-0:30` - Player Call-to-Action
- Visual: Targeting overlays, dossier card, hunter silhouette/weapon prep.
- Text/VO: "You are the last licensed hunter. D.U.C.K. authorizes full containment."

6. `0:30-0:36` - Transition to Gameplay
- Visual: World map zoom-in to first campaign city pin.
- Text/VO: "Operation Featherfall begins now."
- End card: "Tap to Deploy" (or auto-transition)

## Voiceover Draft
"D.U.C.K. Command - Global Emergency Broadcast.  
Year 20XX. Ducks developed coordinated intelligence.  
Cities fell one by one. Human response collapsed.  
The uprising began in the Countryside. Paris is occupied. Manhattan is locked down.  
You are the last licensed hunter.  
By order of D.U.C.K., you are authorized to reclaim every city.  
Operation Featherfall begins now."

## Art/Asset Requirements (For Later AI Generation)
- World map base image (clean + damaged variants)
- Landmark images: Countryside, Eiffel Tower, Manhattan streets
- Duck swarm silhouettes (near/far variants)
- HUD/radio overlay elements (scanlines, warning frames, static)
- D.U.C.K. insignia/logo card
- End-card typography ("Operation Featherfall", "Tap to Deploy")

## Style Notes
- Tone: urgent military briefing, not comedy.
- Palette: muted/dark with red alert accents.
- Motion: map push-ins, marker spread, glitch cuts, subtle parallax.
- Keep readability high on mobile screens.

## Implementation Notes
- Play once before first Campaign map load, then store a viewed flag in `PlayerPrefs`.
- Add Skip button after first 1-2 seconds.
- Keep all text localizable and avoid embedding critical text directly in baked video frames.
