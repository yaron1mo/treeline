# Full 1 v 1 matrix, every defender against every attacker (2026-11-15)

Tool `tools/balance/Matchups.cs`: 8 defenders x 12 attackers x 3 positions (open, trench, forest belt; 25 m beside the
route) x 100 seeds = 28,800 runs. Mortar, anti-tank team, air defence and both drone operators always have a recon
drone over the approach (Yaroslav: recon is their base condition); rifle squad, machine gunner and sniper fight alone.
Data as of this commit (Sheet = source of truth). Enemy aviation is new in this commit: FPV dives at a defender its
side sees (own camera 40 m), helicopter drops 20 regulars 55% of the way down and flies out.

Files: `runs.csv`, `summary.csv` (stopped %, defender alive %, line damage per pairing and position), `tables.md`.

Main points:
1. Infantry: the three shooters and the FPV operator win every 1 v 1 against single recruits and regulars; the rifle
   squad loses to special forces unless hidden in a belt (0 / 41 / 98% open / trench / belt).
2. Armour: only the anti-tank team and the FPV operator stop it (95-100%); everything else 0%. Hidden, small arms
   survive a passing tank more often (machine gunner vs modern tank: alive 0 / 89 / 71%).
3. Fast transport: small arms stop buggies and motorbikes 100%; the van stops and lets 12 recruits out, which kill
   a rifle squad in the open (alive 0%) but not in a belt (90%).
4. Enemy FPV: hard to shoot down (rifle 21%, machine gunner 52%, sniper 28%, air defence 67%); otherwise it kills
   the defender most of the time, from above (trench and belt do not help). Our FPV drone intercepts it 100%.
5. Helicopter: only air defence brings it down (100%), after some troops are already out (line damage ~4 of 20);
   against anyone else 20 regulars land and the defender dies.
