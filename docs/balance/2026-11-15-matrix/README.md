# Full 1 v 1..5 matrix, every defender against every attacker (2026-11-15)

Tool `tools/balance/Matchups.cs` (Yaroslav: open and trench-inside-a-forest-belt; 1 to 5 attackers of each type).
8 defenders x 12 attackers x 5 counts x 2 positions x 50 seeds = 48,000 runs. The defender stands 25 m beside the
route, in the open or in a trench dug inside a forest belt. Attackers come 2 s apart. Mortar, anti-tank team, air
defence and both drone operators always have a recon drone over the approach; rifle squad, machine gunner and sniper
fight alone. Data as of this commit.

Files: `runs.csv` (every run), `summary.csv` (killed %, defender alive %, held %, line damage per cell). The same
grid is on the Sheet, tab `balance_1v1`.
