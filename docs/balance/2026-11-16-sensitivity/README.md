# Which stat matters: MRAP, van, buggy, motorbike (2026-11-16)

Yaroslav: which stat of the MRAP and the van decides how far they get; buggy and motorbike maybe need more HP.
`tools/balance/sensitivity.py` changes one column of one unit in a copy of the data and runs `Matchups.exe` against
all 8 defenders, open and trench+belt, 1 to 5 attackers 0.3 s apart, 50 seeds (recon for mortar, ATGM, air defence and
drone operators). Metric: mean line damage per run (passengers count); `variants.csv` overall, `by_defender.csv` per defender.

## MRAP (hp 60, armour high, 4 special forces inside)

Only the ATGM and the FPV can hurt it: small arms do 0 to high armour, mortar and bomber grenades 0.05. So:
- **armour is the stat**: high -> medium: the ATGM lives 23% -> 94%, the FPV halves its line damage (18.8 -> 9.0) and lives 6% -> 95%; low: the machine gunner and sniper start to stop it too (-17% overall).
- HP, speed, gun, sight, dismount settings: within 4%. HP 30 still helps the ATGM (one shot either way, fewer missiles needed for a group).
- Passenger count moves line damage directly (each special forces soldier who reaches the rear counts): 2 -> -30%, 6 -> +29%.

## Van (hp 50, no armour, 12 recruits)

- **What is inside** dominates: 6 recruits -42%, 18 recruits +42%.
- **HP**: 25 -> -36% (sniper and machine gunner stop it), 75 -> +29% (and the FPV can no longer stop it: 2 -> 37.7).
- **Armour medium** makes it immune to small arms: +34%.
- Speed: 5.5 m/s -13% but the defenders die more (the recruits get out closer and slower to the line); 16.5 m/s +22%.
- Dismount at 0.4 of HP: +9% and defenders die much more (troops get out earlier, further up).
- Hit radius, blast, dismount interval, regular instead of recruit: within 5%.

## Buggy and motorbike

They hardly get through: mean line damage about 1 per run out of 3 possible (one vehicle in three, on average over 1 to 5).
More HP is the biggest lever after speed: buggy 25 -> 38 HP +36%, 50 HP +69%; motorbike 15 -> 23 HP +35%, 30 HP +60%.
Speed is as strong: 15 m/s +48% (buggy), +63% (motorbike). Nobody dies to them (they do not shoot).
