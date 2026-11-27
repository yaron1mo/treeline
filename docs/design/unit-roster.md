# Full unit roster (both sides)

Proposal by the designer agent, 2026-11-27, for issue #26. Status: **Open**, updated after two rounds of decisions of Yaroslav on PR #28 (the same day). Nothing here changes a "Decided" item in `progression.md`. The squad rules are in [squad-selection.md](squad-selection.md) (PR #24).

## 1. Goal

Agree one final list of attacker and defender types, so that Mistral balances once. Each attacker has a hard counter. Each defender has a job and a situation where it is the best choice. Depth comes from upgrade paths and squad choice, not from many types.

## 1a. Decisions of Yaroslav (2026-11-27) used in this file

| # | Decision | Where it is used |
| --- | --- | --- |
| 1 | Keep motorbike, van, MRAP and helicopter. Each gets a clear role and counter. | Section 3 |
| 2 | EW stations of both sides work until they are destroyed. They send small waves from time to time. | Section 4 |
| 3 | EW does not stop the fibre-optic FPV. | Section 4 |
| 4 | The Shahed-type drone is an attacker, added now. It does not damage units. It flies slowly past the defence and takes base health. The player must spend a slot on air defence. | Sections 3, 5 |
| 5 | The player does not see the EW zone. The player sees the waves and the effect on drones. | Section 4 |
| 6 | EW belongs to the Drone forces branch. | Section 6 |
| 7 | Support calls at release: howitzer, rocket salvo, air strike. | Section 6 |
| 8 | 14 attacker types accepted. | Section 3, 8 |
| 9 | The own EW radius is visible in the placing mode and when the player taps the station. Never in battle without a tap. | Section 4 |
| 10 | The Shahed-type drone has a fixed base damage per drone. | Section 5 |
| 11 | EW **slows** a Shahed and gives it **periodic damage**. One station does not destroy it. **Two overlapping stations destroy one Shahed.** (A Shahed that learns: later, probably not.) | Sections 4, 5 |
| 12 | EW is a **utility module for the player's recon drones**. It uses a separate "utilities" slot that the player takes into the mission. | Sections 4, 6 |

## 2. Player experience

The player sees 14 enemy types. Each has a clear icon and one clear answer ("tank: anti-tank team", "Shahed: air defence"). The brief shows the class, so the player picks the answer before the battle. A late wave mixes types, so one answer is not enough. The EW station is a rare surprise: in battle the player sees waves from a station and drones that shake and smoke, not a zone on the map. The player sees the radius of the own station only when placing it or tapping it.

## 3. Attackers (14 types for release)

Source of numbers: `units.csv`. "Stopped" values are from `tools/balance/stop.csv` (higher = the defender stops it better). The list is longer than the first proposal, but the new rows are variants of existing jobs: the vehicles use the same `behaviour` columns (`carrier`, `air_assault`).

| # | Type (`id`) | Role | Hard counter (best) | It counters | Appears | In `units.csv` |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Recruit (`recruit`) | Cheap infantry mass | Machine gunner (20), sniper (20) | Defenders with no cover; thin lines | Early | Keep |
| 2 | Regular soldier (`regular`) | Infantry, low armour | Machine gunner (20), mortar (13) | Rifle squads | Early | Keep |
| 3 | Special forces (`special_forces`) | Elite infantry, sees at night, fast | Sniper (3.0), mortar (3.9), machine gunner (2.4) | Short-sight defenders at night; drone crews | Middle | Keep |
| 4 | Motorbike (`motorbike`) | **Runner:** 15 HP, fastest ground unit. It tests the line and takes a small base hit if nobody stops it. It makes the player cover the whole front, not one point. | Rifleman or machine gunner (any fast fire; it dies to a burst) | Single heavy defenders (anti-tank, mortar) that cannot turn fast | Early | Keep. Check its row in `stop.csv` |
| 5 | Buggy (`buggy`) | **Raider:** 30 HP, fast, runs past the line | Machine gunner (14), sniper (10) | Slow, short-range defenders (mortar, anti-tank) | Early | Keep |
| 6 | Van (`van`) | **Soft carrier:** 50 HP, no armour, carries 7 recruits and unloads them at a set point. It is a cheap way to put infantry behind the first line. | Machine gunner on the van before it unloads; mortar on the unloaded group | Lines that only watch the front | Early–middle | Keep |
| 7 | MRAP (`mrap`) | **Armoured carrier:** 60 HP, medium armour, carries 4 special forces. Small arms do not stop it. It brings elite infantry through a weak line. | Anti-tank team; FPV operator | Infantry-only squads; machine gunners | Middle | Keep |
| 8 | Infantry fighting vehicle (`ifv`) | Heavy carrier with a gun: unloads 8 regulars, shoots while it moves | Anti-tank team (3.0) | Infantry-only squads | Middle | Keep |
| 9 | Tank (`old_tank`, later `modern_tank`) | Heavy armour, long range | Anti-tank team (3.8–5.1), FPV (1.0–2.4) | Everything without anti-tank | Middle (old), late (modern) | Keep both rows as two tiers of one type |
| 10 | Helicopter (`helicopter`) | **Air assault:** 150 HP, flies over the line and drops 20 regulars at 55% of the route. The ground line cannot stop it. | Air defence (only strong counter). FPV and explosive can hit it. | Any squad with no air defence; the rear line | Late | Keep. Check that `targetable_by` matches section 5 |
| 11 | Recon UAV (`recon_uav`) | Spotter: calls FPV on what it sees | Air defence, sniper, machine gunner | Static defenders in the open | Middle | Keep. Fix `targetable_by` (no "radar" defender exists) |
| 12 | FPV drone (`fpv`; later fibre-optic FPV) | Single-use strike on a spotted unit | Air defence (2.4), machine gunner (1.4). EW stops the radio FPV, **not** the fibre-optic FPV | Mortar, sniper, operators (soft, valuable units) | Middle (radio), late (fibre-optic) | Keep. Fibre-optic: **new** row, same `behaviour` |
| 13 | One-way drone, Shahed type (`one_way_drone`) | **Base attacker.** It does no damage to units. It flies slowly past the defence to the base and takes base health. | Air defence (only hard counter). EW can turn it away. | Squads with no air defence | Middle (rare), late (often) | Add now |
| 14 | EW team (`ew_team`) | Carries or drops an EW station. Weak in a fight | Mortar, sniper, bomber drone, FPV (kill the station) | Drone squads | Middle | **New** (a `regular` that carries the station; see section 4) |

Why the four kept vehicles are not copies of each other:

| Pair | Difference the player feels |
| --- | --- |
| Motorbike and buggy | The motorbike is one weak runner; the buggy is a tougher raider. Both are answered by fast fire. |
| Van and MRAP | Same job (carrier) at two tiers: the van is soft and brings recruits; the MRAP is armoured and brings elite infantry. Machine gun answers the van; anti-tank answers the MRAP. |
| MRAP and IFV | The IFV is bigger, shoots and carries more. It comes later. |
| Helicopter and Shahed | The helicopter brings soldiers (a ground fight follows). The Shahed brings only base damage (no fight, a timer). Both need air defence, so the player must keep that slot. |

Rare comic enemies (decided, not in the count): for example the looter pig with a washing machine.

## 4. EW mini station (both sides)

Decided rules: on a vehicle or carried by a soldier; can be dropped; keeps working after the carrier dies; low HP; must be destroyed separately; both sides; **works until it is destroyed** (no battery, no switch); **belongs to the Drone forces branch**.

| Item | Proposal |
| --- | --- |
| HP | 6 (one rifle burst kills it) |
| Radius | 50 m. Drone sight is 40 m, so a drone is inside the zone before it can see the station. |
| Carrier | The attacker EW team or a vehicle. The defender EW station is a placed unit (type 8 on the defender list). |
| Drop | The carrier drops the station at its first stop, or when it dies. The station stays. |
| Duration | Until it is destroyed. |

Effect on each air item, simple rule: **inside the zone the radio link is bad.**

| Item | Effect inside the zone |
| --- | --- |
| Recon drone | Sight radius is half (40 m → 20 m). It still lives. |
| Bomber drone (Mavic type) | Cannot hold position: it flies back to its launch point and tries again after it leaves the zone. No drops while inside. |
| FPV (radio) | 80% chance that it loses the link on entry and falls. |
| FPV, fibre-optic | **No effect.** This is its job: it beats EW and loses to air defence and machine guns. |
| One-way drone | It slows down and takes periodic damage. One station does not destroy it. Two overlapping stations destroy one Shahed. |
| Artillery (support call) | The "seen by our drones" accuracy bonus does not work for a target in the zone. The shell uses the unseen accuracy. |

What the player sees (decided: **the zone is not drawn in battle**; the own station shows its radius in the placing mode and on tap):
- The station sends small waves from time to time. This shows that it is an EW station. For an enemy station, the waves are visible only when a defender sees the station (normal fog rules).
- A recon drone in the zone shakes and smokes.
- An FPV drone in the zone gets an electric layer (flicker), then falls with a spark.
- The bomber drone shows the same shake and returns.
- The fibre-optic FPV shows nothing. The player learns that it is immune.
- A Shahed in the zone slows down and smokes. In two zones it burns and falls.

The enemy radius is never shown: the player learns it by play. The own radius is shown as a ring in the placing mode and on tap (decided).

**Utilities slot (decided).** The EW station is a utility module of the recon drones. The player takes it into the mission in a separate "utilities" slot, not in a squad slot. How it is placed on the field: see question 1.

## 5. Air defence, helicopters and Shahed-type drones

| Shooter | Hits | Does not hit |
| --- | --- | --- |
| Air defence gun | Recon UAV, FPV, fibre-optic FPV, helicopter, Shahed-type drone | Ground units |
| Machine gunner | Recon UAV, FPV (low chance) | Helicopter, Shahed-type drone |
| Sniper | Recon UAV | FPV, helicopter, Shahed-type drone |
| FPV drone (own) | Recon UAV, helicopter (air hit) | — |

The Shahed-type drone has no AI: it flies a slow, straight line over the defence to the base and does a fixed base damage per drone (decided) when it arrives. It does no damage to units on the way. The player moves and aims nothing. The slow speed gives the air defence time to shoot. In an EW zone it slows down more and takes periodic damage (decided). One station does not kill it; two overlapping stations kill one Shahed. So EW is a good partner of air defence, but not a replacement for it. The only questions are "is there air defence?" and "how many EW stations overlap?". Its purpose is to force the player to take an air defence slot. The defender gets no one-way drone at release.

## 6. Defenders (10 types for release)

Branches are the decided three: Assault, Drone forces, Intelligence.

| # | Type (`id`) | Branch | Job | Best choice when | Appears | In `units.csv` |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Rifleman (`rifle_squad`) | Assault | Cheap base and upgrade root (marksman, then sniper or machine gunner) | Money is short; early waves; the player needs many cheap bodies to hold ground | Early | Keep |
| 2 | Machine gunner (`machine_gun`) | Assault | Stops infantry and fast units; soft answer to drones | Waves of recruits, regulars and buggies; the first line | Early | Keep |
| 3 | Anti-tank team (`anti_tank`) | Assault | Stops armour (only hard counter to IFV and tank) | Any wave with a vehicle | Middle | Keep |
| 4 | Air defence gun (`air_defence`) | Assault | Stops all air items | Recon UAV, FPV, helicopter and Shahed waves | Middle | Keep |
| 5 | Sniper (`sniper`) | Intelligence | Long range, picks one target | Elite infantry and EW teams at long range, in clear weather | Early–middle | Keep |
| 6 | Mortar crew (`mortar`) | Intelligence (fire support) | Area damage from behind cover, uses shared vision | Infantry groups on a known route; killing an EW station in cover | Early | Keep |
| 7 | Recon drone (`recon_drone`) | Intelligence (reconnaissance) | Gives sight to the whole side | Fog, night, mortar squads, long routes | Middle | Keep |
| 8 | EW station (`ew_station`) | Drone forces | Zone against enemy drones and enemy artillery accuracy. A utility module of the recon drones, in its own utilities slot | FPV, recon UAV and Shahed waves; two overlapping stations kill a Shahed | Middle | **New** |
| 9 | Bomber drone operator (`bomber_operator`) | Drone forces | Reusable drone, drops grenades on a cluster | Infantry in cover; EW teams; targets behind obstacles | Middle | Keep |
| 10 | FPV operator (`fpv_operator`) | Drone forces | Single-use strike on a vehicle, at long range | Tanks and IFVs when no anti-tank is in range; drone-kill on vehicle carriers | Middle | Keep. Upgrades: improvised → factory → fibre-optic (fibre-optic is **new**; not a new type) |

Marksman, sniper and machine gunner upgrades are paths of the rifleman. Grenadier and ATGM (as a separate unit) stay "later" (decided).

**Support calls** (decided: three at release; one category, with delay and cooldown): howitzer strike (early), rocket salvo (Grad type, middle), air strike (late). HIMARS type and the nuclear weapon are updates and the story ending.

## 7. Check: hard counters and no dead units

**Each attacker has a hard counter.** Yes, see section 3. The weakest are special forces (best counter is only 3.9) and the tank tier "modern" (3.8). Mistral must measure both.

**Each defender has a job.** See column "Job". Danger zones from the current `stop.csv`:

| Defender | Risk | What Claude and Mistral measure |
| --- | --- | --- |
| Rifleman | The machine gunner is better in each fight (20 vs 7 on recruits) | Price per kill. The rifleman wins on cost and as upgrade root. If not, the machine gunner is too cheap. |
| Mortar | Weak against fast units (0.7–1.8) | It must still be the best against infantry in cover. |
| Bomber operator | Low values everywhere (≤ 3.4) | The best choice must be "infantry in cover" and "EW team". If not, raise the number of drops. |
| FPV operator | EW makes it unreliable | It must win against tanks when there is no EW. The fibre-optic upgrade removes the weakness. |
| EW station | New; no value yet | It must change the result of a drone wave in a measured run, with the zone hidden. |
| Motorbike, van, MRAP, helicopter | Kept by decision; no check yet that each has a clear counter | Each must have a defender with "stopped" above the set value, and must not be a copy of the buggy or IFV. |

## 8. Roster size

14 attacker types, 10 defender types, 3 support calls. The attacker count is above the 8–10 of the issue, because Yaroslav kept four vehicles (14 accepted). Hard counters, not the count, keep it readable: 5 carriers/runners are answered by only 3 defender answers (fast fire, anti-tank, air defence).

## 9. Connection to decided items

Uses: "Full unit roster before more balance work", "EW for both sides", "Air defence and one-way drones", "Drones: one line", "Infantry: rifleman → marksman → fork", "Three meta branches", "Enemy variety from wave content, not many new types", "Mortar: starting field unit", "Artillery: call buttons".

## 10. What Claude must build or measure

1. Check each row in the Sim: exists, change, new. New: `ew_team`, `ew_station`, fibre-optic FPV, `one_way_drone` (slow, fixed base damage, no unit damage). Also: the Shahed response to EW (slow, periodic damage, death in two overlapping zones); a ring for the own station in the placing mode and on tap; the utilities slot in the run setup.
2. EW as a Sim feature: a zone entity with HP, radius, per-drone-type response (a data column, not an `if` on a name), a link to artillery accuracy, and a periodic "wave" event that Render can show. The zone has no draw event. Check how the old `ew` and `ew_chance` columns relate.
3. Effect events for Render: `drone_jammed` (shake and smoke for recon, electric layer for FPV, smoke for Shahed).
4. Fix `targetable_by` of the recon UAV (the `radar` target does not exist in the defender list). Check helicopter `targetable_by` against section 5.
5. Mistral: one balance pass with the final roster. Pass: each attacker has a defender with "stopped" above a set value; no defender is best nowhere; the squads measurement can start. Measure motorbike, van and MRAP first (not yet in the counter table).

## 11. Open questions (new, after the second round)

1. **How is the EW station placed in the field, if it is a recon-drone utility?**
   a) The player drops it on the field like a defender; the utilities slot only decides that it is available, and the number of stations is limited per mission. b) The recon drone carries it and drops it where the drone is (the player sends the drone). c) Both: a drone drop is the upgrade of a).
   Recommendation: a). Reason: it is the simplest on a phone, and the hard choice is where to place it. b) needs a new control.
2. **Utilities slot: how many, and what else goes in it?**
   a) One slot, EW only at release. b) One slot; EW and one more module later (for example a decoy or a flare). c) Two slots from the middle of the campaign.
   Recommendation: a), with b) as a later update. Reason: a slot with one item is not a choice yet; do not add items before the roster is measured.
3. **Does the later upgraded Shahed (anti-jam antenna) ignore part of the EW effect?**
   a) Yes: less slowdown and less damage (as in public reports). b) No: it only has more HP. c) Not at release.
   Recommendation: c). Reason: you said a learning Shahed is "probably not"; keep one Shahed for the first measurement.
