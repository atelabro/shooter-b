# Stage Duck Counts

Counted from the campaign `StageConfig` and `StageSpawnConfig` assets in `ShooterBRemake/Assets/Data/Campaign/`.

Duck totals include pattern-expanded spawns, meaning a spawn entry with `patternRef` counts as the full number of entries in that pattern.

Health totals use `healthOverride` when present. Otherwise, boss ducks use their `Constants.DuckHealth` value and all other ducks count as `1` health.

Excluded from health totals are the explicit final boss entries at the end of each city:
- Skopje: `duckType 24`, `healthOverride 6`
- Paris: `duckType 25`, `healthOverride 12`
- London: `duckType 27`, `healthOverride 15`
- New York: `duckType 18`, `healthOverride 10`
- Los Angeles: `duckType 30`, `healthOverride 18`
- Cairo: `duckType 40`, `healthOverride 24`
- Tokyo: `duckType 26`, `healthOverride 30`

Countryside does not currently have an explicit final boss entry to exclude.

## Summary

- Total stages: 40
- Total waves: 200
- Total duck spawns: 4679
- Total boss duck health: 115
- Total health excluding city-final bosses: 7720
- Total hit points: 7835

## By City

| Campaign Order | City | Stages | Waves | Duck Spawns | Boss Duck Health | Health Excluding Final Boss | Total Hit Points |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | Countryside | 3 | 10 | 143 | 0 | 231 | 231 |
| 2 | Skopje | 5 | 21 | 548 | 6 | 751 | 757 |
| 3 | Paris | 6 | 32 | 705 | 12 | 706 | 718 |
| 4 | London | 5 | 26 | 626 | 15 | 663 | 678 |
| 5 | New York | 6 | 32 | 766 | 10 | 868 | 878 |
| 6 | Los Angeles | 5 | 26 | 605 | 18 | 740 | 758 |
| 7 | Cairo | 5 | 26 | 614 | 24 | 765 | 789 |
| 8 | Tokyo | 5 | 27 | 672 | 30 | 3284 | 3314 |

## By Stage

| Campaign Order | City | City Stage | Global Stage Index | Stage Name | Waves | Duck Spawns | Boss Duck Health | Health Excluding Final Boss | Total Hit Points |
| ---: | --- | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| 1 | Countryside | 1 | 0 | Countryside - Day 1 | 2 | 22 | 0 | 22 | 22 |
| 1 | Countryside | 2 | 1 | Countryside - Day 2 | 4 | 60 | 0 | 144 | 144 |
| 1 | Countryside | 3 | 2 | Countryside - Day 3 | 4 | 61 | 0 | 65 | 65 |
| 2 | Skopje | 1 | 3 | Macedonia Square | 3 | 105 | 0 | 105 | 105 |
| 2 | Skopje | 2 | 4 | Stone Bridge & Vardar | 4 | 106 | 0 | 106 | 106 |
| 2 | Skopje | 3 | 5 | Old Bazaar (Carsija) | 4 | 109 | 0 | 109 | 109 |
| 2 | Skopje | 4 | 6 | Kale Fortress | 5 | 113 | 0 | 159 | 159 |
| 2 | Skopje | 5 | 7 | Vodno / Millennium Cross | 5 | 115 | 6 | 272 | 278 |
| 3 | Paris | 1 | 8 | Eiffel District | 5 | 95 | 0 | 95 | 95 |
| 3 | Paris | 2 | 9 | Louvre Front | 5 | 118 | 0 | 118 | 118 |
| 3 | Paris | 3 | 10 | Moulin Quarter | 5 | 111 | 0 | 111 | 111 |
| 3 | Paris | 4 | 11 | Notre Dame Perimeter | 5 | 120 | 0 | 120 | 120 |
| 3 | Paris | 5 | 12 | Sacre-Coeur Heights | 5 | 114 | 0 | 114 | 114 |
| 3 | Paris | 6 | 13 | Arc de Triomphe Ring | 7 | 147 | 12 | 148 | 160 |
| 4 | London | 1 | 20 | Big Ben Watchline | 5 | 117 | 0 | 117 | 117 |
| 4 | London | 2 | 21 | Buckingham Perimeter | 5 | 145 | 0 | 145 | 145 |
| 4 | London | 3 | 22 | London Eye Circuit | 5 | 123 | 0 | 123 | 123 |
| 4 | London | 4 | 23 | Tower Bridge Ambush | 5 | 128 | 0 | 128 | 128 |
| 4 | London | 5 | 24 | Whitehall Final Push | 6 | 113 | 15 | 150 | 165 |
| 5 | New York | 1 | 14 | Central Park Sweep | 5 | 117 | 0 | 117 | 117 |
| 5 | New York | 2 | 15 | Brooklyn Bridge Run | 5 | 145 | 0 | 145 | 145 |
| 5 | New York | 3 | 16 | Liberty Harbor | 5 | 123 | 0 | 123 | 123 |
| 5 | New York | 4 | 17 | Manhattan Gridlock | 5 | 122 | 0 | 122 | 122 |
| 5 | New York | 5 | 18 | Times Square Crossfire | 6 | 127 | 0 | 157 | 157 |
| 5 | New York | 6 | 19 | Empire State Finale | 6 | 132 | 10 | 204 | 214 |
| 6 | Los Angeles | 1 | 30 | Downtown Sweep | 5 | 117 | 0 | 134 | 134 |
| 6 | Los Angeles | 2 | 31 | Hollywood Hills | 5 | 145 | 0 | 170 | 170 |
| 6 | Los Angeles | 3 | 32 | Rodeo Drive Rush | 5 | 123 | 0 | 155 | 155 |
| 6 | Los Angeles | 4 | 33 | Santa Monica Breakwater | 5 | 122 | 0 | 156 | 156 |
| 6 | Los Angeles | 5 | 34 | Port Lockdown | 6 | 98 | 18 | 125 | 143 |
| 7 | Cairo | 1 | 35 | Desert Approach | 5 | 125 | 0 | 151 | 151 |
| 7 | Cairo | 2 | 36 | Oasis Crossfire | 5 | 145 | 0 | 170 | 170 |
| 7 | Cairo | 3 | 37 | Pyramid Gate | 5 | 123 | 0 | 158 | 158 |
| 7 | Cairo | 4 | 38 | Sphinx Watch | 5 | 123 | 0 | 157 | 157 |
| 7 | Cairo | 5 | 39 | Inner Pyramid Siege | 6 | 98 | 24 | 129 | 153 |
| 8 | Tokyo | 1 | 25 | Sensoji Frontline | 5 | 129 | 0 | 661 | 661 |
| 8 | Tokyo | 2 | 26 | Torii Passage | 5 | 147 | 0 | 833 | 833 |
| 8 | Tokyo | 3 | 27 | Fuji Skyline | 5 | 127 | 0 | 645 | 645 |
| 8 | Tokyo | 4 | 28 | Tokyo Tower Control | 5 | 130 | 0 | 480 | 480 |
| 8 | Tokyo | 5 | 29 | Shibuya Neon Siege | 7 | 139 | 30 | 665 | 695 |
