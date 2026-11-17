# Infantry balance calibration — v1 (2026-11-13)

## Updated difficulty and tactical defence specification (2026-11-13)

- **Reference attacker = Regular soldier**, rather than Recruit. Recruit and special forces retain deliberately different matchup outcomes.
- **Hard/open field:** a single ordinary Rifleman with no terrain defensive bonus should be overrun at approximately **1:2 Regulars**. Hard is the default difficulty for general play; campaign/story may later offer selectable difficulty.
- **With a strong defensive position:** a Rifleman should hold against approximately **1:6–1:8 Regulars** (at least six). This is a target for the *defensive positional advantage*, not an automatic multiplier to sight, HP, or armour.
- The numerical threshold is intentionally a **design target**, not yet a specified probability. The calibration report should separately show P(any breach), P(defender death), average number killed, and remaining defender HP, for each count.
- Separate scenarios: (a) unfortified open field, (b) a future controlled, well-defended position. Do not silently infer the latter from open-field results; cover placement and exact cover multipliers still need definition.
- Sight range: machine gunner **40 m**, sniper **60 m**. Attack range is unchanged. These values are changed in the authoritative Sheet and in the branch CSV.
- Keep sniper anti-elite/high-priority and machine gun anti-mass/light-infantry roles as distinct qualitative constraints.

Note that any claimed historic military attacker:defender ratio is not an empirical rule of this simulator; these are game-feel targets chosen by the designer.

## Game design goals (Yaroslav)
- Balance baseline: **one ordinary defender, no terrain advantage**, versus a sequence of attackers.
- Intended nominal *overrun threshold* (number of attackers required per defender): **Hard 3:1, Normal 4:1, Easy 5:1**. These are game-design objectives, not claims about military doctrine or preselected probabilities.
- Main skill: choosing strong positions, predicting routes, keeping defenders alive. Terrain advantages are intentionally excluded from this initial calibration.
- Rock–paper–scissors roles:
  - Rifleman: default all-round infantry defender.
  - Machine gunner: excels against **numerous, lightly protected** infantry.
  - Sniper: excels at **valuable and fast** threats, especially special forces; needs range/targeting opportunity to realize that role.

## Experiments (before grid search)
1. Monte Carlo on real `Treeline.Sim` with seeded per-shot hit/miss:
   - One defender (rifleman, gunner, sniper) vs 1, 2, 3, 4, or 5 attackers (recruit, regular, special forces).
   - Three release intervals 0.75, 1.5, 3 seconds; 100 random seeds per combination (**13,500 runs**).
   - 40x300m, no obstacles/cover or defensive buffs; defender at (20,200), enemy wave begins at north edge.
   - Key: *probability no attackers cross*, mean attackers killed, *probability defender survives*, remaining defender HP (%), and breakthrough rate. Use Wilson 95% CIs for the two binary outcomes and approximate 95% CI for average penetration.
2. Local one-at-a-time sensitivity:
   - Counts 3, 4, 5; spawn interval fixed at 1.5 s; 40 shared seeds per paired baseline and changed scenario.
   - Individually perturb **±20%** of defender HP, per-hit damage, attack speed, accuracy, sight range; attacker HP and movement speed.
   - Report paired differences in penetration (percentage points), kills, defender survival and remaining HP; rank by average absolute change.
   - **15,120 runs**. Screening only: does not reveal interactions or globally optimal values.
3. Existing legacy 900-run sweep remains as smoke/baseline.
 
## How we judge ratios
A numerical "one defender can deal with three attackers" is incomplete by itself. Evaluate the *entire curve* P(no breakthrough | attackers=n), P(defender survives | n), and mean kills, rather than declaring a winner from one simulation. Let Yaroslav specify acceptable probabilities at 3/4/5 after inspecting estimates. Do **not** hard-code guessed win-rate targets or nerf unit stats to force ratios.

## Caveats and mechanics to inspect next
- The `spawn_gap_s` setting changes the **release time**, not guaranteed geometric separation. Speed × spawn gap is an estimate for same straight path only; each spawn can choose a different entry position.
- The simulation has no suppression mechanics and uses nearest-target selection. It does not currently implement sniper high-value targeting or immunity to close-range counterfire.
- Sight and shared detection may cap engagement even if `range_m` is high. The open-field setup must not be mistaken for a combined defence or terrain simulation.
- Different classes should not be forced to the exact same hold-rate curve: their strengths against recruit vs SF should be distinctive.
- Once acceptable probabilities are defined, proceed to parameter search; keep separate seeds for final validation to reduce overfitting.
