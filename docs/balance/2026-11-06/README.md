# Balance run 2026-11-06

First balance run with all ground units working (carriers, wrecks, tree costs, mortar splash, threat-aware routes).
Made with `tools/balance/Balance.cs` on the data as of this commit. All unit numbers are still proposals.

Two variants of the firing rule (`battle_rules.direct_fire_needs_own_sight`):
- `shared/`: anyone fires at what any friendly unit sees, out to `range_m` (current rule).
- `own/`: direct fire only at what the shooter itself sees; shared vision only for the mortar.

## Files

| File | What |
| --- | --- |
| `duels.csv` | One defender (in the open, 10 m beside the route) against a stream of one attacker type: 8 troops 1.5 s apart or 3 vehicles 4 s apart, on open ground. The mortar gets a rifle squad as spotter. |
| `crossing.csv` | Each attacker alone and unopposed on each map: does it get across, and how long it takes. |
| `maps.csv` | Each map, 20 seeds: budget 1500, six defenders (machine gunner, anti-tank, sniper, 2 rifle squads, mortar) placed beside the routes the attackers take, against five identical mixed waves (6 recruits, 4 regulars, 2 motorbikes, 1 MRAP, 1 old tank, 1 van). Line health 20. |
| `waves.csv` | The same runs wave by wave, with how many attackers used each route mode. |
| `units_totals.csv` | Totals per unit over all map runs. |

## What it shows

1. **One defender loses to a stream of regulars or special forces**, whatever the defender. Against 8 regulars: 0 to 6 kills, and the defender dies. Against 8 special forces: 0 kills. Recruits are fine (7 or 8 of 8 killed by every defender that can shoot troops; the anti-tank team cannot). Causes: `low` armour halves small-arms damage, special forces see 40 m against our 30 m, and defender hit points are 12 to 30.
2. **The anti-tank team loses to three old tanks** (0 kills, dies): 20 HP against tank fire. Two anti-tank teams do kill one old tank (test `DestroyedTank_...`).
3. **Carriers make the biggest share of line damage.** An MRAP that gets across does 7 (3 + 4 special forces inside), a van 14 of the line's 20. MRAPs alone did 38% (shared) and 46% (own) of all line damage in the map runs.
4. **No run survives five mixed waves** (0 of 60 in both variants): 1.2 to 1.8 waves on average.
5. **The firing rule matters for defenders' survival:** defenders lost per run 1.0 to 6.0 with shared vision, 1.0 to 1.8 with own sight. The sniper (12 HP, no armour) dies in every run in both.
6. **Routes work.** Every attacker gets across every map unopposed (none stuck); 20 s (MRAP, balka map) to 110 s (old tank, forest belts). Adaptation shows up: wave 1 all fastest; wave 2 about 7% evade or safest (only lag-1 units: special forces, MRAP); wave 3 about a third evade (regulars, lag 2, now know wave 1).

## Open for Yaroslav

- Firing rule: shared vision out to `range_m` (current) or own sight for direct fire.
- Defender strength: a stream of 8 regulars should not beat every defender on its own. Raise defender HP and damage, lower regular armour, or accept that defenders need cover and combinations.
- Carriers' passenger line damage: keep "each passenger counts" or count the carrier only.
