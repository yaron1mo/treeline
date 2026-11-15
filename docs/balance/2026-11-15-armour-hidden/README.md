# Armour 1 v 1, hidden defenders: rerun after the 2026-11-15 data changes

Same test as `../2026-11-13-armour-hidden/` (tool `tools/balance/ArmourHidden.cs`, 40,000 runs), on data with the
sniper as `small_arms` and the mortar at 25 damage x 0.8 shots/s (Yaroslav's Sheet edits).

- **Sniper no longer kills armour:** with recon it stopped 92-100% of old tanks and up to 42% of IFVs; now 0%.
  It also no longer draws the IFV's troops out by hitting it, so it survives the IFV (0% -> 66-100%), while against
  an old tank it now mostly dies (it cannot hurt it and the tank sees it).
- **Mortar almost unchanged:** 25 x 0.8 = 20 damage per second, as before (20 x 1). With recon it still stops
  99-100% of old tanks and 89-96% of IFVs; modern tank 22-49%, MRAP 22-58%.
- Other defenders unchanged (no cell moved by 5 points or more).
