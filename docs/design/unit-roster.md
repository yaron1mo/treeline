# Full unit roster (both sides)

Proposal by the designer agent, 2026-11-27, for issue #26. Status: **Open**. Yaroslav decides. Nothing here changes a "Decided" item in `progression.md`. The squad rules are in [squad-selection.md](squad-selection.md) (PR #24).

## 1. Goal

Agree one final list of attacker and defender types, so that Mistral balances once. Each attacker has a hard counter. Each defender has a job and a situation where it is the best choice. The list is small: depth comes from upgrade paths and squad choice, not from many types.

## 2. Player experience

The player sees ten enemy types in the game. Each one has a clear icon and one clear answer ("tank: anti-tank team", "drone: air defence"). The brief shows the class, so the player picks the answer before the battle. A late wave mixes types, so one answer is not enough. The EW station and the one-way drone are rare surprises, not new systems.

## 3. Attackers (10 types for release)

Source of numbers: `units.csv`. "Stopped" values are from `tools/balance/stop.csv` (higher = the defender stops it better).

| # | Type (`id`) | Role | Hard counter (best) | It counters | Appears | In `units.csv` |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Recruit (`recruit`) | Cheap infantry mass | Machine gunner (20), sniper (20) | Defenders with no cover; thin lines | Early | Keep |
| 2 | Regular soldier (`regular`) | Infantry, low armour. Can carry an EW station | Machine gunner (20), mortar (13) | Rifle squads | Early | Keep |
| 3 | Special forces (`special_forces`) | Elite infantry, sees at night, fast | Sniper (3.0), mortar (3.9), machine gunner (2.4) | Short-sight defenders at night; drone crews | Middle | Keep |
| 4 | Buggy (`buggy`) | Fast raider: runs past the line | Machine gunner (14), sniper (10) | Slow, short-range defenders (mortar, anti-tank) | Early | Keep. Takes the role of motorbike |
| 5 | Infantry fighting vehicle (`ifv`) | Armour that unloads infantry | Anti-tank team (3.0) | Infantry-only squads | Middle | Keep |
| 6 | Tank (`old_tank`, later `modern_tank`) | Heavy armour, long range | Anti-tank team (3.8–5.1), FPV (1.0–2.4) | Everything without anti-tank | Middle (old), late (modern) | Keep both rows as two tiers of one type |
| 7 | Recon UAV (`recon_uav`) | Spotter: calls FPV on what it sees | Air defence, sniper, machine gunner | Static defenders in the open | Middle | Keep. Fix `targetable_by` (no "radar" defender exists) |
| 8 | FPV drone (`fpv`; later fibre-optic FPV) | Single-use strike on a spotted unit | Air defence (2.4), machine gunner (1.4). EW stops the radio FPV, **not** the fibre-optic FPV | Mortar, sniper, operators (soft, valuable units) | Middle (radio), late (fibre-optic) | Keep. Fibre-optic: **new** row, same `behaviour` |
| 9 | One-way drone, Shahed type (`one_way_drone`) | Ignores defenders, hits the rear line | Air defence (only hard counter). EW can turn it away | Squads with no air defence | Late | Keep |
| 10 | EW team (`ew_team`) | Carries or drops an EW station. Weak in a fight | Mortar, sniper, bomber drone, FPV (kill the station) | Drone squads | Middle | **New** (a `regular` that carries the station; see section 4) |

Removed from the release list (rows stay in the file, switched off, for later or for rare comic waves): `motorbike` (same job as the buggy), `van` (same job as the buggy plus a small carrier), `mrap` (same job as the IFV), `helicopter` (needs a second air-defence answer to be fair; update after release).

Rare comic enemies (decided, not in the count): for example the looter pig with a washing machine.

## 4. EW mini station (both sides)

Decided rules: on a vehicle or carried by a soldier; can be dropped; keeps working after the carrier dies; low HP; must be destroyed separately; both sides.

| Item | Proposal |
| --- | --- |
| HP | 6 (one rifle burst kills it) |
| Radius | 50 m. Drone sight is 40 m, so the zone is bigger than the sight of a drone, and the drone cannot see it before it enters. |
| Carrier | The attacker EW team or a vehicle. The defender EW station is a placed unit (type 10 on the defender list). |
| Drop | The carrier drops the station at its first stop, or when it dies. The station stays. |
| Duration | Whole battle (see question 2). |

Effect on each air item, simple rule: **inside the zone the radio link is bad.**

| Item | Effect inside the zone |
| --- | --- |
| Recon drone | Sight radius is half (40 m → 20 m). It still lives. |
| Bomber drone (Mavic type) | Cannot hold position: it flies back to its launch point and tries again after it leaves the zone. No drops while inside. |
| FPV (radio) | 80% chance that it loses the link on entry and falls. |
| FPV, fibre-optic | **No effect.** This is its job: it beats EW and loses to air defence and machine guns. |
| One-way drone | 50% chance that it turns away and misses its target. |
| Artillery (support call) | The "seen by our drones" accuracy bonus does not work for a target in the zone. The shell uses the unseen accuracy. |

How the player sees it:
- Own station: a dashed ring in the friendly colour, a slow pulse, and an antenna icon.
- Enemy station: the ring appears when any defender sees the station. Before that, the player sees only the effect: the drone icon flickers, shows "no signal" and falls with a small spark.
- A drone that enters the zone shows the same spark. This is the "tasty candy" feedback: clear and short.

## 5. Air defence and one-way drones

| Shooter | Hits | Does not hit |
| --- | --- | --- |
| Air defence gun | Recon UAV, FPV, fibre-optic FPV, one-way drone | Ground units |
| Machine gunner | Recon UAV, FPV (low chance) | One-way drone (too fast and too armoured) |
| Sniper | Recon UAV | FPV, one-way drone |
| FPV drone (own) | Recon UAV (air hit) | — |

The one-way drone has no AI: it flies a straight line to the rear and does line damage. The player does not move or aim anything. The only questions are "is there air defence?" and "is there an EW station?". The defender gets no one-way drone of its own at release (low priority; question 4).

## 6. Defenders (10 types for release)

Branches are the decided three: Assault, Drone forces, Intelligence.

| # | Type (`id`) | Branch | Job | Best choice when | Appears | In `units.csv` |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Rifleman (`rifle_squad`) | Assault | Cheap base and upgrade root (marksman, then sniper or machine gunner) | Money is short; early waves; the player needs many cheap bodies to hold ground | Early | Keep |
| 2 | Machine gunner (`machine_gun`) | Assault | Stops infantry and fast units; soft answer to drones | Waves of recruits, regulars and buggies; the first line | Early | Keep |
| 3 | Anti-tank team (`anti_tank`) | Assault | Stops armour (only hard counter to IFV and tank) | Any wave with a vehicle | Middle | Keep |
| 4 | Air defence gun (`air_defence`) | Assault | Stops all air items | Recon UAV, FPV and one-way drone waves | Middle | Keep |
| 5 | Sniper (`sniper`) | Intelligence | Long range, picks one target | Elite infantry and EW teams at long range, in clear weather | Early–middle | Keep |
| 6 | Mortar crew (`mortar`) | Intelligence (fire support) | Area damage from behind cover, uses shared vision | Infantry groups on a known route; killing an EW station in cover | Early | Keep |
| 7 | Recon drone (`recon_drone`) | Intelligence (reconnaissance) | Gives sight to the whole side | Fog, night, mortar squads, long routes | Middle | Keep |
| 8 | EW station (`ew_station`) | Intelligence | Zone against enemy drones and enemy artillery accuracy | FPV, recon UAV and one-way waves; as a cheap partner to air defence | Middle | **New** |
| 9 | Bomber drone operator (`bomber_operator`) | Drone forces | Reusable drone, drops grenades on a cluster | Infantry in cover; EW teams; targets behind obstacles | Middle | Keep |
| 10 | FPV operator (`fpv_operator`) | Drone forces | Single-use strike on a vehicle, at long range | Tanks and IFVs when no anti-tank is in range; drone-kill on vehicle carriers | Middle | Keep. Upgrades: improvised → factory → fibre-optic (fibre-optic is **new**; not a new type) |

Marksman, sniper and machine gunner upgrades are paths of the rifleman. Grenadier and ATGM (as a separate unit) stay "later" (decided).

**Support calls** (one category, with delay and cooldown): release proposal is 3: howitzer strike (early), rocket salvo (Grad type, middle), air strike (late). HIMARS type and the nuclear weapon are updates and the story ending.

## 7. Check: hard counters and no dead units

**Each attacker has a hard counter.** Yes, see section 3. The weakest are special forces (best counter is only 3.9) and the tank tier "modern" (3.8). Mistral must measure both.

**Each defender has a job.** See column "Job". Danger zones from the current `stop.csv`:

| Defender | Risk | What Claude and Mistral measure |
| --- | --- | --- |
| Rifleman | The machine gunner is better in each fight (20 vs 7 on recruits) | Price per kill. The rifleman wins on cost and as upgrade root. If not, the machine gunner is too cheap. |
| Mortar | Weak against fast units (0.7–1.8) | It must still be the best against infantry in cover. |
| Bomber operator | Low values everywhere (≤ 3.4) | The best choice must be "infantry in cover" and "EW team". If not, raise the number of drops. |
| FPV operator | EW makes it unreliable | It must win against tanks when there is no EW. The fibre-optic upgrade removes the weakness. |
| EW station | New; no value yet | It must change the result of a drone wave in a measured run. |

## 8. Roster size

10 attacker types, 10 defender types, 3 support calls. This is inside the proposal of the issue (8–10 each). The game still feels small because the upgrade paths give the depth.

## 9. Connection to decided items

Uses: "Full unit roster before more balance work", "EW for both sides", "Air defence and one-way drones (low priority)", "Drones: one line", "Infantry: rifleman → marksman → fork", "Three meta branches", "Enemy variety from wave content, not many new types", "Mortar: starting field unit", "Artillery: call buttons".

## 10. What Claude must build or measure

1. Check each row in the Sim: exists, change, new. New: `ew_team`, `ew_station`, fibre-optic FPV.
2. EW as a Sim feature: a zone entity with HP, radius, per-drone-type response (a data column, not an `if` on a name), and a link to artillery accuracy. Check how the old `ew` and `ew_chance` columns relate.
3. Mark removed rows as disabled (data flag), not deleted.
4. Fix `targetable_by` of the recon UAV (the `radar` target does not exist in the defender list).
5. Mistral: one balance pass with the final roster. Pass: each attacker has a defender with "stopped" above a set value; no defender is best nowhere; the squads measurement can start.

## 11. Open questions

1. **Roster cuts: motorbike, van, mrap, helicopter.**
   a) Remove all four from release (proposal). b) Keep the helicopter (needs a second air answer). c) Keep the van as a rare comic enemy.
   Recommendation: a), with c) allowed later. Reason: each has the same job as another unit; fewer types means one balance pass.
2. **EW station: whole battle or battery?**
   a) Whole battle (proposal). b) Battery 60 s, then it stops. c) Whole battle, but the owner can switch it off.
   Recommendation: a). Reason: it is the simplest. The cost is only the HP. Option b) adds a timer to read.
3. **Does EW stop the fibre-optic FPV?**
   a) No (proposal). b) Yes, at a lower chance.
   Recommendation: a). Reason: it gives the late FPV a clear reason to exist and gives air defence a job.
4. **Do defenders get a one-way drone (Shahed type)?**
   a) Not at release (proposal). b) As a support call "one-way strike" (late).
   Recommendation: a). Reason: the issue says "so that it exists", low priority; as a call it would be an easy later add.
5. **Is the enemy EW zone visible before the player finds it?**
   a) Only after a defender sees the station (proposal). b) Always visible. c) Never; only the effect shows.
   Recommendation: a). Reason: it makes the recon drone and the sniper useful, and the player can still act.
6. **Which branch owns the EW station?**
   a) Intelligence (proposal). b) Drone forces.
   Recommendation: a). Reason: it protects the whole side and fits "intelligence"; the drone branch keeps its own risk (EW) and the fibre-optic answer.
7. **Support calls for release: 3 or more?**
   a) Howitzer, rocket salvo, air strike (proposal). b) Add HIMARS. c) Only howitzer.
   Recommendation: a). Reason: three calls give a choice with a clear job each; HIMARS is a later unlock.
