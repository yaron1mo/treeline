# One defender against one armoured vehicle, hidden in different ways (2026-11-13)

Yaroslav: hide the anti-armour defenders a little and test each of them 1 v 1 against each armoured vehicle.
Tool `tools/balance/ArmourHidden.cs`, summary `tools/balance/summarize_armour_hidden.py`. 40,000 runs:
5 defenders x 4 vehicles x 5 positions x with/without recon x 200 seeds. Data as of this commit (blast radii
mortar 10 m, FPV 7 m, anti-tank 5 m; trench cover fixed; waypoint FPV uses shared vision).

Field 120 x 300 m, one entry at x = 40: the vehicle drives straight down to the rear edge. The defender stands at
y = 200: `route` on the route in the open; the others 25 m aside: `open`, in a `trench` (covered, still seen),
in a forest `belt` along the route (seen at most 50%), behind a `rock` on the enemy side. Recon: a recon drone
that sees the whole approach. The FPV operator sends its drone to a point on the route (40, 120).

Files: `runs.csv` (every run), `summary.csv`, `tables.md` (vehicle stopped % / defender alive %).

## What it shows

1. **Recon decides most of it.** With a recon drone the anti-tank team and the FPV stop every vehicle (100%) from
   any position. Without it, position decides.
2. **Hiding works for the anti-tank team.** Against tanks: open 4-12% stopped, trench 64-73%, forest belt 74-85%.
   Behind a rock it is worse than the open (2%): the rock also hides the tank from the team until it is level.
   Against the IFV it dies anyway (the 8 soldiers who get out), except in the belt (66% stopped).
3. **FPV is the strongest alone.** Old tank and IFV: 100% from anywhere. Modern tank: 30% in the open, 100% in a
   trench or belt. MRAP (fast, high armour): 0% unless in the belt (92%).
4. **The sniper and the mortar kill tanks when someone spots for them.** With recon: sniper vs old tank 92-100%,
   mortar vs old tank 99-100% and IFV 92-100%. Cause: long exposure on an open approach, `heavy` vs medium
   armour 0.3, `explosive` vs medium 0.6, tanks 100 HP. Open for Yaroslav.
5. **The bomber never stops armour** (25 x 0.5 x armour per grenade); hidden, it survives. As intended.
6. **Trench before this run gave no cover at all** (fixed in this commit), so earlier results with trenches are wrong.
