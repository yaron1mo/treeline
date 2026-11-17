# Balance after Yaroslav's tuning (2026-11-15)

Data: mortar damage 15, accuracy 0.3, min range 55 m; machine gunner accuracy 0.3; rifle squad accuracy 0.8;
sight +10 m for rifle squad (40), machine gunner (50), sniper (70). Shooting model as in `../2026-11-15-ballistics/`.
Same runners and layout (`armour-*`, `infantry-*`).

1. **Sight decides infantry fights.** Our shooters now see further than their targets (rifle 40 vs regular 30,
   machine gunner 50 vs special forces 40, sniper 70 vs special forces 40), fire first, and at close range hardly
   miss. Held (gap 1.5 s): rifle squad vs 3 / 4 / 5 regulars 91 / 63 / 27% (was 0); vs 5 recruits 96%;
   machine gunner vs 2 / 3 / 5 special forces 100 / 94 / 64% (was 0); sniper vs 4 special forces 94% (was 0).
   The lower machine-gun accuracy (0.3) changes little: it still hits whatever is close.
2. **Mortar:** with recon it still stops every old tank and IFV, no longer any modern tank or MRAP (0%; was 100%).
   Alone it does nothing against armour: it sees 40 m and may not fire under 55 m, so it needs a spotter.
3. Everything else in the armour test unchanged (no cell moved 5 points or more).
