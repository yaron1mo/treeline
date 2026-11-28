# Progression and meta design

Source of truth for design decisions. Copied from Notion page "11 — Progression & Meta Design (draft)" on 2026-11-27 and extended with the decisions of 2026-11-25…27. Owner: Yaroslav (decisions). Claude (systems, numbers). Designer agent (proposals).
Write all text in ASD-STE100. Mark each item as **Decided** (with date and who) or **Open**.

## 1. Decided

- **Unit = one defender on the field.** Each unit has its own call sign. (Yaroslav, 2026-11-24)
- **Base unit per branch, upgrade paths from it.** Infantry: rifleman → marksman → sniper, or rifleman → machine gunner. Artillery: mortar. Drones: bomber drone. Each base unit scales and branches.
- **Upgrades:** money can upgrade a unit at any time. Experience makes the upgrade 50–80% cheaper (exact value: balance).
- **Modes have separate progress:** story (own roster), weekly challenge (same for all players, donations allowed), endless (own roster, kept for all time).
- **Age rating 16+**, no gore. Note (Claude, 2026-11-25): cartoon blood on non-human characters can rate PEGI 12; check the IARC form before release.
- **Call signs:** the player names each unit, or the game gives a random word. No real people's names in gameplay.
- **Death:** a unit at 0 HP is killed ("200") by default. When a valuable upgraded unit dies, a pop-up offers to save it: "for N money or 3 ads we retreat and save <call sign>". The player chooses.
- **Retreat from the battle:** the player can leave the battle; no earnings, all units saved. This ability can be upgraded (for example in the intelligence branch).
- **Unlocked weapons:** close to real ones (ATGM, air defence, tanks, jets, nuclear weapon at the end). Names: legal check before release.
- **Counter pacing:** the whole counter takes 30–50 hours.
- **World:** attackers are pigs in helmets (zombie pigs; Codex art, PR #8); defenders are undead in modern military style (smiling skulls). Call signs mean "they do not die". No real flags, symbols or letters (store risk).
- **Three meta branches (maximum 3):** Assault; Drone forces; Intelligence (two paths: reconnaissance, fire support). Imbalance is allowed, a dead branch is not.
- **Infantry:** rifleman → marksman → fork: sniper or machine gunner. Grenadier and ATGM: later. Sniper = intelligence; marksman = assault.
- **Drones:** one line: recon → bomber → improvised FPV → factory FPV → fibre-optic FPV.
- **Mortar:** starting field unit; upgraded by the intelligence branch (fire support path).
- **Artillery** (howitzer, Grad, HIMARS): not on the field. Call buttons with delay and cooldown. Accuracy is better where our side sees (drones). Support calls (artillery, air strikes, nuclear weapon) are one category.
- **Squad before each mission** (Yaroslav, 2026-11-25): the player takes a limited number of unit types (slots), as in XCOM and the seed choice of Plants vs. Zombies. Slots open with progress. The choice must be hard: the wave needs some types (hard counters), the player wants others. One squad that wins everything is a failure (measured by `mistral/squads`).
- **Depth from upgrade paths, not from many unit types** (Yaroslav, 2026-11-25): two units of the same type with different paths must play differently.
- **Enemy variety from wave content and mission modifiers** (night, fog, EW, two directions), not from many new enemy types. Rare comic enemies are allowed (example: a looter pig with a washing machine). Do not copy Plants vs. Zombies enemies.
- **Weekly challenge** (Yaroslav, 2026-11-25): same map, seed and modifier for all players; local score; leaderboard (Game Center, Google Play Games); reward tiers (cosmetic). No loot boxes. Each week is for one fund (donation).
- **Maps** (Yaroslav, 2026-11-27): Forest belts is the reference map. Balka and River crossing are not a priority.
- **Unit art** (Yaroslav, 2026-11-27): units first (directional views and animations), then maps.

- **Full unit roster before more balance work** (Yaroslav, 2026-11-27): agree all attacker and defender types first, so that the balance is not done again for each new unit. The squads measurement waits for this.
- **EW for both sides** (Yaroslav, 2026-11-27): a mini EW station. It can be on a vehicle or carried by a soldier. The carrier can drop it, and it keeps working after the carrier is killed, so the player must destroy it separately. It has low HP. Effect on drones: to be proposed by the designer.
- **Air defence and one-way attack drones (Shahed type)** (Yaroslav, 2026-11-27): add them to the roster, with low priority ("so that they exist"). The game must not get more complex because of them.
- **Visual target** (Yaroslav, 2026-11-27): the game must feel like a tasty candy: clear, bright, satisfying feedback.
- **Real maps** (Yaroslav, 2026-11-27): 20 map prototypes (at least 15) from real terrain (OpenStreetMap data, fictional sector names). Each map can later get a winter and a night version.

- **Squad slots** (Yaroslav, 2026-11-27): 3 slots. Recon drones have a separate slot with their own count.
- **Wounds only** (Yaroslav, 2026-11-27): no fatigue.
- **Forecast by intelligence level** (Yaroslav, 2026-11-27): each intelligence upgrade opens one item: number of waves; attacker types on the level; composition of the next wave; the optimal attacker route.
- **Endless** (Yaroslav, 2026-11-27): many different levels of different length (7, 10, 20 waves …); each level is a different map.
- **No pair bonus and no pair hint** (Yaroslav, 2026-11-27): players find good pairs alone.

- **Roster decisions on PR #28** (Yaroslav, 2026-11-27): keep motorbike, van, MRAP and helicopter (each needs a role). EW stations of both sides work until destroyed; they show small waves from time to time. EW does not stop fibre-optic FPV. The player does not see the EW zone; the player sees the station work and its effect on drones (shake, smoke, an electric layer). EW belongs to the Drone forces branch. Support calls at release: howitzer, rocket salvo, air strike.
- **Shahed-type drone (attacker), added now** (Yaroslav, 2026-11-27): no damage to units; flies slowly past the defence and takes health from the base; makes the player spend a slot on air defence.
- **Art:** `docs/design/art-brief.md`. A satire, not realism or horror. Pigs are ridiculous, not scary. No zombies on the defence side: human → experienced human → veteran skeleton with black humour and sarcasm (Yaroslav, 2026-11-27).

## 2. Open

- **Death or wounded:** a unit at 0 HP is wounded ("300") or killed ("200"); proposal 50/50; what evacuation costs; recovery time. Proposal: [unit-progression.md](unit-progression.md), sections 5 and 9.
- **Unit roster for release** (10 attacker types, 10 defender types, 3 support calls), EW station numbers and effects, air-defence rules, roster cuts. Proposal and options: [unit-roster.md](unit-roster.md).
- **Roster follow-up (2026-11-27, second round on PR #28):** how the EW station is placed when it is a recon-drone utility; how many utility slots and what else goes in them; whether an upgraded Shahed resists EW. Options: [unit-roster.md](unit-roster.md), section 11.
- **Barracks size** and units per level. Proposal: [unit-progression.md](unit-progression.md), section 6.
- **Unit upgrade paths and experience** (designer, 2026-11-28, issue #20): paths for rifleman, mortar and recon drone; experience as a discount; whether a step needs the branch level. Options: [unit-progression.md](unit-progression.md), sections 3, 4 and 9.
- **Slots:** how many at start, how they open.
- **Veteran slots and general slots** (Yaroslav's idea, 2026-11-27): if all upgraded veterans die, where do new units come from? Separate veteran slots and general slots; effect on the weekly challenge (nothing is carried over). To design together.
- **Weekly challenge squad:** later (separate balance).
- **In-battle progression:** upgrades during a battle and their costs.
- **Campaign structure:** chapters, levels per chapter, what each level introduces.
- **Monetisation model** (Yaroslav, 2026-11-25, not decided): (a) free first chapters + one purchase of the full campaign, (b) paid game, (c) free with rewarded ads and purchases.
- **Campaign length for release:** proposal 10–15 hours, the rest as updates.
- **Story:** the tutorial character, advisors, the ending.
- **Map concept sheets** (designer, 2026-11-27, issue #48): 22 maps in [maps/README.md](maps/README.md). Open: which maps stay (goal 20); whether crop rows count as "filled"; five new Sim rows (`stack`, `shed`, `silo`, `camo_net`, `dugout`); how the designed maps relate to the real-terrain maps; campaign order of the maps.

## 3. The player's goal (Yaroslav, 2026-11-24)

- A total enemy-loss counter across all modes. Milestones unlock new weapons.
- The last milestone unlocks the final weapon: a nuclear weapon. Using it is the story ending (short cut-scene, a 3–5 minute post-apocalyptic wave, credits). If the player does not use it, the game continues.

## 4. Risks

- Kill counter as the main goal in a war-themed game (store sensitivity, framing).
- Nuclear ending near a real conflict (framing, store review).
- Pig enemies must not read as a real nationality.
