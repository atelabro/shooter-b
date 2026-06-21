# Stage Evaluation: ShooterB Remake Campaign

## Stage Summary Table

| Stage | City | Index | StartDiff | SpeedMult | Waves | Duck Types | Star3 |
|-------|------|-------|-----------|-----------|-------|------------|-------|
| Countryside_0 | Countryside | 0 | 1 | 0.80 | 2 | MK_PHALARX, MK_ARCHER | 31 |
| Countryside_1 | Countryside | 1 | 2 | 0.88 | 4 | + MK_VOJVODA | 46 |
| Countryside_2 | Countryside | 2 | 3 | 0.90 | 5 | + SAMUIL_ELITE (health 5) | 61 |
| Skopje_0 | Skopje | 3 | 4 | 0.70 | 3 | PHALARX (health 10!), ARCHER | 73 |
| Skopje_1 | Skopje | 4 | 5 | 0.71 | 3 | PHALARX, ARCHER | 90 |
| Skopje_2 | Skopje | 5 | 6 | 0.79 | 3 | + VOJVODA | 130 |
| Skopje_3 | Skopje | 6 | 7 | 0.98 | 6 | + SAMUIL_GUARD, ELITE, KING | 144 |
| Skopje_4 | Skopje | 7 | 8 | 0.85 | 3 | SAMUIL_GUARD, ELITE, KING | 146 |
| Paris_0 | Paris | 8 | 9 | 0.91 | 5 | NAPOLEON, MUSKETEER | 155 |
| Paris_1 | Paris | 9 | 10 | ~0.93 | 6 | NAPOLEON + REVOLUTIONARY, ARTIST | 176 |
| Paris_2 | Paris | 10 | 11 | ~0.95 | 6 | NAPOLEON + REVOLUTIONARY, ARTIST | 240 |
| Paris_3 | Paris | 11 | 12 | ~0.97 | 6 | NAPOLEON + REVOLUTIONARY, ARTIST | 273 |
| Paris_4 | Paris | 12 | 13 | ~0.99 | 6 | NAPOLEON + REVOLUTIONARY, ARTIST | 377 |
| Paris_5 | Paris | 13 | 13 | 1.01 | 6 | NAPOLEON, MUSKETEER, MUSKETEER_2 | 455 |
| London_0 | London | 14 | 10 | ~0.90 | 5 | BRITISH_REDCOAT, POLICE | 182 |
| London_1 | London | 15 | 11 | ~0.92 | 6 | + BRITISH_PUNK | 215 |
| London_2 | London | 16 | 12 | ~0.95 | 6 | British types | 280 |
| London_3 | London | 17 | 13 | ~0.97 | 6 | British types | 348 |
| London_4 | London | 18 | 14 | ~0.99 | 6 | British types | 403 |
| NewYork_0 | NewYork | 19 | 11 | ~0.92 | 5 | USA_POLICE, USA_WORKER | 202 |
| NewYork_1 | NewYork | 20 | 12 | ~0.95 | 6 | + USA_BUSINESS | 241 |
| NewYork_2 | NewYork | 21 | 13 | ~0.97 | 6 | USA types | 293 |
| NewYork_3 | NewYork | 22 | 14 | ~0.99 | 6 | USA types | 364 |
| NewYork_4 | NewYork | 23 | 15 | ~1.01 | 6 | USA types | 429 |
| NewYork_5 | NewYork | 24 | 16 | ~1.03 | 6 | USA types | 520 |
| Tokyo_0 | Tokyo | 25 | 13 | ~0.97 | 5 | SAMURAI, STRAW, KIMONO | 439 |
| Tokyo_1 | Tokyo | 26 | 14 | ~0.99 | 5 | Japanese types | 500 |
| Tokyo_2 | Tokyo | 27 | 15 | ~1.01 | 5 | Japanese types | 432 |
| Tokyo_3 | Tokyo | 28 | 16 | ~1.03 | 5 | Japanese types | 442 |
| Tokyo_4 | Tokyo | 29 | 17 | ~1.05 | 5 | Japanese types | 455+ |
| Kyoto_0 | Kyoto | 40 | 13 | 1.08 | 5 | SAMURAI, STRAW, KIMONO, KYOTO_KIMONO | 439 |
| Kyoto_1 | Kyoto | 41 | 14 | 1.12 | 5 | Japanese types + MONK | 500 |
| Kyoto_2 | Kyoto | 42 | 15 | 1.16 | 5 | Japanese types + KYOTO_KIMONO, MONK, TANUKI | 432 |
| Kyoto_3 | Kyoto | 43 | 16 | 1.20 | 5 | Japanese types + KYOTO_KIMONO, MONK, TANUKI, YAKUZA_BOSS | 442 |
| Kyoto_4 | Kyoto | 44 | 17 | 1.25 | 5 | Japanese types + MONK, TANUKI, YAKUZA_BOSS, SAMURAI_BOSS | 469 |
| Rio_0 | Rio de Janeiro | 45 | 13 | 1.18 | 5 | FOOTBALLER, LIFEGUARD | 439 |
| Rio_1 | Rio de Janeiro | 46 | 14 | 1.22 | 5 | FOOTBALLER, LIFEGUARD | 500 |
| Rio_2 | Rio de Janeiro | 47 | 17 | 1.40 | 5 | FOOTBALLER, LIFEGUARD, PEACH_ARMY | 432 |
| Rio_3 | Rio de Janeiro | 48 | 18 | 1.46 | 5 | Brazil regulars + CARNIVAL elite | 442 |
| Rio_4 | Rio de Janeiro | 49 | 18 | 1.46 | 5 | Brazil regulars + CARNIVAL elite | 442 |
| Rio_5 | Rio de Janeiro | 50 | 20 | 1.55 | 5 | Brazil regulars + CARNIVAL elite + LIFEGUARD_BOSS | 469 |

---

## Issues Found

### CRITICAL

#### 1. Paris stages 1-4 still use Revolutionary (8) and Artist (10) ducks
Stages 0 and 5 were fixed to use Musketeer/Napoleon only. Stages 1-4 still have FRENCH_REVOLUTIONARY and FRENCH_ARTIST — inconsistent.

#### 2. Paris_4 and Paris_5 have the same starting difficulty (13)
Two consecutive stages at the same difficulty level. Paris_5 should be 14 or 15.

#### 3. Tokyo_2 star threshold regresses from Tokyo_1
Tokyo_1 star3 = 500, Tokyo_2 star3 = 432. A later stage is easier to 3-star. Breaks progression.

#### 4. Skopje_4 has only 3 waves while Skopje_3 has 6
The final Skopje stage has fewer waves than the one before it. Anticlimactic — the city boss should be the peak.

---

### MODERATE

#### 5. Skopje_0 wave 1 has a duck with health override 10
With Rifle doing 1 damage, this requires 10 hits on the very first Skopje wave (difficulty 4). Likely confusing for players.

#### 6. Countryside_2 introduces SAMUIL_ELITE (health 5)
SAMUIL_ELITE is a Macedonian faction duck that thematically belongs to Skopje. Wrong city, and unexpectedly tanky for stage 3.

#### 7. London and NewYork start at lower difficulty than Paris ends
Paris ends at startingDifficulty 13. London starts at 10, NewYork at 11. Parallel cities create a regression if the player visits them after completing Paris.

#### 8. Countryside_1 forces weapon 3 (MrSulko)
Player weapon choice gets overridden. Fine as a tutorial mechanic but should be clearly communicated.

---

### FUN / BALANCE

#### 9. Star thresholds in late Paris feel very high
Paris_5 requires 455 pts for 3 stars. Each Musketeer = 2 pts. ~40 ducks = ~80 base points. Reaching 455 requires near-perfect combos. May be too demanding for 3-star.

#### 10. All Paris duck types worth the same points (2 pts)
Napoleon, Musketeer, and Musketeer_2 all award 2 points. The Musketeer_2 boss has 3-10 health and is significantly harder to kill — boss kills should reward more points.

#### 11. Skopje speed barely changes between stages 0 and 1
Skopje_0: 0.70, Skopje_1: 0.71 — essentially identical. Players won't feel any escalation.

#### 12. Tokyo star thresholds start extremely high
Tokyo_0 star3 = 439 vs NewYork_0 star3 = 202 and London_0 star3 = 182. Tokyo feels punishing from the very first stage compared to the other parallel cities.

---

## What Is Working Well

- Thematic duck grouping: each city has its own distinct duck roster
- Boss encounters in Skopje: health + size overrides on SAMUIL_KING create a real climax moment
- Paris_5 final boss design: MUSKETEER_2 with health 10 + BounceMid path + sizeMultiplier 1.2 in last wave is well designed
- Custom DuckPattern assets used in later stages give memorable scripted sequences
- Countryside is a gentle tutorial: slow speed, small wave count, clear announcements
- Wave announcements in Skopje+ set tone well and are properly localized

---

## Recommended Fixes (priority order)

1. Replace Revolutionary (8) and Artist (10) with Musketeer (21) in Paris stages 1-4
2. Raise Paris_5 startingDifficulty to 14 or 15
3. Fix Tokyo_2 star3 threshold — increase to ~520+ for consistent escalation
4. Add waves to Skopje_4 — currently 3, should be 5-6 to match Skopje_3's climax feel
5. Consider increasing FRENCH_NAPOLEON and MUSKETEER_2 points in Constants.cs (both currently 2)
6. Review Tokyo star thresholds for stage 0 — 439 feels disproportionate vs other cities
7. Remove SAMUIL_ELITE from Countryside_2 — thematically wrong city
8. Reconsider Skopje_0 health override of 10 on wave 1 — very harsh for difficulty 4
